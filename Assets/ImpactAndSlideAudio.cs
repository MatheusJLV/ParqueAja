using System.Collections;
using UnityEngine;

/// <summary>
/// Plays impact one-shots on collisions and a short sliding loop while tangential motion is sustained.
/// Uses two distinct AudioSources: one for impacts, one for sliding.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class ImpactAndSlideAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Clip used for impacts (short hits).")]
    public AudioClip impactClip;
    [Tooltip("Clip used for sliding (looped).")]
    public AudioClip slideClip;

    [Header("Impact Settings")]
    public float minImpactForce = 0.15f;
    public float maxImpactForce = 3.0f;
    [Range(0f, 1f)] public float impactBaseVolume = 1f;
    public float impactCooldown = 0.08f;
    public bool impactRandomizePitch = true;
    [Range(0f, 0.35f)] public float impactPitchVariance = 0.12f;
    public bool impactScaleByForce = true;

    [Header("Sliding Settings")]
    public float slideThreshold = 0.15f;
    [Range(0f, 1f)] public float slideMaxVolume = 1f;
    public float slideFadeDuration = 0.25f;
    public float slideIdleTimeout = 0.5f;
    public bool slidePitchBySpeed = false;
    [Range(0.8f, 1.2f)] public float slidePitchMin = 0.95f;
    [Range(0.8f, 1.2f)] public float slidePitchMax = 1.05f;
    public float slidePitchRefSpeed = 0.6f;

    [Header("Feature Toggles")]
    [Tooltip("Enable or disable impact sound playback.")]
    public bool allowImpactSound = true;
    [Tooltip("Enable or disable sliding loop. Disabling it still allows impacts.")]
    public bool allowSlideSound = true;

    // Internal state
    private AudioSource impactSource;
    private AudioSource slideSource;
    private float nextImpactTime;
    private float lastSlideTime;
    private bool slideActive;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        // Create two sources for independent control
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            impactSource = gameObject.AddComponent<AudioSource>();
            slideSource = GetComponent<AudioSource>();
        }
        else
        {
            impactSource = sources[0];
            slideSource = sources[1];
        }

        // Configure impact source
        impactSource.playOnAwake = false;
        impactSource.loop = false;
        impactSource.volume = 1f;

        // Configure slide source
        slideSource.playOnAwake = false;
        slideSource.loop = true;
        slideSource.volume = 0f;
    }

    private void OnCollisionEnter(Collision c)
    {
        if (!allowImpactSound) return;
        if (impactClip == null) return;

        float impact = c.relativeVelocity.magnitude;
        if (impact < minImpactForce) return;

        float now = Time.time;
        if (now < nextImpactTime) return; // cooldown

        // Randomize pitch
        if (impactRandomizePitch)
            impactSource.pitch = 1f + Random.Range(-impactPitchVariance, impactPitchVariance);
        else
            impactSource.pitch = 1f;

        // Volume based on force
        float vol = impactBaseVolume;
        if (impactScaleByForce)
        {
            float t = Mathf.InverseLerp(minImpactForce, maxImpactForce, impact);
            vol *= Mathf.Clamp01(t);
        }

        impactSource.PlayOneShot(impactClip, vol);
        nextImpactTime = now + impactCooldown;
    }

    private void OnCollisionStay(Collision c)
    {
        // Sliding only valid if allowed AND impacts are allowed
        if (!allowSlideSound || !allowImpactSound) return;
        if (slideClip == null) return;

        Vector3 rel = c.relativeVelocity;
        Vector3 n = c.contacts[0].normal;
        float tangential = (rel - Vector3.Dot(rel, n) * n).magnitude;

        if (tangential > slideThreshold)
        {
            lastSlideTime = Time.time;

            if (!slideActive)
            {
                slideActive = true;
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeSlide(slideSource.volume, slideMaxVolume, slideFadeDuration, true));
            }

            if (slidePitchBySpeed)
            {
                float t = Mathf.Clamp01(tangential / Mathf.Max(0.0001f, slidePitchRefSpeed));
                slideSource.pitch = Mathf.Lerp(slidePitchMin, slidePitchMax, t);
            }
        }
    }

    private void OnCollisionExit(Collision c)
    {
        if (!allowSlideSound) return;

        if (slideActive)
        {
            slideActive = false;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeSlide(slideSource.volume, 0f, slideFadeDuration, false));
        }
    }

    private void Update()
    {
        if (!allowSlideSound) return;
        if (slideActive && Time.time - lastSlideTime > slideIdleTimeout)
        {
            slideActive = false;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeSlide(slideSource.volume, 0f, slideFadeDuration, false));
        }
    }

    private IEnumerator FadeSlide(float from, float to, float duration, bool startIfNeeded)
    {
        if (!allowSlideSound) yield break;

        if (startIfNeeded)
        {
            slideSource.clip = slideClip;
            if (!slideSource.isPlaying) slideSource.Play();
        }

        float startTime = Time.realtimeSinceStartup;
        duration = Mathf.Max(0.01f, duration);

        while (true)
        {
            float a = (Time.realtimeSinceStartup - startTime) / duration;
            if (a >= 1f) a = 1f;
            slideSource.volume = Mathf.Lerp(from, to, a);
            if (a >= 1f) break;
            yield return null;
        }

        if (!startIfNeeded && to <= 0.0001f)
        {
            slideSource.volume = 0f;
            slideSource.Stop();
        }
    }

    private void OnDisable() => HardStopAudio();
    private void OnDestroy() => HardStopAudio();

    public void HardStopAudio()
    {
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
        if (impactSource) impactSource.Stop();
        if (slideSource)
        {
            slideSource.Stop();
            slideSource.volume = 0f;
        }
        slideActive = false;
    }
}
