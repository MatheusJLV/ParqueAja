using System.Collections;
using UnityEngine;

// Reproduce un sonido en loop de "deslizamiento" cuando este objeto mantiene contacto tangencial.
// Diseñado para deslizamientos cortos (1-5 segundos), muy ligero en rendimiento.
[RequireComponent(typeof(AudioSource))]
public class SlidingSound : MonoBehaviour
{
    public AudioSource audioSource;          // Fuente de audio que reproduce el sonido de deslizamiento
    [Tooltip("Minimum tangential velocity to start sliding sound.")]
    public float slideThreshold = 0.15f;     // Velocidad tangencial mínima para iniciar el sonido de deslizamiento
    [Tooltip("Seconds to fade in/out when starting or stopping.")]
    public float fadeDuration = 0.3f;        // Duración del fade in/out al iniciar o detener el sonido
    [Tooltip("Stop the loop if no sliding for this long.")]
    public float idleTimeout = 0.5f;         // Tiempo sin deslizamiento antes de detener el sonido         // Tiempo sin deslizamiento antes de detener el sonido

    private float lastSlideTime;             // Último momento en que se detectó deslizamiento
    private Coroutine fadeRoutine;           // Referencia a la corrutina de fade actualmente en ejecución
    private bool isSliding = false;          // Indica si el objeto está actualmente deslizándose          // Indica si el objeto está actualmente deslizándose

    // Inicializa el AudioSource con configuración para loop y volumen en 0
    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    // Detecta el deslizamiento calculando la velocidad tangencial del contacto e inicia el sonido si supera el umbral
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

    // Verifica si el deslizamiento se ha detenido por tiempo suficiente para hacer fade out del sonido
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

    // Corrutina que hace fade in del volumen del audio gradualmente
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

    // Corrutina que hace fade out del volumen del audio gradualmente y detiene la reproducción
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
