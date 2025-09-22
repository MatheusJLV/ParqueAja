using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class GaltonPSTrailController : MonoBehaviour
{
    [Header("Trail core")]
    [Range(0.1f, 5f)] public float trailTime = 1.2f;
    [Range(0.005f, 0.2f)] public float minVertexDistance = 0.03f;

    [Header("Thickness (real-world)")]
    [Tooltip("Visual trail thickness in METERS")]
    [Range(0.001f, 2f)] public float thicknessMeters = 0.04f; // 4cm default
    [Tooltip("How many scene units equal 1 meter (set 100 if your scene is 100u = 1m)")]
    [Min(0.0001f)] public float worldUnitsPerMeter = 1f;

    [Header("Width by speed (optional)")]
    public bool widthBySpeed = true;
    [Tooltip("Trail width at 0 m/s (meters)")] public float widthMinMeters = 0.03f;
    [Tooltip("Trail width at speedMax (meters)")] public float widthMaxMeters = 0.10f;
    [Tooltip("Speed (m/s) that maps to widthMax")] public float speedMax = 5f;
    [Tooltip("Smoothing time for width changes (seconds)")][Range(0.0f, 0.5f)] public float widthSmooth = 0.08f;

    [Header("Color (random on start)")]
    public bool randomizeColorOnStart = true;
    [Range(0f, 1f)] public float saturation = 1f;
    [Range(0f, 1f)] public float value = 1f;
    [Range(0f, 1f)] public float startAlpha = 1f;
    [Range(0f, 1f)] public float endAlpha = 0f;

    [Header("Goal detection")]
    public string goalTag = "Goal";
    public string goalObjectName = "GoalZone";
    public bool preferTagCheck = true;
    [Range(0f, 1f)] public float fadeOutSeconds = 0.25f;

    [Header("Glow (optional)")]
    [Tooltip("Multiply trail material base color for Bloom punch (URP only)")]
    [Range(1f, 10f)] public float hdrIntensity = 4f;

    ParticleSystem ps;
    ParticleSystemRenderer psr;
    Rigidbody rb;
    float currentMeters; // smoothed width

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (!ps) ps = gameObject.AddComponent<ParticleSystem>();
        psr = GetComponent<ParticleSystemRenderer>();
        rb = GetComponent<Rigidbody>();

        // ?? Main ??
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = 1;
        main.startLifetime = 9999f;
        main.startSpeed = 0f;
        main.startSize = 0.001f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.useUnscaledTime = false;

        // ?? Emission ??
        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new(0f, 1) });

        // ?? Shape off ??
        var shape = ps.shape; shape.enabled = false;

        // ?? Trails ??
        var trails = ps.trails;
        trails.enabled = true;
        trails.worldSpace = true;             // stereo safe
        trails.dieWithParticles = true;
        trails.ratio = 1f;
        trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        trails.lifetime = trailTime;
        trails.minVertexDistance = minVertexDistance;

        // initial width
        currentMeters = thicknessMeters;
        ApplyTrailWidthMeters(currentMeters);

        // ?? Renderer ??
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sortMode = ParticleSystemSortMode.OldestInFront;
        psr.enableGPUInstancing = true;
        psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psr.receiveShadows = false;

        EnsureURPMaterial(); // makes sure no pink & applies HDR intensity
    }

    void OnEnable()
    {
        ps.Clear(true);

        if (randomizeColorOnStart)
        {
            float h = Random.value;
            var baseC = Color.HSVToRGB(h, Mathf.Clamp01(saturation), Mathf.Clamp01(value));
            ApplyTrailGradient(baseC, startAlpha, endAlpha);
        }

        ps.Play(true);
    }

    void Update()
    {
        // Optional: widen by speed
        if (widthBySpeed && rb)
        {
            float t = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(0.0001f, speedMax));
            float targetMeters = Mathf.Lerp(widthMinMeters, widthMaxMeters, t);
            // smooth change
            currentMeters = Mathf.Lerp(currentMeters, targetMeters, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, widthSmooth)));
            ApplyTrailWidthMeters(currentMeters);
        }
    }

    // Width curve 1?0, with world-scale conversion
    void ApplyTrailWidthMeters(float meters)
    {
        float wUnits = meters * worldUnitsPerMeter;
        var curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        var trails = ps.trails;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(wUnits, curve);
    }

    void ApplyTrailGradient(Color c, float a0, float a1)
    {
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(Mathf.Clamp01(a0), 0f), new GradientAlphaKey(Mathf.Clamp01(a1), 1f) }
        );
        var trails = ps.trails;
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.Gradient };
    }

    void EnsureURPMaterial()
    {
        if (!psr) return;
        var m = psr.trailMaterial;
        bool broken = (m == null) || (m.shader == null) || m.shader.name == "Hidden/InternalErrorShader";
        if (broken)
        {
            var s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s != null)
            {
                m = new Material(s);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white * hdrIntensity);
                psr.trailMaterial = m;
            }
        }
        else if (m.HasProperty("_BaseColor"))
        {
            // Punch up existing material for Bloom
            var baseCol = m.GetColor("_BaseColor");
            m.SetColor("_BaseColor", baseCol.maxColorComponent < 1.01f ? Color.white * hdrIntensity : baseCol);
        }
    }

    // ?? Stop trail on Goal ??
    void OnTriggerEnter(Collider other) { if (IsGoal(other)) StartCoroutine(FadeOutAndClear()); }
    void OnCollisionEnter(Collision col) { if (IsGoal(col.collider)) StartCoroutine(FadeOutAndClear()); }

    bool IsGoal(Collider c)
    {
        if (preferTagCheck && !string.IsNullOrEmpty(goalTag) && c.CompareTag(goalTag)) return true;
        if (!string.IsNullOrEmpty(goalObjectName) && c.name == goalObjectName) return true;
        if (c.transform.parent && c.transform.parent.name == goalObjectName) return true;
        return false;
    }

    IEnumerator FadeOutAndClear()
    {
        float t0 = Time.time;
        var trails = ps.trails;
        float start = trails.lifetime.constant; // Access the 'constant' property of MinMaxCurve  

        while (Time.time - t0 < fadeOutSeconds)
        {
            float k = 1f - Mathf.Clamp01((Time.time - t0) / Mathf.Max(0.0001f, fadeOutSeconds));
            trails.lifetime = new ParticleSystem.MinMaxCurve(k * start); // Create a new MinMaxCurve with the updated value  
            yield return null;
        }

        trails.lifetime = new ParticleSystem.MinMaxCurve(0f); // Set lifetime to 0 using MinMaxCurve  
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        enabled = false;
    }
}
