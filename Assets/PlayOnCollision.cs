using UnityEngine;

/// <summary>
/// Plays an AudioSource when this object collides with something.
/// - Works with standard collisions (not triggers)
/// - Ignores long contacts; plays once per impact
/// - Optionally limits minimum impact force to trigger sound
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class PlayOnCollision : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Tooltip("Base volume applied before impact scaling.")]
    [Range(0f, 1f)] public float baseVolume = 1f;

    [Header("Triggering")]
    [Tooltip("Minimum collision relative speed required to play.")]
    public float minImpactForce = 0.15f;

    [Tooltip("Impact relative speed that maps to max volume (for scaling).")]
    public float maxImpactForce = 3.0f;

    [Tooltip("Seconds to wait between plays to avoid spam.")]
    public float cooldownSeconds = 0.08f;

    [Header("Variation")]
    public bool randomizePitch = true;

    [Range(0f, 0.35f)]
    public float pitchVariance = 0.12f;

    [Tooltip("Scale volume by impact strength (min - baseVolume*0, max - baseVolume*1).")]
    public bool scaleVolumeByImpact = true;

    private float _nextPlayableTime;

    private void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impact = collision.relativeVelocity.magnitude;
        if (impact < minImpactForce)
            return;

        float now = Time.time;
        if (now < _nextPlayableTime)
            return; // cooldown active

        // Pitch variation
        audioSource.pitch = randomizePitch
            ? 1f + Random.Range(-pitchVariance, pitchVariance)
            : 1f;

        // Volume scaling by impact (optional)
        float vol = baseVolume;
        if (scaleVolumeByImpact)
        {
            float t = Mathf.InverseLerp(minImpactForce, maxImpactForce, impact);
            vol *= Mathf.Clamp01(t);
        }

        // Play without overlap: use Play() if the clip is short and you don't want stacking,
        // or PlayOneShot for safe overlap. Here we respect cooldown and use PlayOneShot
        // so extremely close separate contacts can still layer (rare due to cooldown).
        var clip = audioSource.clip;
        if (clip != null)
            audioSource.PlayOneShot(clip, vol);
        else
            audioSource.Play(); // fallback if clip is routed via the source

        _nextPlayableTime = now + Mathf.Max(0f, cooldownSeconds);
    }
}
