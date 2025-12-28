using System.Collections;
using UnityEngine;

/*
 * DirectionalRotationAudio:
 * Reproduce audio direccional basado en la rotación del objeto alrededor de un eje específico,
 * con clips separados para direcciones positiva y negativa, y control de volumen y tono.
 */

[DisallowMultipleComponent]
public class DirectionalRotationAudio : MonoBehaviour
{
    public enum AxisSource
    {
        LocalAxis,   // Usar el eje forward/up/right del objeto
        WorldAxis,   // Usar los ejes X/Y/Z del mundo
        FromTwoPoses // Calcular el eje desde Pose A a Pose B
    }

    public enum AxisChoice { Forward, Up, Right, X, Y, Z }

    [Header("Axis Detection")]
    public AxisSource axisSource = AxisSource.LocalAxis; // Fuente del eje de rotación
    public AxisChoice axisChoice = AxisChoice.Up; // Elección del eje específico

    [Tooltip("Pose A and Pose B used to infer rotation axis (FromTwoPoses mode).")]
    public Transform poseA; // Transform para Pose A
    public Transform poseB; // Transform para Pose B

    [Header("Clips")]
    [Tooltip("Played while rotating in + direction around the axis.")]
    public AudioClip positiveClip; // Clip de audio para rotación positiva
    [Tooltip("Played while rotating in - direction around the axis.")]
    public AudioClip negativeClip; // Clip de audio para rotación negativa

    [Header("Levels")]
    [Range(0f, 1f)] public float maxVolume = 1f; // Volumen máximo

    [Header("Thresholds")]
    [Tooltip("Angular speed (rad/s) to start sound.")]
    public float startThreshold = 0.4f; // Velocidad angular para iniciar sonido
    [Tooltip("Angular speed (rad/s) to stop sound (hysteresis).")]
    public float stopThreshold = 0.25f; // Velocidad angular para detener sonido
    [Tooltip("If below stop threshold for this long, fade out.")]
    public float idleTimeout = 0.25f; // Tiempo de inactividad para fade out

    [Header("Timing")]
    [Tooltip("How many times per second to sample (lower = cheaper).")]
    public float sampleRateHz = 12f; // Frecuencia de muestreo
    [Tooltip("Seconds for crossfades between clips.")]
    public float fadeDuration = 0.2f; // Duración del fade

    [Header("Optional Pitch Mapping")]
    public bool pitchBySpeed = false; // Si mapear tono por velocidad
    [Range(0.5f, 2f)] public float pitchMin = 0.9f; // Tono mínimo
    [Range(0.5f, 2f)] public float pitchMax = 1.1f; // Tono máximo
    [Tooltip("Angular speed (rad/s) mapped to pitchMax.")]
    public float pitchRefSpeed = 3f; // Velocidad angular de referencia para tono máximo

    // Internals
    private Vector3 _axisWorld;           // Eje normalizado en espacio mundial
    private Rigidbody _rb;                // Rigidbody del objeto
    private AudioSource _srcPos;          // AudioSource para dirección positiva
    private AudioSource _srcNeg;          // AudioSource para dirección negativa
    private Coroutine _sampler;           // Coroutine para muestreo
    private Coroutine _fadePos;           // Coroutine para fade positivo
    private Coroutine _fadeNeg;           // Coroutine para fade negativo
    private float _lastAboveTimePos;      // Último tiempo por encima del umbral positivo
    private float _lastAboveTimeNeg;      // Último tiempo por encima del umbral negativo
    private bool _posActive;              // Si el audio positivo está activo
    private bool _negActive;              // Si el audio negativo está activo

    // Fallback rotation tracking when no Rigidbody
    private Quaternion _prevRot;          // Rotación previa para tracking sin Rigidbody

    void Awake()
    {
        // Inicializar componentes y configurar AudioSources
        _rb = GetComponent<Rigidbody>();

        // Dos fuentes independientes para crossfade
        _srcPos = gameObject.AddComponent<AudioSource>();
        _srcNeg = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { _srcPos, _srcNeg })
        {
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 1f; // 3D por defecto, ajustar en inspector si es necesario
            s.volume = 0f;
        }
        _srcPos.clip = positiveClip;
        _srcNeg.clip = negativeClip;

