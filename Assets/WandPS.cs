using System.Collections;
using System.Reflection;
using UnityEngine;

/*
WandPS: controla los ParticleSystems "chidori" en la varita y detecta
empujes (thrust) para disparar/desactivar efectos, audio y luces.
Funcionalidad principal:
- Encender/apagar PS y sonidos.
- Manejar dim/restore de luces mediante LightManager.
- Vigilar movimiento/velocidad local para detectar un thrust sostenido.
*/
public class WandPS : MonoBehaviour
{
    [Header("Particle Systems")]
    //ParticleSystem fino (trail / detalle)
    [SerializeField] private ParticleSystem thinPS;
    //ParticleSystem grueso (cuerpo)
    [SerializeField] private ParticleSystem thickPS;
    [Header("Light control")]
    //Referencia al manager de luces en la escena
    [SerializeField] private LightManager lightManager;   // assign from scene
    //Si true atenuar en vez de apagar por completo
    [SerializeField] private bool dimInsteadOfOff = true; // true = dim, false = full off
    [Range(0f, 1f)]
    //Intensidad objetivo al atenuar
    [SerializeField] private float dimIntensity = 0.3f;   // used when dimInsteadOfOff = true
    [Range(0f, 1f)]
    //Intensidad de restauración al apagar el Chidori
    [SerializeField] private float restoreIntensity = 1f; // used on TurnOff()

    [Header("Thrust detection")]
    //Eje local que representa la dirección de empuje (ej. +X)
    [SerializeField] private Vector3 localThrustAxis = Vector3.right; // red arrow = +X
    //Transform de la cabeza (para checks relativos si necesario)
    [SerializeField] private Transform head;
    //Velocidad minima global para considerar thrust
    [SerializeField] private float velocityThreshold = 0.9f;
    //Componente axial mínima (negativa) para considerar empuje hacia adelante
    [SerializeField] private float axialVelocityThreshold = 0.4f;
    //Alineación mínima entre velocidad y eje local para validar dirección
    [Range(0f, 1f)][SerializeField] private float minAlignment = 0.7f;
    //Dot product mínimo para asegurar que se aleja de la cabeza (si se usa)
    [SerializeField] private float awayFromHeadDotMin = 0.0f;
    //Suavizado de la velocidad medida
    [Range(0.01f, 1f)][SerializeField] private float velocitySmoothing = 0.8f;
    //Intervalo de muestreo para la coroutine
    [SerializeField] private float sampleInterval = 0.015f;
    //Cooldown entre detecciones
    [SerializeField] private float cooldown = 0.12f;
    //StartOn: arrancar watcher al habilitar
    [SerializeField] private bool startOn = true;

    /*
    Rigidbody opcional usado para leer linearVelocity directamente
    Coroutine que vigila el thrust
    Última posición world usada al calcular velocidad si no hay rigidbody
    Velocidad suavizada
    Timer de cooldown restante
    */
    private Rigidbody rb;
    private Coroutine watchRoutine;
    private Vector3 lastPosWS;
    private Vector3 velSmoothed;
    private float cooldownTimer;

    [SerializeField] private AudioSource chidoriAS;          // play once when Chidori starts
    [SerializeField] private MusicManagerScript musicManager; // background music control
    [SerializeField] private AudioSource staticAS;

    [SerializeField] private float thrustDwellTime = 1.0f; // seconds to sustain thresholds
    private float thrustDwellCounter = 0f;

    /*
    TurnOn: enciende PS, maneja luces y audio, y arranca el watcher de movimiento
    Secuencia:
    - Reproducir PS (thin y thick)
    - Configurar luces en oscuridad (dim o apagado)
    - Reproducir audio de Chidori
    - Reproducir música de fondo
    - Iniciar vigilancia de movimiento
    */
    public void TurnOn()
    {
        if (thinPS && !thinPS.isPlaying) thinPS.Play(true);
        if (thickPS && !thickPS.isPlaying) thickPS.Play(true);

        // Light: dim or turn off on activation
        if (lightManager)
        {
            lightManager.SetDark_Mode2();
            float target = dimInsteadOfOff ? Mathf.Clamp01(dimIntensity) : 0f;
            TryLightSetIntensityOrToggle(target);
        }

        if (chidoriAS != null)
        {
            if (chidoriAS.clip != null)
                chidoriAS.PlayOneShot(chidoriAS.clip);
            else
                chidoriAS.Play(); // fallback if no clip specified for one-shot
        }

        if (musicManager != null)
            musicManager.PlaySongByName("Naruto - Bad Situation");

        StartWatcher();
    }
    /*
    TurnOff: apaga PS, detiene audios, restaura luces y para el watcher
    Secuencia:
    - Detener PS (thin y thick)
    - Parar audios (Chidori y Static)
    - Reproducir música aleatoria
    - Restaurar luces al brillo original
    - Detener vigilancia
    */

