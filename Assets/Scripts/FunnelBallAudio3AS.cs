using UnityEngine;

public class FunnelBallAudioLite : MonoBehaviour
{
    [Header("Audio Sources (assign in Inspector)")]
    [SerializeField] private AudioSource continuousAS;   // loop source
    [SerializeField] private AudioSource spontaneousAS;  // one-shot on enter

    [Header("Clips (assign)")]
    //[SerializeField] private AudioClip rollClip;     // played on continuousAS (loop)
    [SerializeField] private AudioClip enterOneShot; // played once on spontaneousAS (optional)

    [SerializeField] private AudioSource slowAS;
    [SerializeField] private AudioSource mediumAS;
    [SerializeField] private AudioSource fastAS;

    private int funelOverlapCount = 0; // handle multiple adjacent Funel triggers

    void Reset()
    {
        // Auto-grab first two AudioSources if present
        var srcs = GetComponentsInChildren<AudioSource>(true);
        if (srcs.Length > 0) continuousAS = srcs[0];
        if (srcs.Length > 1) spontaneousAS = srcs[1];
    }

    void Awake()
    {
        if (continuousAS)
        {
            continuousAS.playOnAwake = false;
            continuousAS.loop = true;
            /*if (rollClip) continuousAS.clip = rollClip;
            continuousAS.spatialBlend = 1f; // 3D*/
        }
        if (spontaneousAS)
        {
            spontaneousAS.playOnAwake = false;
            spontaneousAS.loop = false;
            spontaneousAS.spatialBlend = 1f; // 3D
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other || !other.CompareTag("Funel")) return;

        funelOverlapCount++;

        // Start loop
        /*if (continuousAS && rollClip)
        {
            if (continuousAS.clip != rollClip) continuousAS.clip = rollClip;
            if (!continuousAS.isPlaying) continuousAS.Play();
        }*/

        // Fire one-shot on enter (optional)
        if (spontaneousAS && enterOneShot)
            spontaneousAS.PlayOneShot(enterOneShot);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other || !other.CompareTag("Funel")) return;

        // Safety: ensure loop stays alive
        /*if (continuousAS && rollClip && funelOverlapCount > 0 && !continuousAS.isPlaying)
            continuousAS.Play();*/
    }

    void OnTriggerExit(Collider other)
    {
        if (!other || !other.CompareTag("Funel")) return;

        funelOverlapCount = Mathf.Max(0, funelOverlapCount - 1);
        if (funelOverlapCount == 0 && continuousAS && continuousAS.isPlaying)
            continuousAS.Stop();
    }

    void OnDisable()
    {
        if (continuousAS && continuousAS.isPlaying) continuousAS.Stop();
    }
}