        _prevRot = transform.rotation;
        RecomputeAxis(); // Calcular el eje inicial
    }

    void OnValidate()
    {
        // Mantener umbrales razonables
        if (stopThreshold > startThreshold) stopThreshold = startThreshold * 0.8f;
        if (sampleRateHz < 4f) sampleRateHz = 4f;
    }

    void OnEnable()
    {
        RecomputeAxis(); // Recalcular eje al habilitar
        if (_sampler != null) StopCoroutine(_sampler);
        _sampler = StartCoroutine(SampleLoop()); // Iniciar muestreo
    }

    void OnDisable()
    {
        if (_sampler != null) StopCoroutine(_sampler); // Detener muestreo
        HardStop(); // Detener audio abruptamente
    }

    public void RecomputeAxis()
    {
        // Calcular el eje de rotación basado en axisSource y axisChoice
        switch (axisSource)
        {
            case AxisSource.LocalAxis:
                // Usar ejes locales del objeto
                _axisWorld = axisChoice switch
                {
                    AxisChoice.Forward => transform.forward, // Eje forward local
                    AxisChoice.Up => transform.up, // Eje up local
                    AxisChoice.Right => transform.right, // Eje right local
                    _ => transform.up // Por defecto up
                };
                break;

            case AxisSource.WorldAxis:
                // Usar ejes mundiales
                _axisWorld = axisChoice switch
                {
                    AxisChoice.X => Vector3.right, // Eje X mundial
                    AxisChoice.Y => Vector3.up, // Eje Y mundial
                    AxisChoice.Z => Vector3.forward, // Eje Z mundial
                    _ => Vector3.up // Por defecto up
                };
                break;

            case AxisSource.FromTwoPoses:
                // Calcular eje desde dos poses
                if (poseA != null && poseB != null)
                {
                    // Calcular la rotación más corta de A a B, extraer eje
                    Quaternion dQ = poseB.rotation * Quaternion.Inverse(poseA.rotation);
                    dQ.ToAngleAxis(out float angleDeg, out Vector3 axis);
                    if (axis.sqrMagnitude > 1e-6f)
                        _axisWorld = axis.normalized; // Ya en mundo ya que poseA/poseB son rotaciones mundiales
                    else
                        _axisWorld = transform.up; // Fallback
                }
                else
                {
                    _axisWorld = transform.up; // Fallback si poses nulas
                }
                break;
        }

        if (_axisWorld.sqrMagnitude < 1e-6f) _axisWorld = Vector3.up; // Evitar eje cero
        _axisWorld.Normalize(); // Normalizar el eje
    }

    private IEnumerator SampleLoop()
    {
        // Bucle principal de muestreo para controlar audio basado en velocidad angular
        float dt = Mathf.Max(0.02f, 1f / Mathf.Max(1f, sampleRateHz));
        while (true)
        {
            // Velocidad angular firmada alrededor del eje elegido
            float signedRadPerSec = GetSignedAngularSpeed(dt);

            // Manejo de dirección positiva
            if (signedRadPerSec >= startThreshold && positiveClip != null)
            {
                _lastAboveTimePos = Time.time; // Registrar tiempo de actividad
                if (!_posActive)
                {
                    _posActive = true; // Marcar como activo
                    StartFade(_srcPos, ref _fadePos, _srcPos.volume, maxVolume, fadeDuration, startIfNeeded: true); // Iniciar fade in
                }
                // Si pos está activo pero neg está sonando, crossfade away
                if (_negActive)
                {
                    _negActive = false; // Desactivar negativo
                    StartFade(_srcNeg, ref _fadeNeg, _srcNeg.volume, 0f, fadeDuration, startIfNeeded: false, stopAtEnd: true); // Fade out negativo
                }
                if (pitchBySpeed)
                {
                    // Calcular y aplicar tono basado en velocidad
                    float t = Mathf.Clamp01(Mathf.Abs(signedRadPerSec) / Mathf.Max(0.0001f, pitchRefSpeed));
                    _srcPos.pitch = Mathf.Lerp(pitchMin, pitchMax, t);
                }
            }
            else if (_posActive && (Time.time - _lastAboveTimePos) > idleTimeout)
            {
                // Fade out si ha estado inactivo demasiado tiempo
                _posActive = false;
                StartFade(_srcPos, ref _fadePos, _srcPos.volume, 0f, fadeDuration, startIfNeeded: false, stopAtEnd: true);
            }

            // Manejo de dirección negativa
            if (signedRadPerSec <= -startThreshold && negativeClip != null)
            {
                _lastAboveTimeNeg = Time.time; // Registrar tiempo de actividad
                if (!_negActive)
                {
                    _negActive = true; // Marcar como activo
                    StartFade(_srcNeg, ref _fadeNeg, _srcNeg.volume, maxVolume, fadeDuration, startIfNeeded: true); // Iniciar fade in
                }
                if (_posActive)
                {
                    _posActive = false; // Desactivar positivo
                    StartFade(_srcPos, ref _fadePos, _srcPos.volume, 0f, fadeDuration, startIfNeeded: false, stopAtEnd: true); // Fade out positivo
                }
                if (pitchBySpeed)
                {
                    // Calcular y aplicar tono basado en velocidad
                    float t = Mathf.Clamp01(Mathf.Abs(signedRadPerSec) / Mathf.Max(0.0001f, pitchRefSpeed));
                    _srcNeg.pitch = Mathf.Lerp(pitchMin, pitchMax, t);
                }
            }
            else if (_negActive && (Time.time - _lastAboveTimeNeg) > idleTimeout)
            {
                // Fade out si ha estado inactivo demasiado tiempo
                _negActive = false;
                StartFade(_srcNeg, ref _fadeNeg, _srcNeg.volume, 0f, fadeDuration, startIfNeeded: false, stopAtEnd: true);
            }

            yield return new WaitForSeconds(dt); // Esperar al siguiente muestreo
        }
    }

    private float GetSignedAngularSpeed(float dt)
    {
        // Calcular velocidad angular firmada alrededor del eje
        if (_rb != null)
        {
            // Usar velocidad angular del Rigidbody (ya en rad/s)
            float sign = Mathf.Sign(Vector3.Dot(_rb.angularVelocity, _axisWorld)); // Determinar dirección positiva/negativa
            return sign * _rb.angularVelocity.magnitude; // Aplicar signo a la magnitud
        }
        else
        {
            // Calcular delta de rotación del transform
            Quaternion current = transform.rotation; // Obtener rotación actual
            Quaternion dq = current * Quaternion.Inverse(_prevRot); // Calcular diferencia
            dq.ToAngleAxis(out float angleDeg, out Vector3 axis);   // Extraer ángulo y eje
            _prevRot = current; // Actualizar para siguiente frame

            if (axis.sqrMagnitude < 1e-12f) return 0f; // Sin cambio de rotación

            axis.Normalize(); // Normalizar el eje
            float angleRad = Mathf.Deg2Rad * Mathf.Abs(angleDeg); // Convertir grados a radianes
            float sign = Mathf.Sign(Vector3.Dot(axis, _axisWorld)); // Determinar signo basado en eje
            return sign * (angleRad / Mathf.Max(1e-6f, dt)); // Velocidad angular en rad/s
        }
    }

    private void StartFade(AudioSource src, ref Coroutine handle, float from, float to, float dur, bool startIfNeeded, bool stopAtEnd = false)
    {
        // Iniciar fade para un AudioSource
        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(FadeCo(src, from, to, dur, startIfNeeded, stopAtEnd));
    }

    private IEnumerator FadeCo(AudioSource src, float from, float to, float dur, bool startIfNeeded, bool stopAtEnd)
    {
        // Coroutine para fade de volumen
        if (startIfNeeded && src.clip != null && !src.isPlaying)
            src.Play(); // Iniciar reproducción si es necesario

        dur = Mathf.Max(0.01f, dur); // Asegurar duración mínima
        float t0 = Time.realtimeSinceStartup; // Tiempo de inicio

        while (true)
        {
            float a = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / dur); // Progreso del fade (0 a 1)
            src.volume = Mathf.Lerp(from, to, a); // Interpolar volumen
            if (a >= 1f) break; // Fade completo
            yield return null; // Esperar siguiente frame
        }

        if (stopAtEnd && to <= 0.0001f)
        {
            src.volume = 0f; // Asegurar volumen cero
            src.Stop(); // Detener reproducción
        }
    }

    public void HardStop()
    {
        // Detener todos los fades y audio abruptamente
        if (_fadePos != null) StopCoroutine(_fadePos);
        if (_fadeNeg != null) StopCoroutine(_fadeNeg);
        if (_srcPos) { _srcPos.Stop(); _srcPos.volume = 0f; }
        if (_srcNeg) { _srcNeg.Stop(); _srcNeg.volume = 0f; }
        _posActive = _negActive = false;
    }
}
