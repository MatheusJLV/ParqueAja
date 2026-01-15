using UnityEngine;

// Sistema de capas de audio para objetos rodantes que mezcla dinámicamente diferentes pistas de audio
// basándose en la velocidad del objeto, creando una transición suave entre sonidos lentos, medios y rápidos.
[RequireComponent(typeof(Rigidbody))]
public class RollingAudioLayers : MonoBehaviour
{
    [Header("Audio Sources for Each Speed")]
    [SerializeField] private AudioSource slowAS;      // Fuente de audio para velocidades lentas
    [SerializeField] private AudioSource mediumAS;    // Fuente de audio para velocidades medias
    [SerializeField] private AudioSource fastAS;      // Fuente de audio para velocidades rápidas

    [Header("Speed thresholds (m/s)")]
    public float slowMax = 2f;      // Velocidad máxima (m/s) considerada como "lenta"
    public float mediumMax = 6f;    // Velocidad máxima (m/s) considerada como "media"

    [Header("Fade speed")]
    public float fadeLerp = 5f;     // Velocidad de interpolación para transiciones de volumen

    private Rigidbody rb;           // Referencia al componente Rigidbody del objeto           // Referencia al componente Rigidbody del objeto

    // Inicializa las referencias y arranca todas las pistas de audio al despertar el componente.
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        StartAll();
    }

    // Inicia la reproducción de todas las fuentes de audio si no están ya reproduciéndose.
    void StartAll()
    {
        if (slowAS && !slowAS.isPlaying) slowAS.Play();
        if (mediumAS && !mediumAS.isPlaying) mediumAS.Play();
        if (fastAS && !fastAS.isPlaying) fastAS.Play();
    }

    // Actualiza el volumen de cada capa de audio cada frame basándose en la velocidad actual del objeto.
    // Calcula factores de mezcla para cada capa y los normaliza para mantener un volumen consistente.
    void Update()
    {
        float v = rb.linearVelocity.magnitude;

        // Determine blend factors (0–1) between layers
        float slowT = Mathf.Clamp01(1f - v / slowMax);
        float medT = Mathf.Clamp01((v - slowMax) / (mediumMax - slowMax));
        float fastT = Mathf.Clamp01((v - mediumMax) / (mediumMax)); // grows beyond medium

        // Normalize blend roughly so total = 1 (optional)
        float total = slowT + medT + fastT;
        if (total < 0.001f) total = 1f;

        slowT /= total; medT /= total; fastT /= total;

        // Smooth volume changes
        if (slowAS) slowAS.volume = Mathf.MoveTowards(slowAS.volume, slowT, fadeLerp * Time.deltaTime);
        if (mediumAS) mediumAS.volume = Mathf.MoveTowards(mediumAS.volume, medT, fadeLerp * Time.deltaTime);
        if (fastAS) fastAS.volume = Mathf.MoveTowards(fastAS.volume, fastT, fadeLerp * Time.deltaTime);
    }

    // Detiene todas las pistas de audio cuando el componente se deshabilita.
    void OnDisable()
    {
        if (slowAS) slowAS.Stop();
        if (mediumAS) mediumAS.Stop();
        if (fastAS) fastAS.Stop();
    }
}

