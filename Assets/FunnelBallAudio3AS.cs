using UnityEngine;

public class FunnelBallAudioLite : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody rb;
    public AudioSource rollAS;      // single rolling loop (loop=true)
    public AudioSource oneshotAS;   // for hits
    public AudioLowPassFilter lpf;  // on the same GO as rollAS

    [Header("Clips")]
    public AudioClip rollLoop;      // generic rolling loop
    public AudioClip hitIn;
    public AudioClip hitOut;

    [Header("Zones (set by triggers)")]
    public bool inExterior, inMiddle, inInterior;

    [Header("Behaviour")]
    [Range(0f, 1f)] public float rollVolume = 0.9f;
    public float attack = 0.06f, release = 0.18f;
    [Tooltip("Pitch +/- range driven by speed (0 = off)")]
    public float pitchFollow = 0.1f;
    public float speedForMaxPitch = 8f;

    [Header("LPF per zone (Hz)")]
    public float lpfExterior = 8000f;
    public float lpfMiddle = 4500f;
    public float lpfInterior = 2200f;
    public float lpfSmooth = 0.12f;

    [Header("Contact")]
    public LayerMask funnelLayer;
    public float contactGrace = 0.10f;

    float lastContactTime, vol, volVel, lpfTarget, lpfVel;
    int contacts;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        rollAS.loop = true; rollAS.spatialBlend = 1f; rollAS.dopplerLevel = 0f; rollAS.playOnAwake = false;
        rollAS.clip = rollLoop; rollAS.volume = 0f;
        if (lpf) lpf.enabled = true;
    }

    void Update()
    {
        /*
        // Contact considered “on” for a short grace window
        bool touching = (Time.time - lastContactTime) <= contactGrace;

        // Target vol from speed * contact
        float speed = rb ? rb.linearVelocity.magnitude : 0f;
        float speed01 = Mathf.Clamp01(speed / 10f); // tweak divisor for taste
        float targetVol = touching ? (rollVolume * speed01) : 0f;

        // Smooth in/out
        float smooth = (targetVol > vol) ? attack : release;
        vol = Mathf.SmoothDamp(vol, targetVol, ref volVel, smooth);

        if (vol > 0.001f)
        {
            if (!rollAS.isPlaying) rollAS.Play();
            rollAS.volume = vol;

            // Mild pitch follow
            if (pitchFollow > 0f)
            {
                float t = Mathf.Clamp01(speed / Mathf.Max(0.001f, speedForMaxPitch));
                rollAS.pitch = Mathf.Lerp(1f - pitchFollow, 1f + pitchFollow, t);
            }

            // Zone tone (LPF)
            lpfTarget = inInterior ? lpfInterior : (inMiddle ? lpfMiddle : lpfExterior);
            if (lpf) lpf.cutoffFrequency = Mathf.SmoothDamp(lpf.cutoffFrequency, lpfTarget, ref lpfVel, lpfSmooth);
        }
        else
        {
            if (rollAS.isPlaying) rollAS.Stop();
        }
        */

        // --- TEMPORARY TEST BODY START ---
        float speed = rb ? rb.linearVelocity.magnitude : 0f;
        float speed01 = Mathf.Clamp01(speed / 10f);
        float targetVol = rollVolume * speed01;   // <-- ignores contact logic

        float smooth = (targetVol > vol) ? attack : release;
        vol = Mathf.SmoothDamp(vol, targetVol, ref volVel, smooth);

        if (vol > 0.001f)
        {
            if (!rollAS.isPlaying) rollAS.Play();
            rollAS.volume = vol;
        }
        else
        {
            if (rollAS.isPlaying) rollAS.Stop();
        }
        // --- TEMPORARY TEST BODY END ---
    }

    // Cheap contact tracking without OnCollisionStay:
    void OnCollisionEnter(Collision c)
    {
        if (((1 << c.collider.gameObject.layer) & funnelLayer) != 0)
        {
            contacts++; lastContactTime = Time.time;
        }
    }
    void OnCollisionExit(Collision c)
    {
        if (((1 << c.collider.gameObject.layer) & funnelLayer) != 0)
        {
            contacts = Mathf.Max(0, contacts - 1);
            if (contacts > 0) lastContactTime = Time.time;
        }
    }

    // Trigger volumes set zone booleans:
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FunelExterior")) inExterior = true;
        else if (other.CompareTag("FunelMedio")) inMiddle = true;
        else if (other.CompareTag("FunelInterior")) inInterior = true;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("FunelExterior")) inExterior = false;
        else if (other.CompareTag("FunelMedio")) inMiddle = false;
        else if (other.CompareTag("FunelInterior")) inInterior = false;
    }

    // One-shots:
    void OnTriggerEnterHitEntrada() { if (oneshotAS && hitIn) oneshotAS.PlayOneShot(hitIn); }
    void OnTriggerEnterHitSalida() { if (oneshotAS && hitOut) oneshotAS.PlayOneShot(hitOut); }
}
