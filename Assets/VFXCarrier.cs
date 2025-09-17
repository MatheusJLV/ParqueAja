using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class VFXCarrier : MonoBehaviour
{

    public VisualEffect carrierVFX;
    private Collider intruder1;
    public bool isCharged = false;


    [Header("Chidori (target Particle Systems)")]
    [SerializeField] private ParticleSystem chidoriThinPS;   // optional
    [SerializeField] private ParticleSystem chidoriThickPS;  // optional


    [Header("Orientation trigger")]
    [Tooltip("Local axis that represents the wand's pointing direction. Red arrow in the editor = +X.")]
    [SerializeField] private Vector3 localPointAxis = Vector3.right;

    [Tooltip("World direction the local axis should face to trigger. For 'X points UP', use Vector3.up.")]
    [SerializeField] private Vector3 targetWorldDirection = Vector3.up;

    [Tooltip("Total cone angle around the target direction considered 'inside'. 120° => 60° half-angle.")]
    [Range(1f, 179f)]
    [SerializeField] private float coneAngle = 120f;

    [Tooltip("Extra degrees beyond the enter half-angle to release the trigger (prevents flicker).")]
    [SerializeField] private float hysteresis = 10f;

    [Tooltip("Seconds between orientation checks.")]
    [SerializeField] private float checkInterval = 0.10f;

    private float enterHalfAngle;
    private float exitHalfAngle;
    private Coroutine watchRoutine;

    public WandPS wandPS; // reference to the WandPS script to check if it's active

    [SerializeField] private AudioSource staticAS;

    private void Start()
    {

        if (carrierVFX != null)
        {
            chidoriThickPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            chidoriThinPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            carrierVFX.Stop();
        }

        enterHalfAngle = coneAngle * 0.5f;
        exitHalfAngle = enterHalfAngle + Mathf.Abs(hysteresis);
    }

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


    public void TurnOff()
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



    public void Charge() => TurnOn();


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

    void OnTriggerEnter(Collider other)
    {
        if (!isCharged || !other.CompareTag("Conductor"))
            return;

        Discharge(other);
    }

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

    void Update()
    {
        if (intruder1 != null && carrierVFX != null)
            carrierVFX.SetVector3("IntruderPosition", intruder1.transform.position);
    }


    public void SwitchToChidoriNow()
    {
        //if (chidoriThinPS != null && !chidoriThinPS.isPlaying) chidoriThinPS.Play(true);
        //if (chidoriThickPS != null && !chidoriThickPS.isPlaying) chidoriThickPS.Play(true);
        if (wandPS != null && wandPS.isActiveAndEnabled)
            wandPS.TurnOn();
        TurnOff();
    }


    private void StartWatchingOrientation()
    {
        if (watchRoutine == null)
            watchRoutine = StartCoroutine(WatchOrientation());
    }

    private void StopWatchingOrientation()
    {
        if (watchRoutine != null)
        {
            StopCoroutine(watchRoutine);
            watchRoutine = null;
        }
    }

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