using System.Collections;
using System.Reflection;
using UnityEngine;

public class WandPS : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem thinPS;
    [SerializeField] private ParticleSystem thickPS;

    [Header("Light control")]
    [SerializeField] private LightManager lightManager;   // assign from scene
    [SerializeField] private bool dimInsteadOfOff = true; // true = dim, false = full off
    [Range(0f, 1f)]
    [SerializeField] private float dimIntensity = 0.3f;   // used when dimInsteadOfOff = true
    [Range(0f, 1f)]
    [SerializeField] private float restoreIntensity = 1f; // used on TurnOff()

    [Header("Thrust detection")]
    [SerializeField] private Vector3 localThrustAxis = Vector3.right; // red arrow = +X
    [SerializeField] private Transform head;
    [SerializeField] private float velocityThreshold = 0.9f;
    [SerializeField] private float axialVelocityThreshold = 0.4f;
    [Range(0f, 1f)][SerializeField] private float minAlignment = 0.7f;
    [SerializeField] private float awayFromHeadDotMin = 0.0f;
    [Range(0.01f, 1f)][SerializeField] private float velocitySmoothing = 0.8f;
    [SerializeField] private float sampleInterval = 0.015f;
    [SerializeField] private float cooldown = 0.12f;
    [SerializeField] private bool startOn = true;

    private Rigidbody rb;
    private Coroutine watchRoutine;
    private Vector3 lastPosWS;
    private Vector3 velSmoothed;
    private float cooldownTimer;

    public void TurnOn()
    {
        if (thinPS && !thinPS.isPlaying) thinPS.Play(true);
        if (thickPS && !thickPS.isPlaying) thickPS.Play(true);

        // Light: dim or turn off on activation
        if (lightManager)
        {
            float target = dimInsteadOfOff ? Mathf.Clamp01(dimIntensity) : 0f;
            TryLightSetIntensityOrToggle(target);
        }

        StartWatcher();
    }

    public void TurnOff()
    {
        if (thinPS && thinPS.isPlaying) thinPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (thickPS && thickPS.isPlaying) thickPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Light: restore on deactivate
        if (lightManager)
        {
            float target = Mathf.Clamp01(restoreIntensity);
            TryLightSetIntensityOrToggle(target);
        }

        StopWatcher();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (startOn) TurnOn();
    }

    private void OnDisable()
    {
        StopWatcher();
    }

    private void StartWatcher()
    {
        if (watchRoutine != null) return;
        lastPosWS = transform.position;
        velSmoothed = Vector3.zero;
        cooldownTimer = 0f;
        watchRoutine = StartCoroutine(WatchThrust());
    }

    private void StopWatcher()
    {
        if (watchRoutine == null) return;
        StopCoroutine(watchRoutine);
        watchRoutine = null;
    }

    private IEnumerator WatchThrust()
    {
        while (isActiveAndEnabled)
        {
            Vector3 v;
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

            Vector3 axisWs = transform.TransformDirection(localThrustAxis).normalized;
            float vMag = velSmoothed.magnitude;
            float vAxialSigned = Vector3.Dot(velSmoothed, axisWs);
            float vAxialNeg = Mathf.Max(0f, -vAxialSigned);
            float alignment = (vMag > 0.0001f) ? (vAxialNeg / vMag) : 0f;

            bool passAxialSpeed = vAxialNeg >= axialVelocityThreshold;
            bool passAlignment = alignment >= minAlignment;
            bool passOverallSpeed = vMag >= velocityThreshold;

            bool passAwayFromHead = true;
            if (head && awayFromHeadDotMin > 0f && vMag > 0.0001f)
            {
                Vector3 fromHeadToWand = (transform.position - head.position).normalized;
                float dot = Vector3.Dot(velSmoothed.normalized, fromHeadToWand);
                passAwayFromHead = dot >= awayFromHeadDotMin;
            }

            if (cooldownTimer > 0f) cooldownTimer -= sampleInterval;

            if (cooldownTimer <= 0f && passAxialSpeed && passAlignment && passOverallSpeed && passAwayFromHead)
            {
                TurnOff();
                cooldownTimer = cooldown;
            }

            yield return new WaitForSeconds(sampleInterval);
        }
    }

    private void TryLightSetIntensityOrToggle(float target)
    {
        // Prefer SetIntensity(float)
        MethodInfo setIntensity = lightManager.GetType().GetMethod("SetIntensity", new[] { typeof(float) });
        if (setIntensity != null)
        {
            setIntensity.Invoke(lightManager, new object[] { target });
            return;
        }

        // Fall back to TurnOff()/TurnOn()
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

        // Last resort: DecreaseIntensity()/IncreaseIntensity()
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
