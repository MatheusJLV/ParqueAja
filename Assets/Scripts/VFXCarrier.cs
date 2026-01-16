using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

// Portador de VFX que gestiona la transición de un efecto de carga a un efecto Chidori basado en orientación.
// Detecta colisiones con conductores, monitorea la dirección de apunte de la varita, y activa Chidori
// cuando la varita se orienta correctamente hacia la dirección objetivo.
public class VFXCarrier : MonoBehaviour
{
    public VisualEffect carrierVFX;                         // Efecto visual del generador/portador
    private Collider intruder1;                            // Colisionador del conductor detectado
    public bool isCharged = false;                         // Indica si el generador está cargado

    [Header("Chidori (target Particle Systems)")]
    [SerializeField] private ParticleSystem chidoriThinPS;   // Sistema de partículas Chidori fino (opcional)
    [SerializeField] private ParticleSystem chidoriThickPS;  // Sistema de partículas Chidori grueso (opcional)

    [Header("Orientation trigger")]
    [Tooltip("Local axis that represents the wand's pointing direction. Red arrow in the editor = +X.")]
    [SerializeField] private Vector3 localPointAxis = Vector3.right;      // Eje local de apunte de la varita

    [Tooltip("World direction the local axis should face to trigger. For 'X points UP', use Vector3.up.")]
    [SerializeField] private Vector3 targetWorldDirection = Vector3.up;   // Dirección objetivo en el mundo

    [Tooltip("Total cone angle around the target direction considered 'inside'. 120° => 60° half-angle.")]
    [Range(1f, 179f)]
    [SerializeField] private float coneAngle = 120f;                      // Ángulo del cono de activación

    [Tooltip("Extra degrees beyond the enter half-angle to release the trigger (prevents flicker).")]
    [SerializeField] private float hysteresis = 10f;                      // Zona muerta para evitar parpadeos

    [Tooltip("Seconds between orientation checks.")]
    [SerializeField] private float checkInterval = 0.10f;                 // Intervalo entre chequeos de orientación

    private float enterHalfAngle;                          // Semiángulo para entrar en el cono
    private float exitHalfAngle;                           // Semiángulo para salir del cono (con histéresis)
    private Coroutine watchRoutine;                        // Corrutina de vigilancia de orientación

    public WandPS wandPS;                                  // Referencia al script WandPS para controlar Chidori

    [SerializeField] private AudioSource staticAS;         // Fuente de audio para el bucle de ruido estático

    [Header("Auto-off")]
    [SerializeField] private bool enableAutoOff = true;                   // Habilitar apagado automático de Chidori
    [SerializeField] private float chidoriAutoOffSeconds = 5f;            // Segundos antes de apagar automáticamente
    private Coroutine _autoOffCo;                          // Corrutina de apagado automático
    private bool chidoriActive = false;                    // Indica si Chidori está actualmente activo

    // Inicializa el sistema, calcula los ángulos del cono y detiene los efectos iniciales
    private void Start()
    {
        if (carrierVFX != null)
        {
            // Asegurar que los PS estén detenidos al arranque (si se usan directamente)
            if (chidoriThickPS) chidoriThickPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (chidoriThinPS) chidoriThinPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            carrierVFX.Stop();
        }

        enterHalfAngle = coneAngle * 0.5f;
        exitHalfAngle = enterHalfAngle + Mathf.Abs(hysteresis);
    }

    // Enciende el generador cargado, inicia el audio estático y comienza a vigilar la orientación
    public void TurnOn()
    {
        isCharged = true;
        if (carrierVFX != null)
            carrierVFX.Play();

        // audio: iniciar bucle de ruido estático
        if (staticAS != null)
        {
            staticAS.loop = true;
            if (!staticAS.isPlaying) staticAS.Play();
        }

        StartWatchingOrientation();
    }

    // Apagado unificado del portador y Chidori
    public void TurnOff()
    {
        DisarmCarrier();
        DeactivateChidori();
    }

    // Alias para TurnOn (carga el generador)
    public void Charge() => TurnOn();

    // Descarga el generador cuando se detecta un conductor, preparando el atractor
    public void Discharge(Collider other)
    {
        if (!other.CompareTag("Conductor"))
            return;

        if (intruder1 != null && other == intruder1)
            return;

        if (intruder1 == null)
        {
            intruder1 = other;
            if (carrierVFX != null)
            {
                carrierVFX.SetBool("Atractor1", true);
                carrierVFX.SetVector3("IntruderPosition", intruder1.transform.position);
            }
        }
    }

    // Detecta el inicio del contacto con un conductor para descargar
    void OnTriggerEnter(Collider other)
    {
        if (!isCharged || !other.CompareTag("Conductor"))
            return;

        Discharge(other);
    }

    // Detecta la salida del contacto con un conductor y desactiva el atractor
    void OnTriggerExit(Collider other)
    {
        if (other == intruder1)
        {
            intruder1 = null;
            if (carrierVFX != null)
            {
                carrierVFX.SetBool("Atractor1", false);
                carrierVFX.SetVector3("IntruderPosition", Vector3.zero);
            }
        }
    }

