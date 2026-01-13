using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
// Reproduce audio cuando el objeto gira alrededor de un eje específico, con distintos clips para cada dirección
public class DirectionalRotationAudio : MonoBehaviour
{
    // Define cómo se determina el eje de rotación a monitorear
    public enum AxisSource
    {
        LocalAxis,   // use this object's forward/up/right
        WorldAxis,   // use world X/Y/Z
        FromTwoPoses // compute axis from Pose A -> Pose B
    }

    // Opciones de eje disponibles según el AxisSource
    public enum AxisChoice { Forward, Up, Right, X, Y, Z }

    [Header("Axis Detection")]
    // Fuente del eje de rotación a monitorear
    public AxisSource axisSource = AxisSource.LocalAxis;
    // Eje específico dentro de la fuente seleccionada
    public AxisChoice axisChoice = AxisChoice.Up;

    // Transforms para calcular eje cuando se usa modo FromTwoPoses
    [Tooltip("Pose A and Pose B used to infer rotation axis (FromTwoPoses mode).")
    ]
    public Transform poseA;
    public Transform poseB;

    [Header("Clips")]
    // Clips de audio para cada dirección de rotación
    [Tooltip("Played while rotating in + direction around the axis.")]
    public AudioClip positiveClip;
    [Tooltip("Played while rotating in - direction around the axis.")]
    public AudioClip negativeClip;

    [Header("Levels")]
    // Volumen máximo cuando un clip está activo
    [Range(0f, 1f)] public float maxVolume = 1f;

    [Header("Thresholds")]
    [Tooltip("Angular speed (rad/s) to start sound.")]
    public float startThreshold = 0.4f;
    [Tooltip("Angular speed (rad/s) to stop sound (hysteresis).")]
    public float stopThreshold = 0.25f;
    [Tooltip("If below stop threshold for this long, fade out.")]
    public float idleTimeout = 0.25f;

    [Header("Timing")]
    [Tooltip("How many times per second to sample (lower = cheaper).")]
    public float sampleRateHz = 12f;
    [Tooltip("Seconds for crossfades between clips.")]
    public float fadeDuration = 0.2f;

    [Header("Optional Pitch Mapping")]
    public bool pitchBySpeed = false;
    [Range(0.5f, 2f)] public float pitchMin = 0.9f;
    [Range(0.5f, 2f)] public float pitchMax = 1.1f;
    [Tooltip("Angular speed (rad/s) mapped to pitchMax.")]
    public float pitchRefSpeed = 3f;

    // Variables privadas para gestión interna del estado
    // Almacena el eje normalizado en espacio mundial
    private Vector3 _axisWorld;           // normalized world-space axis
    // Referencia al Rigidbody para obtener velocidad angular
    private Rigidbody _rb;
    // Fuentes de audio para ambas direcciones de rotación
    private AudioSource _srcPos;          // + direction
    private AudioSource _srcNeg;          // - direction
    // Corrutinas activas
    private Coroutine _sampler;
    private Coroutine _fadePos;
    private Coroutine _fadeNeg;
    // Timestamps para control de timeout
    private float _lastAboveTimePos;
    private float _lastAboveTimeNeg;
    // Banderas que indican si cada fuente está reproduciendo
    private bool _posActive;
    private bool _negActive;

    // Fallback rotation tracking when no Rigidbody
    private Quaternion _prevRot;

