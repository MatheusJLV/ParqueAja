using System.Collections;
using System.Reflection;
using UnityEngine;

// Sistema de control de varita con efectos de partículas Chidori
// Gestiona sistemas de partículas, iluminación, audio y detección de empuje para activación/desactivación
public class WandPS : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem thinPS;   // Sistema de partículas delgadas del Chidori
    [SerializeField] private ParticleSystem thickPS;  // Sistema de partículas gruesas del Chidori

    [Header("Light control")]
    [SerializeField] private LightManager lightManager;   // Administrador de luces de la escena
    [SerializeField] private bool dimInsteadOfOff = true; // true = atenuar, false = apagar completamente
    [Range(0f, 1f)]
    [SerializeField] private float dimIntensity = 0.3f;   // Intensidad al atenuar (cuando dimInsteadOfOff = true)
    [Range(0f, 1f)]
    [SerializeField] private float restoreIntensity = 1f; // Intensidad a restaurar en TurnOff()

    [Header("Thrust detection")]
    [SerializeField] private Vector3 localThrustAxis = Vector3.right; // Eje de empuje local (flecha roja = +X)
    [SerializeField] private Transform head;                          // Referencia a la cabeza del jugador
    [SerializeField] private float velocityThreshold = 0.9f;          // Velocidad mínima total
    [SerializeField] private float axialVelocityThreshold = 0.4f;     // Velocidad mínima en el eje de empuje
    [Range(0f, 1f)][SerializeField] private float minAlignment = 0.7f; // Alineación mínima requerida
    [SerializeField] private float awayFromHeadDotMin = 0.0f;          // Producto punto mínimo para alejarse de la cabeza
    [Range(0.01f, 1f)][SerializeField] private float velocitySmoothing = 0.8f; // Suavizado de velocidad
    [SerializeField] private float sampleInterval = 0.015f;            // Intervalo de muestreo en segundos
    [SerializeField] private float cooldown = 0.12f;                   // Tiempo de espera entre activaciones
    [SerializeField] private bool startOn = true;                      // Iniciar activado al habilitar

    private Rigidbody rb;           // Rigidbody para obtener velocidad física
    private Coroutine watchRoutine;  // Corrutina de vigilancia de empuje
    private Vector3 lastPosWS;       // Última posición en espacio mundial
    private Vector3 velSmoothed;     // Velocidad suavizada
    private float cooldownTimer;     // Temporizador de enfriamiento

    [SerializeField] private AudioSource chidoriAS;          // Audio de inicio del Chidori (reproducir una vez)
    [SerializeField] private MusicManagerScript musicManager; // Control de música de fondo
    [SerializeField] private AudioSource staticAS;            // Audio de ruido estático

    [SerializeField] private float thrustDwellTime = 1.0f; // Segundos para mantener umbrales de empuje
    private float thrustDwellCounter = 0f;                  // Contador de tiempo sostenido del empuje                  // Contador de tiempo sostenido del empuje

    // Enciende el Chidori: activa partículas, atenúa luces, reproduce audio y música, e inicia vigilancia
    public void TurnOn()
    {
        if (thinPS && !thinPS.isPlaying) thinPS.Play(true);
        if (thickPS && !thickPS.isPlaying) thickPS.Play(true);

        // Luz: atenuar o apagar al activar
        if (lightManager)
        {
            lightManager.SetDark_Mode2();
            float target = dimInsteadOfOff ? Mathf.Clamp01(dimIntensity) : 0f;
            TryLightSetIntensityOrToggle(target);
        }

        // Reproducir audio del Chidori una vez
        // Reproducir audio del Chidori una vez
        if (chidoriAS != null)
        {
            if (chidoriAS.clip != null)
                chidoriAS.PlayOneShot(chidoriAS.clip);
            else
                chidoriAS.Play(); // alternativa si no hay clip especificado para one-shot
        }

        // Reproducir música temática de Naruto
        if (musicManager != null)
            musicManager.PlaySongByName("Naruto - Bad Situation");

        StartWatcher();
    }

    // Apaga el Chidori: detiene partículas, restaura luces, detiene audio y música, y para vigilancia
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

        // Luz: restaurar al desactivar
        if (lightManager)
        {
            lightManager.SetBright_Mode2();
            float target = Mathf.Clamp01(restoreIntensity);
            TryLightSetIntensityOrToggle(target);
        }

        StopWatcher();
    }

    // Encendido inicial: activa partículas y atenúa luces sin audio/música
    public void TurnOnFirst()
    {
        if (thinPS && !thinPS.isPlaying) thinPS.Play(true);
        if (thickPS && !thickPS.isPlaying) thickPS.Play(true);

        // Luz: atenuar o apagar al activar
        if (lightManager)
        {
            //lightManager.SetDark();
            float target = dimInsteadOfOff ? Mathf.Clamp01(dimIntensity) : 0f;
            TryLightSetIntensityOrToggle(target);
        }

        StartWatcher();
    }

    // Apagado inicial: detiene partículas y restaura luces sin detener audio/música
    public void TurnOffFirst()
    {
        if (thinPS && thinPS.isPlaying) thinPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (thickPS && thickPS.isPlaying) thickPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Luz: restaurar al desactivar
        if (lightManager)
        {
            //lightManager.SetBright();
            float target = Mathf.Clamp01(restoreIntensity);
            TryLightSetIntensityOrToggle(target);
        }

        StopWatcher();
    }

    // Inicializa el componente Rigidbody
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Activa el Chidori al habilitar el componente si startOn está activado
    private void OnEnable()
    {
        if (startOn) TurnOnFirst();
    }

    // Detiene la vigilancia al deshabilitar el componente
    private void OnDisable()
    {
        StopWatcher();
    }

    // Inicia la corrutina de vigilancia de empuje
    private void StartWatcher()
    {
        if (watchRoutine != null) return;
        lastPosWS = transform.position;
        velSmoothed = Vector3.zero;
        cooldownTimer = 0f;
        watchRoutine = StartCoroutine(WatchThrust());
    }

    // Detiene la corrutina de vigilancia de empuje
    private void StopWatcher()
    {
        if (watchRoutine == null) return;
        StopCoroutine(watchRoutine);
        watchRoutine = null;
    }

    // Corrutina que vigila continuamente el empuje de la varita para detectar movimiento de apagado
    private IEnumerator WatchThrust()
    {
        while (isActiveAndEnabled)
        {
            Vector3 v;
            // Obtener velocidad del Rigidbody o calcular manualmente
            if (rb != null && !rb.isKinematic)
            {
                v = rb.linearVelocity;
            }
            else
            {
                Vector3 pos = transform.position;
                v = (pos - lastPosWS) / Mathf.Max(sampleInterval, 1e-4f);
                lastPosWS = pos;
            }

            velSmoothed = Vector3.Lerp(velSmoothed, v, velocitySmoothing);

            // Calcular velocidad axial y alineación con el eje de empuje
            Vector3 axisWs = transform.TransformDirection(localThrustAxis).normalized;
            float vMag = velSmoothed.magnitude;
            float vAxialSigned = Vector3.Dot(velSmoothed, axisWs);
            float vAxialNeg = Mathf.Max(0f, -vAxialSigned);
            float alignment = (vMag > 0.0001f) ? (vAxialNeg / vMag) : 0f;

            // Verificar si se cumplen todos los umbrales de empuje
            bool passAxialSpeed = vAxialNeg >= axialVelocityThreshold;
            bool passAlignment = alignment >= minAlignment;
            bool passOverallSpeed = vMag >= velocityThreshold;
            bool passAwayFromHead = true;

            // ... your away-from-head check here

            // Mantener contador si se cumplen todos los umbrales
            if (passAxialSpeed && passAlignment && passOverallSpeed && passAwayFromHead)
            {
                thrustDwellCounter += sampleInterval;
            }
            else
            {
                thrustDwellCounter = 0f; // reiniciar si no se sostiene
            }

            // Activar solo si se mantuvo el tiempo suficiente y el enfriamiento expiró
            if (cooldownTimer <= 0f && thrustDwellCounter >= thrustDwellTime)
            {
                TurnOff();
                cooldownTimer = cooldown;
                thrustDwellCounter = 0f;
            }

            yield return new WaitForSeconds(sampleInterval);
        }
    }

    // Intenta configurar la intensidad de luz usando reflexión para compatibilidad con diferentes LightManagers
    private void TryLightSetIntensityOrToggle(float target)
    {
        // Preferir SetIntensity(float)
        MethodInfo setIntensity = lightManager.GetType().GetMethod("SetIntensity", new[] { typeof(float) });
        if (setIntensity != null)
        {
            setIntensity.Invoke(lightManager, new object[] { target });
            return;
        }

        // Alternativa: usar TurnOff()/TurnOn()
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

        // Último recurso: DecreaseIntensity()/IncreaseIntensity()
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
