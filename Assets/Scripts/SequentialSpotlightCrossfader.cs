using System.Collections;
using UnityEngine;

// Sistema de crossfade secuencial para múltiples AudioSources que mantiene todas las fuentes reproduciendo continuamente
// y pasa el "spotlight" (volumen principal) a cada una en secuencia mediante transiciones suaves.
// - No usa Update(): utiliza una sola corrutina con WaitForSecondsRealtime
// - Resiliente a cambios de timeScale
// - Funciona con 2 o más fuentes de audio (por defecto asume 4)
[DisallowMultipleComponent]
public class SequentialSpotlightCrossfader : MonoBehaviour
{
    [Header("Audio Sources (order = spotlight order)")]
    public AudioSource[] sources;          // Array de fuentes de audio en el orden de rotación del spotlight

    [Header("Per-source hold times (seconds)")]
    [Tooltip("Must match number of sources (e.g., [3, 2, 1, 4])")]
    public float[] holdTimes;              // Tiempo que cada fuente mantiene el spotlight antes de pasar a la siguiente              // Tiempo que cada fuente mantiene el spotlight antes de pasar a la siguiente

    [Header("Volumes")]
    [Range(0f, 1f)] public float baseVolume = 0.15f;        // Volumen base para las fuentes que no tienen el spotlight
    [Range(0f, 1f)] public float spotlightVolume = 0.9f;    // Volumen para la fuente que tiene el spotlight activo    // Volumen para la fuente que tiene el spotlight activo

    [Header("Crossfade timing")]
    public float fadeDuration = 0.7f;      // Duración de la transición de crossfade entre fuentes      // Duración de la transición de crossfade entre fuentes

    [Header("Global fade")]
    [Tooltip("Duration of the overall fade when the system turns on.")]
    public float globalFadeIn = 1.5f;      // Duración del fade-in global cuando el sistema se activa
    [Tooltip("Duration of the overall fade when the system turns off.")]
    public float globalFadeOut = 1.5f;     // Duración del fade-out global cuando el sistema se desactiva     // Duración del fade-out global cuando el sistema se desactiva

    private Coroutine loopRoutine;         // Referencia a la corrutina del ciclo de spotlight
    private Coroutine globalFadeRoutine;   // Referencia a la corrutina del fade global
    private float globalFadeFactor = 0f;   // Factor de fade global: 0 = silencio, 1 = volumen completo
    private bool isFadingOut = false;      // Indica si el sistema está en proceso de fade-out      // Indica si el sistema está en proceso de fade-out

    // Inicializa el sistema: configura las fuentes de audio, inicia el fade-in global y comienza el ciclo de spotlight
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

    // Detiene el sistema: cancela las corrutinas activas e inicia el fade-out global
    private void OnDisable()
    {
        if (loopRoutine != null) StopCoroutine(loopRoutine);
        if (globalFadeRoutine != null) StopCoroutine(globalFadeRoutine);
        isFadingOut = true;
        globalFadeRoutine = StartCoroutine(GlobalFade(globalFadeFactor, 0f, globalFadeOut));
    }

    // Ciclo principal que rota el spotlight entre las fuentes de audio, haciendo crossfade y esperando el tiempo asignado a cada una
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

    // Realiza un crossfade hacia la fuente especificada, ajustando los volúmenes de todas las fuentes suavemente
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

    // Controla el fade global del sistema, escalando todos los volúmenes gradualmente entre los valores especificados
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