    // Actualiza continuamente la posición del intruso en el VFX
    void Update()
    {
        if (intruder1 != null && carrierVFX != null)
            carrierVFX.SetVector3("IntruderPosition", intruder1.transform.position);
    }

    // Llamado cuando el objetivo está dentro del cono: desarma el portador, activa Chidori, inicia temporizador de apagado automático
    public void SwitchToChidoriNow()
    {
        // Desarma inmediatamente el portador (detiene VFX/audio/vigilancia)
        DisarmCarrier();

        // Enciende Chidori vía WandPS (método preferido)
        if (wandPS != null && wandPS.isActiveAndEnabled)
            wandPS.TurnOn();

        // (Opcional) Si deseas reproducir PS directamente en lugar de usar WandPS:
        // if (chidoriThinPS && !chidoriThinPS.isPlaying)  chidoriThinPS.Play(true);
        // if (chidoriThickPS && !chidoriThickPS.isPlaying) chidoriThickPS.Play(true);

        chidoriActive = true;

        // Iniciar apagado automático si está habilitado
        if (enableAutoOff)
        {
            if (_autoOffCo != null) StopCoroutine(_autoOffCo);
            _autoOffCo = StartCoroutine(AutoOffAfterDelay());
        }
    }

    // ===== Métodos auxiliares (separación limpia de responsabilidades) =====

    // Desarma el portador: detiene VFX, audio y vigilancia de orientación
    private void DisarmCarrier()
    {
        isCharged = false;

        // audio: detener bucle estático
        if (staticAS != null && staticAS.isPlaying)
            staticAS.Stop();

        intruder1 = null;
        if (carrierVFX != null)
        {
            carrierVFX.SetBool("Atractor1", false);
            carrierVFX.SetVector3("IntruderPosition", Vector3.zero);
            carrierVFX.Stop();
        }

        StopWatchingOrientation();
    }

    // Desactiva Chidori: detiene el temporizador, apaga WandPS y los sistemas de partículas
    private void DeactivateChidori()
    {
        // detener temporizador de apagado automático
        if (_autoOffCo != null)
        {
            StopCoroutine(_autoOffCo);
            _autoOffCo = null;
        }

        // Apagar vía WandPS
        if (wandPS != null && wandPS.isActiveAndEnabled)
            wandPS.TurnOff();

        // (Opcional) También asegurar que los PS se detengan si se usan directamente
        if (chidoriThinPS) chidoriThinPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (chidoriThickPS) chidoriThickPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        chidoriActive = false;
    }

    private IEnumerator AutoOffAfterDelay()
    {
        float timeRemaining = chidoriAutoOffSeconds;

        while (timeRemaining > 0f)
        {
            // If manually turned off, stop waiting
            if (!chidoriActive)
            {
                _autoOffCo = null;
                yield break;
            }

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        _autoOffCo = null;
        // Timer expired � shut everything down
        TurnOff();
    }

    // ===== Vigilancia de orientación =====

    // Inicia la corrutina que vigila la orientación del eje
    private void StartWatchingOrientation()
    {
        if (watchRoutine == null)
            watchRoutine = StartCoroutine(WatchOrientation());
    }

    // Detiene la corrutina de vigilancia de orientación
    private void StopWatchingOrientation()
    {
        if (watchRoutine != null)
        {
            StopCoroutine(watchRoutine);
            watchRoutine = null;
        }
    }

    // Corrutina que monitorea continuamente el ángulo entre el eje local y el objetivo mundial
    private IEnumerator WatchOrientation()
    {
        bool armed = true;

        while (isActiveAndEnabled && isCharged)
        {
            Vector3 worldDir = transform.TransformDirection(localPointAxis).normalized;
            Vector3 tgt = targetWorldDirection.sqrMagnitude > 0f ? targetWorldDirection.normalized : Vector3.up;
            float angToTarget = Vector3.Angle(worldDir, tgt);

            // Si está armado y dentro del ángulo de entrada, cambiar a Chidori
            if (armed && angToTarget <= enterHalfAngle)
            {
                SwitchToChidoriNow();
                yield break;
            }

            // Rearmar cuando salga del ángulo de salida (histerésis)
            if (angToTarget > exitHalfAngle)
                armed = true;

            yield return new WaitForSeconds(checkInterval);
        }
    }

#if UNITY_EDITOR
    // Visualiza el eje local (rojo) y dirección objetivo (verde) en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 worldDir = transform.TransformDirection(localPointAxis).normalized;
        Gizmos.DrawRay(transform.position, worldDir * 0.6f);

        Gizmos.color = Color.green;
        Vector3 tgt = (targetWorldDirection.sqrMagnitude > 0f ? targetWorldDirection.normalized : Vector3.up);
        Gizmos.DrawRay(transform.position, tgt * 0.6f);
    }
#endif
}
