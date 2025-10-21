using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RollingAudioLayers : MonoBehaviour
{
    [Header("Audio Sources for Each Speed")]
    [SerializeField] private AudioSource slowAS;
    [SerializeField] private AudioSource mediumAS;
    [SerializeField] private AudioSource fastAS;

    [Header("Speed thresholds (m/s)")]
    public float slowMax = 2f;
    public float mediumMax = 6f;

    [Header("Fade speed")]
    public float fadeLerp = 5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        StartAll();
    }

    void StartAll()
    {
        if (slowAS && !slowAS.isPlaying) slowAS.Play();
        if (mediumAS && !mediumAS.isPlaying) mediumAS.Play();
        if (fastAS && !fastAS.isPlaying) fastAS.Play();
    }

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

    void OnDisable()
    {
        if (slowAS) slowAS.Stop();
        if (mediumAS) mediumAS.Stop();
        if (fastAS) fastAS.Stop();
    }
}

