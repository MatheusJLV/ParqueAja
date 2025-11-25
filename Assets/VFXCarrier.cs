using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

/*
VFXCarrier: controla un VisualEffect "carrier" que atrapa objetos conductores
y puede convertir esa carga en un "Chidori" (ParticleSystems / WandPS).
Funcionalidades principales:
- Encender/apagar carrier VFX y audio estático.
- Detectar intrusos con trigger y actualizar su posición al VFX.
- Vigilar la orientación local para disparar Chidori cuando apunta dentro de un cono.
- Auto-apagado del Chidori tras un retraso opcional.
*/

public class VFXCarrier : MonoBehaviour
{
    //Referencia al VisualEffect del carrier
    public VisualEffect carrierVFX;
    //Collider actualmente "intruso" (primer conductor detectado)
    private Collider intruder1;
    //Flag de carga del carrier
    public bool isCharged = false;

    [Header("Chidori (target Particle Systems)")]
    //PS objetivo (opcional) - fino
    [SerializeField] private ParticleSystem chidoriThinPS;   // optional
     //PS objetivo (opcional) - grueso
    [SerializeField] private ParticleSystem chidoriThickPS;  // optional

    [Header("Orientation trigger")]
    //Local axis que representa hacia dónde apunta la varita (p. ej. +X)
    [Tooltip("Local axis that represents the wand's pointing direction. Red arrow in the editor = +X.")]
    [SerializeField] private Vector3 localPointAxis = Vector3.right;

    //Dirección world objetivo que se quiere alcanzar para activar (p. ej. Vector3.up)
    [Tooltip("World direction the local axis should face to trigger. For 'X points UP', use Vector3.up.")]
    [SerializeField] private Vector3 targetWorldDirection = Vector3.up;

    //Ángulo total del cono (ej. 120 => half-angle 60)
    [Tooltip("Total cone angle around the target direction considered 'inside'. 120� => 60� half-angle.")]
    [Range(1f, 179f)]
    [SerializeField] private float coneAngle = 120f;

    //Histeresis en grados para evitar flicker al entrar/salir del cono
    [Tooltip("Extra degrees beyond the enter half-angle to release the trigger (prevents flicker).")]
    [SerializeField] private float hysteresis = 10f;
    //Segundos entre comprobaciones de orientación
    [Tooltip("Seconds between orientation checks.")]
    [SerializeField] private float checkInterval = 0.10f;

    //Ángulo mitad de entrada y salida (calculados)
    private float enterHalfAngle;
    private float exitHalfAngle;
    private Coroutine watchRoutine;
    //Referencia opcional a WandPS para encender/apagar el Chidori
    public WandPS wandPS; // reference to the WandPS script
    [SerializeField] private AudioSource staticAS; //Audio source para ruido estático del carrier

    [Header("Auto-off")]
    //Habilitar auto-off del Chidori
    [SerializeField] private bool enableAutoOff = true;
    //Segundos hasta auto-off
    [SerializeField] private float chidoriAutoOffSeconds = 5f;
    private Coroutine _autoOffCo;
    private bool chidoriActive = false;

    //Start: inicializar estado y asegurar PS/VFX parados al inicio
    private void Start()
    {
        if (carrierVFX != null)
        {
            // Ensure PS are stopped at boot (if used directly)
            if (chidoriThickPS) chidoriThickPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (chidoriThinPS) chidoriThinPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            carrierVFX.Stop();
        }

        enterHalfAngle = coneAngle * 0.5f;
        exitHalfAngle = enterHalfAngle + Mathf.Abs(hysteresis);
    }

    //TurnOn: cargar y arrancar VFX/audio + empezar a vigilar orientación
    public void TurnOn()
    {
        isCharged = true;
        if (carrierVFX != null)
            carrierVFX.Play();

        // audio: start static loop
        if (staticAS != null)
        {
            staticAS.loop = true;
            if (!staticAS.isPlaying) staticAS.Play();
        }

        StartWatchingOrientation();
    }

    /// <summary>
    /// Unified shutdown for both the carrier and Chidori.
    /// </summary>

    //Unified shutdown para carrier y Chidori
    public void TurnOff()
    {
        DisarmCarrier();
        DeactivateChidori();
    }
    