    // Inicialización del componente
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Two independent sources so we can crossfade
        _srcPos = gameObject.AddComponent<AudioSource>();
        _srcNeg = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { _srcPos, _srcNeg })
        {
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 1f; // 3D by default, tweak in inspector if desired
            s.volume = 0f;
        }
        _srcPos.clip = positiveClip;
        _srcNeg.clip = negativeClip;

        _prevRot = transform.rotation;
        RecomputeAxis();
    }

    // Validación de parámetros en el inspector
    void OnValidate()
    {
        // Keep thresholds sane
        if (stopThreshold > startThreshold) stopThreshold = startThreshold * 0.8f;
        if (sampleRateHz < 4f) sampleRateHz = 4f;
    }

    // Activación del componente
    void OnEnable()
    {
        RecomputeAxis();
        if (_sampler != null) StopCoroutine(_sampler);
        _sampler = StartCoroutine(SampleLoop());
    }

    // Desactivación del componente
    void OnDisable()
    {
        if (_sampler != null) StopCoroutine(_sampler);
        HardStop();
    }

    // Calcula o recalcula el eje de rotación según la configuración
    public void RecomputeAxis()
    {
        switch (axisSource)
        {
            case AxisSource.LocalAxis:
                _axisWorld = axisChoice switch
                {
                    AxisChoice.Forward => transform.forward,
                    AxisChoice.Up => transform.up,
                    AxisChoice.Right => transform.right,
                    _ => transform.up
                };
                break;

            case AxisSource.WorldAxis:
                _axisWorld = axisChoice switch
                {
                    AxisChoice.X => Vector3.right,
                    AxisChoice.Y => Vector3.up,
                    AxisChoice.Z => Vector3.forward,
                    _ => Vector3.up
                };
                break;

            case AxisSource.FromTwoPoses:
                if (poseA != null && poseB != null)
                {
                    // Compute shortest rotation from A to B, extract axis
                    Quaternion dQ = poseB.rotation * Quaternion.Inverse(poseA.rotation);
                    dQ.ToAngleAxis(out float angleDeg, out Vector3 axis);
                    if (axis.sqrMagnitude > 1e-6f)
                        _axisWorld = axis.normalized; // already in world since poseA/poseB are world-space rotations
                    else
                        _axisWorld = transform.up;
                }
                else
                {
                    _axisWorld = transform.up;
                }
                break;
        }

        if (_axisWorld.sqrMagnitude < 1e-6f) _axisWorld = Vector3.up;
        _axisWorld.Normalize();
    }

    // Loop principal que monitorea velocidad angular y gestiona transiciones de audio
    private IEnumerator SampleLoop()
    {
        float dt = Mathf.Max(0.02f, 1f / Mathf.Max(1f, sampleRateHz));
        while (true)
        {
            // Obtiene velocidad angular con signo para determinar dirección
            float signedRadPerSec = GetSignedAngularSpeed(dt);

            // Lógica para dirección positiva: verifica umbral de inicio
            // Si la velocidad supera startThreshold, activa el clip positivo
            if (signedRadPerSec >= startThreshold && positiveClip != null)
            {
                _lastAboveTimePos = Time.time;
                if (!_posActive)
                {
                    _posActive = true;
                    StartFade(_srcPos, ref _fadePos, _srcPos.volume, maxVolume, fadeDuration, startIfNeeded: true);
                }
                // If pos is active but neg is playing, crossfade away
                if (_negActive)
                {
                    _negActive = false;
                    StartFade(_srcNeg, ref _fadeNeg, _srcNeg.volume, 0f, fadeDuration, startIfNeeded: false, stopAtEnd: true);
                }
                if (pitchBySpeed)
                {
                    float t = Mathf.Clamp01(Mathf.Abs(signedRadPerSec) / Mathf.Max(0.0001f, pitchRefSpeed));
                    _srcPos.pitch = Mathf.Lerp(pitchMin, pitchMax, t);
                }
            }
            else if (_posActive && (Time.time - _lastAboveTimePos) > idleTimeout)
            {
                _posActive = false;
                StartFade(_srcPos, ref _fadePos, _srcPos.volume, 0f, fadeDuration, startIfNeeded: false, stopAtEnd: true);
            }

            // Lógica para dirección negativa: verifica velocidad en dirección opuesta
            // Si la velocidad es negativa y supera el umbral, activa el clip negativo
            // Negative direction handling
            if (signedRadPerSec <= -startThreshold && negativeClip != null)
            {
                _lastAboveTimeNeg = Time.time;
                if (!_negActive)
                {
                    _negActive = true;
                    StartFade(_srcNeg, ref _fadeNeg, _srcNeg.volume, maxVolume, fadeDuration, startIfNeeded: true);
                }
                if (_posActive)
                {
                    _posActive = false;
                    StartFade(_srcPos, ref _fadePos, _srcPos.volume, 0f, fadeDuration, startIfNeeded: false, stopAtEnd: true);
                }
                if (pitchBySpeed)
                {
                    float t = Mathf.Clamp01(Mathf.Abs(signedRadPerSec) / Mathf.Max(0.0001f, pitchRefSpeed));
                    _srcNeg.pitch = Mathf.Lerp(pitchMin, pitchMax, t);
                }
            }
            else if (_negActive && (Time.time - _lastAboveTimeNeg) > idleTimeout)
            {
                _negActive = false;
                StartFade(_srcNeg, ref _fadeNeg, _srcNeg.volume, 0f, fadeDuration, startIfNeeded: false, stopAtEnd: true);
            }

            yield return new WaitForSeconds(dt);
        }
    }
    // Obtiene la velocidad angular con signo alrededor del eje monitorizado
    private float GetSignedAngularSpeed(float dt)
    {
        // Si hay Rigidbody, obtiene su velocidad angular directamente
        // Si no, calcula velocidad a partir del cambio de rotación del frame
        if (_rb != null)
        {
            // Rigidbody.angularVelocity is already rad/s in world space
            float sign = Mathf.Sign(Vector3.Dot(_rb.angularVelocity, _axisWorld));
            return sign * _rb.angularVelocity.magnitude;
        }
        else
        {
            // Fallback cuando no hay Rigidbody: calcula velocidad angular manual
            // Compara rotación actual con la del frame anterior
            // Transform delta
            Quaternion current = transform.rotation;
            Quaternion dq = current * Quaternion.Inverse(_prevRot);
            dq.ToAngleAxis(out float angleDeg, out Vector3 axis);   // angle in degrees this frame
            _prevRot = current;

            if (axis.sqrMagnitude < 1e-12f) return 0f;

            axis.Normalize();
            float angleRad = Mathf.Deg2Rad * Mathf.Abs(angleDeg);
            float sign = Mathf.Sign(Vector3.Dot(axis, _axisWorld));
            return sign * (angleRad / Mathf.Max(1e-6f, dt)); // rad/s
        }
    }

    // Inicia una transición suave de volumen para una fuente de audio
    private void StartFade(AudioSource src, ref Coroutine handle, float from, float to, float dur, bool startIfNeeded, bool stopAtEnd = false)
    {
        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(FadeCo(src, from, to, dur, startIfNeeded, stopAtEnd));
    }

    // Corrutina que interpola el volumen de forma suave
    private IEnumerator FadeCo(AudioSource src, float from, float to, float dur, bool startIfNeeded, bool stopAtEnd)
    {
        if (startIfNeeded && src.clip != null && !src.isPlaying)
            src.Play();

        dur = Mathf.Max(0.01f, dur);
        float t0 = Time.realtimeSinceStartup;

        while (true)
        {
            float a = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / dur);
            src.volume = Mathf.Lerp(from, to, a);
            if (a >= 1f) break;
            yield return null;
        }

        if (stopAtEnd && to <= 0.0001f)
        {
            src.volume = 0f;
            src.Stop();
        }
    }

    // Detiene inmediatamente todos los sonidos y transiciones
    public void HardStop()
    {
        if (_fadePos != null) StopCoroutine(_fadePos);
        if (_fadeNeg != null) StopCoroutine(_fadeNeg);
        if (_srcPos) { _srcPos.Stop(); _srcPos.volume = 0f; }
        if (_srcNeg) { _srcNeg.Stop(); _srcNeg.volume = 0f; }
        _posActive = _negActive = false;
    }
}
