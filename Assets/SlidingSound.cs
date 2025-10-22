using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a looping "slide" sound when this object maintains tangential contact.
/// Designed for short-lived slides (1–5 seconds), very light on performance.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SlidingSound : MonoBehaviour
{
    public AudioSource audioSource;
    [Tooltip("Minimum tangential velocity to start sliding sound.")]
    public float slideThreshold = 0.15f;
    [Tooltip("Seconds to fade in/out when starting or stopping.")]
    public float fadeDuration = 0.3f;
    [Tooltip("Stop the loop if no sliding for this long.")]
    public float idleTimeout = 0.5f;

    private float lastSlideTime;
    private Coroutine fadeRoutine;
    private bool isSliding = false;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    void OnCollisionStay(Collision collision)
    {
        // Use relative velocity projected onto contact tangent.
        Vector3 relVel = collision.relativeVelocity;
        Vector3 normal = collision.contacts[0].normal;
        float tangentialSpeed = (relVel - Vector3.Dot(relVel, normal) * normal).magnitude;

        if (tangentialSpeed > slideThreshold)
        {
            lastSlideTime = Time.time;
            if (!isSliding)
            {
                isSliding = true;
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeIn());
            }
        }
    }

    void Update()
    {
        // Check if slide has stopped long enough
        if (isSliding && Time.time - lastSlideTime > idleTimeout)
        {
            isSliding = false;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeIn()
    {
        if (!audioSource.isPlaying) audioSource.Play();
        float start = Time.realtimeSinceStartup;
        while (audioSource.volume < 1f)
        {
            float t = (Time.realtimeSinceStartup - start) / fadeDuration;
            audioSource.volume = Mathf.Clamp01(t);
            yield return null;
        }
        audioSource.volume = 1f;
    }

    private IEnumerator FadeOut()
    {
        float startVol = audioSource.volume;
        float start = Time.realtimeSinceStartup;
        while (audioSource.volume > 0f)
        {
            float t = (Time.realtimeSinceStartup - start) / fadeDuration;
            audioSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop();
    }
}