    public void Charge() => TurnOn();  //Alias para TurnOn
    //Discharge: llamado al detectar un collider conductor; setea intruso si procede
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

    //Trigger enter: si está cargado y collider es conductor, llamar a Discharge
    void OnTriggerEnter(Collider other)
    {
        if (!isCharged || !other.CompareTag("Conductor"))
            return;

        Discharge(other);
    }

    //Trigger exit: si sale el intruso registrado, limpiar referencia y VFX
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
    //Update: mantener la posición del intruso actual en el VFX cada frame
    void Update()
    {
        if (intruder1 != null && carrierVFX != null)
            carrierVFX.SetVector3("IntruderPosition", intruder1.transform.position);
    }

    /// <summary>
    /// Called when aim is inside the cone: disarm carrier, enable Chidori, start auto-off timer.
    /// </summary>
    public void SwitchToChidoriNow()
    {
        // Disarm the carrier immediately (stop VFX/audio/watcher)
        DisarmCarrier();

        // Turn Chidori ON via WandPS (preferred)
        if (wandPS != null && wandPS.isActiveAndEnabled)
            wandPS.TurnOn();

        // (Optional) If you want to directly play PS instead of WandPS:
        // if (chidoriThinPS && !chidoriThinPS.isPlaying)  chidoriThinPS.Play(true);
        // if (chidoriThickPS && !chidoriThickPS.isPlaying) chidoriThickPS.Play(true);

        chidoriActive = true;

        // Start auto-off if enabled
        if (enableAutoOff)
        {
            if (_autoOffCo != null) StopCoroutine(_autoOffCo);
            _autoOffCo = StartCoroutine(AutoOffAfterDelay());
        }
    }

    // ===== Helpers (clean separation of responsibilities) =====
    //DisarmCarrier: parar VFX/audio, limpiar intruso y detener watcher
    private void DisarmCarrier()
    {
        isCharged = false;

        // audio: stop static loop
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

    //DeactivateChidori: apagar Chidori y cancelar temporizador auto-off
    private void DeactivateChidori()
    {
        // stop auto-off timer
        if (_autoOffCo != null)
        {
            StopCoroutine(_autoOffCo);
            _autoOffCo = null;
        }

        // Turn off via WandPS
        if (wandPS != null && wandPS.isActiveAndEnabled)
            wandPS.TurnOff();

        // (Optional) Also ensure PS are stopped if used directly
        if (chidoriThinPS) chidoriThinPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (chidoriThickPS) chidoriThickPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        chidoriActive = false;
    }

    //AutoOff coroutine: cuenta regresiva y apaga todo al expirar
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

    // ===== Orientation watcher =====
    //StartWatchingOrientation: iniciar coroutine de vigilancia si no existe
    private void StartWatchingOrientation()
    {
        if (watchRoutine == null)
            watchRoutine = StartCoroutine(WatchOrientation());
    }
    //StopWatchingOrientation: detener coroutine de vigilancia si existe
    private void StopWatchingOrientation()
    {
        if (watchRoutine != null)
        {
            StopCoroutine(watchRoutine);
            watchRoutine = null;
        }
    }
    /*
    WatchOrientation: comprueba periódicamente la orientación local en world
    - Calcula el ángulo entre el eje local apuntador y la dirección objetivo
    - Si entra dentro del half-angle de entrada dispara SwitchToChidoriNow
    - Usa histeresis para evitar oscilaciones
    */
    private IEnumerator WatchOrientation()
    {
        bool armed = true;

        while (isActiveAndEnabled && isCharged)
        {
            Vector3 worldDir = transform.TransformDirection(localPointAxis).normalized;
            Vector3 tgt = targetWorldDirection.sqrMagnitude > 0f ? targetWorldDirection.normalized : Vector3.up;
            float angToTarget = Vector3.Angle(worldDir, tgt);

            if (armed && angToTarget <= enterHalfAngle)
            {
                SwitchToChidoriNow();
                yield break;
            }

            if (angToTarget > exitHalfAngle)
                armed = true;

            yield return new WaitForSeconds(checkInterval);
        }
    }

#if UNITY_EDITOR
    //OnDrawGizmosSelected: visualizar ejes de apuntado y target en editor
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