    public void TurnOff()
    {
        if (thinPS && thinPS.isPlaying) thinPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (thickPS && thickPS.isPlaying) thickPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (chidoriAS != null && chidoriAS.isPlaying)
            chidoriAS.Stop();

        if (staticAS != null && staticAS.isPlaying)
            staticAS.Stop();

        if (musicManager != null)
            musicManager.PlayRandomMusic();

        // Light: restore on deactivate
        if (lightManager)
        {
            lightManager.SetBright_Mode2();
            float target = Mathf.Clamp01(restoreIntensity);
            TryLightSetIntensityOrToggle(target);
        }

        StopWatcher();
    }

    /*
    TurnOnFirst: versión ligera de TurnOn usada en inicialización OnEnable
    - Reproducir PS (thin y thick)
    - Configurar luces en oscuridad
    - NO reproduce música de fondo específica
    - Iniciar vigilancia de movimiento
    */
    public void TurnOnFirst()
    {
        if (thinPS && !thinPS.isPlaying) thinPS.Play(true);
        if (thickPS && !thickPS.isPlaying) thickPS.Play(true);

        // Light: dim or turn off on activation
        if (lightManager)
        {
            //lightManager.SetDark();
            float target = dimInsteadOfOff ? Mathf.Clamp01(dimIntensity) : 0f;
            TryLightSetIntensityOrToggle(target);
        }

        StartWatcher();
    }
    /*
    TurnOffFirst: versión ligera de TurnOff usada en inicialización OnEnable
    - Detener PS (thin y thick)
    - Restaurar luces al brillo original
    - NO cambia música
    - Detener vigilancia
    */
    public void TurnOffFirst()
    {
        if (thinPS && thinPS.isPlaying) thinPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (thickPS && thickPS.isPlaying) thickPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Light: restore on deactivate
        if (lightManager)
        {
            //lightManager.SetBright();
            float target = Mathf.Clamp01(restoreIntensity);
            TryLightSetIntensityOrToggle(target);
        }

        StopWatcher();
    }

    //Awake: cachear componentes necesarios
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    //OnEnable: arrancar si startOn = true
    private void OnEnable()
    {
        if (startOn) TurnOnFirst();
    }

    //OnDisable: asegurar que el watcher se detenga
    private void OnDisable()
    {
        StopWatcher();
    }

    //StartWatcher: iniciar coroutine si no existe
    private void StartWatcher()
    {
        if (watchRoutine != null) return;
        lastPosWS = transform.position;
        velSmoothed = Vector3.zero;
        cooldownTimer = 0f;
        watchRoutine = StartCoroutine(WatchThrust());
    }

    //StopWatcher: detener coroutine si existe
    private void StopWatcher()
    {
        if (watchRoutine == null) return;
        StopCoroutine(watchRoutine);
        watchRoutine = null;
    }

    /*
    WatchThrust: coroutine principal que muestrea movimiento y decide
    si se cumple la condición de "thrust" sostenido para disparar acciones.
    - Usa Rigidbody.linearVelocity si hay rigidbody no kinematic.
    - Calcula componente axial contra localThrustAxis.
    - Valida thresholds y tiempo sostenido (dwell).
    - Aplica cooldown entre detecciones.
    */

