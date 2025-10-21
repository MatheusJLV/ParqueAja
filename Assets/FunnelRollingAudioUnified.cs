using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))] // ball collider (not trigger)
public class FunnelRollingAudioUnified : MonoBehaviour
{
    [Header("Looping Layers (3D, loop=on, playOnAwake=off)")]
    [SerializeField] private AudioSource slowAS;
    [SerializeField] private AudioSource mediumAS;
    [SerializeField] private AudioSource fastAS;

    [Header("Hit One-Shot (3D, loop=off, playOnAwake=off)")]
    [SerializeField] private AudioSource hitAS;     // shared one-shot source
    [SerializeField] private AudioClip hitEntrada;  // tag: FunnelHitEntrada
    [SerializeField] private AudioClip hitSalida;   // tag: FunnelHitSalida

    [Header("Speed thresholds (m/s)")]
    public float slowMax = 2f;     // up to here = mostly slow
    public float mediumMax = 6f;   // above this = fast dominates

    [Header("Fades & Pitch")]
    public float fadeLerp = 6f;    // higher = faster volume response
    public bool modulatePitch = false;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.25f;
    public float pitchAtSpeed = 10f; // speed giving pitchMax

    [Header("Tags")]
    public string funelTag = "Funel"; // exact casing

    private Rigidbody rb;
    private int funelOverlap = 0;
    private bool loopsStarted = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) Debug.LogError("[FunnelRollingAudioUnified] Missing Rigidbody.");

        // Safety: ensure looping sources are configured
        PrepareLoopSource(slowAS);
        PrepareLoopSource(mediumAS);
        PrepareLoopSource(fastAS);
        if (hitAS)
        {
            hitAS.loop = false;
            hitAS.playOnAwake = false;
            hitAS.spatialBlend = 1f;
        }
    }

    void PrepareLoopSource(AudioSource src)
    {
        if (!src) return;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f; // 3D
        src.volume = 0f;       // start silent; we�ll ramp when inside Funel
    }

    void Update()
    {
        if (funelOverlap <= 0 || !rb) return;

        float v = rb.linearVelocity.magnitude;

        // Compute rough crossfade weights
        float wSlow = Mathf.Clamp01(1f - (v / Mathf.Max(0.0001f, slowMax)));
        float wMed = Mathf.Clamp01((v - slowMax) / Mathf.Max(0.0001f, (mediumMax - slowMax)));
        float wFast = Mathf.Clamp01((v - mediumMax) / Mathf.Max(0.0001f, mediumMax)); // grows gradually above mediumMax

        // Soft normalize so they sum ~1
        float sum = wSlow + wMed + wFast;
        if (sum < 0.001f) { wSlow = 1f; wMed = 0f; wFast = 0f; }
        else { wSlow /= sum; wMed /= sum; wFast /= sum; }

        // Smoothly move volumes
        float step = fadeLerp * Time.deltaTime;
        if (slowAS) slowAS.volume = Mathf.MoveTowards(slowAS.volume, wSlow, step);
        if (mediumAS) mediumAS.volume = Mathf.MoveTowards(mediumAS.volume, wMed, step);
        if (fastAS) fastAS.volume = Mathf.MoveTowards(fastAS.volume, wFast, step);

        // Optional pitch modulation (subtle)
        if (modulatePitch)
        {
            float t = Mathf.Clamp01(v / Mathf.Max(0.0001f, pitchAtSpeed));
            float p = Mathf.Lerp(pitchMin, pitchMax, t);
            if (slowAS) slowAS.pitch = p;
            if (mediumAS) mediumAS.pitch = p;
            if (fastAS) fastAS.pitch = p;
        }

        if (Time.frameCount % 30 == 0)
            Debug.Log($"funelOverlap={funelOverlap} loopsStarted={loopsStarted}");

    }

    // ---------- Trigger Handling ----------

    void OnTriggerEnter(Collider other)
    {
        if (!other) return;

        // Hits (one-shots)
        if (other.CompareTag("FunnelHitEntrada"))
        {
            if (hitAS && hitEntrada) hitAS.PlayOneShot(hitEntrada);
            return;
        }
        if (other.CompareTag("FunnelHitSalida"))
        {
            if (hitAS && hitSalida) hitAS.PlayOneShot(hitSalida);
            return;
        }

        // Rolling zone
        if (other.CompareTag(funelTag))
        {
            funelOverlap++;
            if (!loopsStarted)
            {
                StartLoopsIfNeeded();
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other && other.CompareTag(funelTag))
        {
            // Safety: if something stopped them, restart while inside
            if (funelOverlap > 0 && !AllAnyPlaying())
                StartLoopsIfNeeded();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other && other.CompareTag(funelTag))
        {
            funelOverlap = Mathf.Max(0, funelOverlap - 1);
            if (funelOverlap == 0)
                StopAllLoops();
        }
    }

    void OnDisable()
    {
        StopAllLoops();
    }

    // ---------- Helpers ----------

    bool AllAnyPlaying()
    {
        return (slowAS && slowAS.isPlaying) || (mediumAS && mediumAS.isPlaying) || (fastAS && fastAS.isPlaying);
    }

    void StartLoopsIfNeeded()
    {
        loopsStarted = true;
        if (slowAS && !slowAS.isPlaying) slowAS.Play();
        if (mediumAS && !mediumAS.isPlaying) mediumAS.Play();
        if (fastAS && !fastAS.isPlaying) fastAS.Play();
        // Start silent; Update() will ramp volumes based on current speed
        if (slowAS) slowAS.volume = 0f;
        if (mediumAS) mediumAS.volume = 0f;
        if (fastAS) fastAS.volume = 0f;
    }

    void StopAllLoops()
    {
        loopsStarted = false;
        if (slowAS) { slowAS.Stop(); slowAS.volume = 0f; }
        if (mediumAS) { mediumAS.Stop(); mediumAS.volume = 0f; }
        if (fastAS) { fastAS.Stop(); fastAS.volume = 0f; }
    }
}

