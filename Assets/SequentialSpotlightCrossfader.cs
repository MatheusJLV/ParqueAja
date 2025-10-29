using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps all sources playing continuously and hands the "spotlight"
/// to each one in sequence by crossfading volumes.
/// - No Update(): uses a single coroutine with WaitForSecondsRealtime
/// - Resilient to timeScale changes
/// - Works with 2+ sources (defaults assume 4)
/// </summary>
[DisallowMultipleComponent]
public class SequentialSpotlightCrossfader : MonoBehaviour
{
    [Header("Audio Sources (order = spotlight order)")]
    public AudioSource[] sources;

    [Header("Per-source hold times (seconds)")]
    [Tooltip("Must match number of sources (e.g., [3, 2, 1, 4])")]
    public float[] holdTimes;

    [Header("Volumes")]
    [Range(0f, 1f)] public float baseVolume = 0.15f;
    [Range(0f, 1f)] public float spotlightVolume = 0.9f;

    [Header("Crossfade timing")]
    public float fadeDuration = 0.7f;

    [Header("Global fade")]
    [Tooltip("Duration of the overall fade when the system turns on.")]
    public float globalFadeIn = 1.5f;
    [Tooltip("Duration of the overall fade when the system turns off.")]
    public float globalFadeOut = 1.5f;

    private Coroutine loopRoutine;
    private Coroutine globalFadeRoutine;
    private float globalFadeFactor = 0f; // 0 = silent, 1 = full
    private bool isFadingOut = false;

    private void OnEnable()
    {
        if (sources == null || sources.Length == 0)
        {
            Debug.LogWarning($"{name}: No sources assigned.");
            return;
        }

        if (holdTimes == null || holdTimes.Length != sources.Length)
        {
            holdTimes = new float[sources.Length];
            for (int i = 0; i < holdTimes.Length; i++)
                holdTimes[i] = 2f;
        }

        // Prepare sources
        foreach (var s in sources)
        {
            if (!s) continue;
            s.loop = true;
            if (!s.isPlaying) s.Play();
            s.volume = 0f; // start silent
        }

        // Fade-in globally
        if (globalFadeRoutine != null) StopCoroutine(globalFadeRoutine);
        globalFadeRoutine = StartCoroutine(GlobalFade(0f, 1f, globalFadeIn));

        // Start the spotlight cycling
        loopRoutine = StartCoroutine(SpotlightLoop());
    }

    private void OnDisable()
    {
        if (loopRoutine != null) StopCoroutine(loopRoutine);
        if (globalFadeRoutine != null) StopCoroutine(globalFadeRoutine);

        // en vez de StartCoroutine(...) hacemos mute inmediato
        foreach (var s in sources)
        {
            if (!s) continue;
            s.volume = 0f;
            s.Stop();
        }

        isFadingOut = false;
    }

    private IEnumerator SpotlightLoop()
    {
        int idx = 0;
        while (true)
        {
            yield return CrossfadeTo(idx);
            yield return new WaitForSecondsRealtime(holdTimes[idx]);
            idx = (idx + 1) % sources.Length;
        }
    }

    private IEnumerator CrossfadeTo(int spotlightIdx)
    {
        int n = sources.Length;
        float[] startVols = new float[n];
        float[] targetVols = new float[n];

        for (int i = 0; i < n; i++)
        {
            if (sources[i] == null) continue;
            startVols[i] = sources[i].volume;
            targetVols[i] = (i == spotlightIdx) ? spotlightVolume : baseVolume;
        }

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float a = (Time.realtimeSinceStartup - startTime) / Mathf.Max(0.01f, fadeDuration);
            if (a >= 1f) a = 1f;

            for (int i = 0; i < n; i++)
            {
                if (!sources[i]) continue;
                float target = Mathf.Lerp(startVols[i], targetVols[i], a) * globalFadeFactor;
                sources[i].volume = target;
            }

            if (a >= 1f) break;
            yield return null;
        }
    }

    private IEnumerator GlobalFade(float from, float to, float duration)
    {
        double t0 = Time.realtimeSinceStartupAsDouble;
        while (true)
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - t0;
            float a = Mathf.Clamp01((float)(elapsed / duration));
            globalFadeFactor = Mathf.Lerp(from, to, a);

            // Apply scaling to all sources in real time
            foreach (var s in sources)
            {
                if (!s) continue;
                s.volume *= globalFadeFactor;
            }

            if (a >= 1f) break;
            yield return null;
        }

        globalFadeFactor = to;

        // If faded out completely, ensure silence
        if (isFadingOut && globalFadeFactor <= 0.001f)
        {
            foreach (var s in sources)
            {
                if (!s) continue;
                s.volume = 0f;
                s.Stop();
            }
            isFadingOut = false;
        }
    }
}