    private IEnumerator WatchThrust()
    {
        while (isActiveAndEnabled)
        {
            Vector3 v;
            /*
            Obtener velocidad:
            - Si hay rigidbody no kinematic, usar linearVelocity directamente
            - Si no, aproximar por diferencia de posiciones sobre sampleInterval
            */
            if (rb != null && !rb.isKinematic)
            {
                // Uso directo de linearVelocity si está disponible
                v = rb.linearVelocity;
            }
            else
            {
                // Aproximación por diferencia de posiciones 
                Vector3 pos = transform.position;
                v = (pos - lastPosWS) / Mathf.Max(sampleInterval, 1e-4f);
                lastPosWS = pos;
            }

            // Suavizar lecturas para estabilidad
            velSmoothed = Vector3.Lerp(velSmoothed, v, velocitySmoothing);
            /*
            Calcular validaciones:
            - axisWs: eje local transformado a world space
            - vMag: magnitud de la velocidad suavizada
            - vAxialSigned: componente axial (puede ser positiva o negativa)
            - vAxialNeg: solo el componente negativo (empuje hacia adelante)
            - alignment: ratio de componente axial respecto a velocidad total
            */
            Vector3 axisWs = transform.TransformDirection(localThrustAxis).normalized;
            float vMag = velSmoothed.magnitude;
            float vAxialSigned = Vector3.Dot(velSmoothed, axisWs);
            float vAxialNeg = Mathf.Max(0f, -vAxialSigned);
            float alignment = (vMag > 0.0001f) ? (vAxialNeg / vMag) : 0f;

            bool passAxialSpeed = vAxialNeg >= axialVelocityThreshold;
            bool passAlignment = alignment >= minAlignment;
            bool passOverallSpeed = vMag >= velocityThreshold;
            bool passAwayFromHead = true;

            /*
            Acumular dwell counter si todas las condiciones se cumplen
            Resetear si alguna falla
            */

            if (passAxialSpeed && passAlignment && passOverallSpeed && passAwayFromHead)
            {
                thrustDwellCounter += sampleInterval;
            }
            else
            {
               //Resetear contador si no se sostiene la condición
                thrustDwellCounter = 0f; // reset if not sustained
            }

            //Trigger solo si se ha sostenido el tiempo requerido y cooldown expiró
            if (cooldownTimer <= 0f && thrustDwellCounter >= thrustDwellTime)
            {
                TurnOff();
                cooldownTimer = cooldown;
                thrustDwellCounter = 0f;
            }

            yield return new WaitForSeconds(sampleInterval);
        }
    }
    /*
    TryLightSetIntensityOrToggle: intenta varios métodos en LightManager para
    cambiar intensidad o alternar estado. Usa reflection para compatibilidad
    con distintas implementaciones de LightManager en el proyecto.
    Orden de intento:
    1. SetIntensity(float) - ideal si está disponible
    2. TurnOff()/TurnOn() - fallback booleano
    3. DecreaseIntensity()/IncreaseIntensity() - último recurso
    */
    private void TryLightSetIntensityOrToggle(float target)
    {
        // Prefer SetIntensity(float)
        MethodInfo setIntensity = lightManager.GetType().GetMethod("SetIntensity", new[] { typeof(float) });
        if (setIntensity != null)
        {
            setIntensity.Invoke(lightManager, new object[] { target });
            return;
        }

        //Retrocompatibilidad: TurnOff()/TurnOn()
        if (target <= 0.001f)
        {
            MethodInfo turnOff = lightManager.GetType().GetMethod("TurnOff", System.Type.EmptyTypes);
            if (turnOff != null)
            {
                turnOff.Invoke(lightManager, null);
                return;
            }
        }
        else
        {
            MethodInfo turnOn = lightManager.GetType().GetMethod("TurnOn", System.Type.EmptyTypes);
            if (turnOn != null)
            {
                turnOn.Invoke(lightManager, null);
                return;
            }
        }

        //Último recurso: DecreaseIntensity()/IncreaseIntensity()
        if (target <= 0.001f)
        {
            MethodInfo dec = lightManager.GetType().GetMethod("DecreaseIntensity", System.Type.EmptyTypes);
            if (dec != null) dec.Invoke(lightManager, null);
        }
        else
        {
            MethodInfo inc = lightManager.GetType().GetMethod("IncreaseIntensity", System.Type.EmptyTypes);
            if (inc != null) inc.Invoke(lightManager, null);
        }
    }
}
