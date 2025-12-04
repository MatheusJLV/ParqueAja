using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

[DisallowMultipleComponent]
public class VanDerGrafEnhanced : MonoBehaviour
{
    //  VFX (cuerpo / varita) 
    [Header("VFX (Generador / Varita)")]
    [Tooltip("VisualEffect del cuerpo (EstaticVanDer.vfx)")]
    public VisualEffect generadorVFX;

    [Tooltip("VisualEffect de la varita (opcional)")]
    public VisualEffect varitaVFX;

    [Header("Valores en runtime (cuerpo)")]
    [Min(0)] public float spawnRate = 60f;
    [Min(0)] public float lifetimeMin = 0.5f;
    [Min(0)] public float lifetimeMax = 1.5f;
    [Min(0)] public float noiseIntensity = 1f;
    [Min(0)] public float attractorStrength = 20f; // varita no lo usa por ahora

    [Header("UI (Sliders Generador)")]
    public Slider spawnRateSD;
    public Slider lifetimeMinSD;
    public Slider lifetimeMaxSD;
    public Slider noiseIntensitySD;
    public Slider attractorStrengthSD;

    [Header("Aplicar (ambos)")]
    public Button aplicarBTN;

    [Header("Opciones")]
    public bool debugLogs = true;

    [Header("Proporciones para VARITA")]
    public float wandSpawnFactor = 0.5f;        // = /2
    public float wandLifetimeMinFactor = 1.6f;  // = *8/5
    public float wandNoiseFactor = 0.5f;        // = /2

    // Blackboard IDs (mismos nombres en ambos VFX)
    static readonly int ID_SpawnRate = Shader.PropertyToID("SpawnRate");
    static readonly int ID_LifetimeMin = Shader.PropertyToID("LifetimeMin");
    static readonly int ID_LifetimeMax = Shader.PropertyToID("LifetimeMax");
    static readonly int ID_NoiseIntensity = Shader.PropertyToID("NoiseIntensity");
    static readonly int ID_AttractorStrength = Shader.PropertyToID("AttractorStrength");

    //  Chidori (ParticleSystem) 
    [Header("Chidori (ParticleSystem)")]
    [Tooltip("PS del Chidori fino (obligatorio para estos controles)")]
    public ParticleSystem chidoriThinPS;

    [Tooltip("PS del Chidori grueso (opcional; se aplica el mismo control)")]
    public ParticleSystem chidoriThickPS;

    [Header("UI (Sliders Chidori)")]
    public Slider chiSimSpeedSD;       // Main  Simulation Speed
    public Slider chiMaxParticlesSD;   // Main  Max Particles (entero)
    public Slider chiNoiseStrengthSD;  // Noise  Strength
    public Slider chiNoiseFreqSD;      // Noise  Frequency
    public Slider chiRateOverTimeSD;   // Emission  Rate over Time
    public Slider chiHueSD;            // Color (Hue 0..1)
    //public Image chiHueFill;          // Imagen para tintar el fill (como en tu ejemplo)

    [Header("Valores en runtime (Chidori)")]
    [Min(0)] public float chiSimSpeed = 5f;
    [Min(1)] public int chiMaxParticles = 15;
    [Min(0)] public float chiNoiseStrength = 5f;
    [Min(0)] public float chiNoiseFrequency = 5f;
    [Min(0)] public float chiRateOverTime = 80f;
    [Range(0, 1)] public float chiHue = 0.6f; // azul por defecto

    // 

    void Start()
    {
        // Suscribir UI (Generador)
        if (spawnRateSD) spawnRateSD.onValueChanged.AddListener(OnSpawnRateChanged);
        if (lifetimeMinSD) lifetimeMinSD.onValueChanged.AddListener(OnLifetimeMinChanged);
        if (lifetimeMaxSD) lifetimeMaxSD.onValueChanged.AddListener(OnLifetimeMaxChanged);
        if (noiseIntensitySD) noiseIntensitySD.onValueChanged.AddListener(OnNoiseIntensityChanged);
        if (attractorStrengthSD) attractorStrengthSD.onValueChanged.AddListener(OnAttractorStrengthChanged);
        if (aplicarBTN) aplicarBTN.onClick.AddListener(ApplyAllFromUI);

        // Suscribir UI (Chidori)
        if (chiSimSpeedSD) chiSimSpeedSD.onValueChanged.AddListener(OnChiSimSpeedChanged);
        if (chiMaxParticlesSD) chiMaxParticlesSD.onValueChanged.AddListener(OnChiMaxParticlesChanged);
        if (chiNoiseStrengthSD) chiNoiseStrengthSD.onValueChanged.AddListener(OnChiNoiseStrengthChanged);
        if (chiNoiseFreqSD) chiNoiseFreqSD.onValueChanged.AddListener(OnChiNoiseFreqChanged);
        if (chiRateOverTimeSD) chiRateOverTimeSD.onValueChanged.AddListener(OnChiRateChanged);
        /*if (chiHueSD)
        {
            chiHueSD.onValueChanged.AddListener(OnChiHueChanged);
            // inicializar color de fill y PS
            OnChiHueChanged(chiHueSD.value);
        }*/

        PushValuesToUI();
        ApplyAll();       // VFX
        ApplyChidori();   // PS
    }

    void OnDestroy()
    {
        if (spawnRateSD) spawnRateSD.onValueChanged.RemoveListener(OnSpawnRateChanged);
        if (lifetimeMinSD) lifetimeMinSD.onValueChanged.RemoveListener(OnLifetimeMinChanged);
        if (lifetimeMaxSD) lifetimeMaxSD.onValueChanged.RemoveListener(OnLifetimeMaxChanged);
        if (noiseIntensitySD) noiseIntensitySD.onValueChanged.RemoveListener(OnNoiseIntensityChanged);
        if (attractorStrengthSD) attractorStrengthSD.onValueChanged.RemoveListener(OnAttractorStrengthChanged);
        if (aplicarBTN) aplicarBTN.onClick.RemoveListener(ApplyAllFromUI);

        if (chiSimSpeedSD) chiSimSpeedSD.onValueChanged.RemoveListener(OnChiSimSpeedChanged);
        if (chiMaxParticlesSD) chiMaxParticlesSD.onValueChanged.RemoveListener(OnChiMaxParticlesChanged);
        if (chiNoiseStrengthSD) chiNoiseStrengthSD.onValueChanged.RemoveListener(OnChiNoiseStrengthChanged);
        if (chiNoiseFreqSD) chiNoiseFreqSD.onValueChanged.RemoveListener(OnChiNoiseFreqChanged);
        if (chiRateOverTimeSD) chiRateOverTimeSD.onValueChanged.RemoveListener(OnChiRateChanged);
        //if (chiHueSD) chiHueSD.onValueChanged.RemoveListener(OnChiHueChanged);
    }

    void OnValidate()
    {
        if (lifetimeMax < lifetimeMin) lifetimeMax = lifetimeMin;
        if (chiMaxParticles < 1) chiMaxParticles = 1;

        if (isActiveAndEnabled)
        {
            PushValuesToUI();
            ApplyAll();
            ApplyChidori();
        }
    }

    //  Handlers (Generador / Varita) 
    void OnSpawnRateChanged(float v)
    {
        spawnRate = Mathf.Max(0f, v);
        SetFloat(generadorVFX, ID_SpawnRate, spawnRate);
        SetFloat(varitaVFX, ID_SpawnRate, spawnRate * wandSpawnFactor);
        if (debugLogs) Debug.Log($"[VDG] SpawnRate cuerpo={spawnRate} varita={spawnRate * wandSpawnFactor}");
    }

    void OnLifetimeMinChanged(float v)
    {
        lifetimeMin = Mathf.Max(0f, v);
        if (lifetimeMax < lifetimeMin)
        {
            lifetimeMax = lifetimeMin;
            if (lifetimeMaxSD) lifetimeMaxSD.SetValueWithoutNotify(lifetimeMax);
            SetFloat(generadorVFX, ID_LifetimeMax, lifetimeMax);
            SetFloat(varitaVFX, ID_LifetimeMax, lifetimeMax);
        }
        SetFloat(generadorVFX, ID_LifetimeMin, lifetimeMin);
        SetFloat(varitaVFX, ID_LifetimeMin, lifetimeMin * wandLifetimeMinFactor);
    }

    void OnLifetimeMaxChanged(float v)
    {
        lifetimeMax = Mathf.Max(lifetimeMin, v);
        SetFloat(generadorVFX, ID_LifetimeMax, lifetimeMax);
        SetFloat(varitaVFX, ID_LifetimeMax, lifetimeMax);
    }

    void OnNoiseIntensityChanged(float v)
    {
        noiseIntensity = Mathf.Max(0f, v);
        SetFloat(generadorVFX, ID_NoiseIntensity, noiseIntensity);
        SetFloat(varitaVFX, ID_NoiseIntensity, noiseIntensity * wandNoiseFactor);
    }

    void OnAttractorStrengthChanged(float v)
    {
        attractorStrength = Mathf.Max(0f, v);
        SetFloat(generadorVFX, ID_AttractorStrength, attractorStrength);
    }

    public void ApplyAllFromUI()
    {
        if (spawnRateSD) spawnRate = Mathf.Max(0f, spawnRateSD.value);
        if (lifetimeMinSD) lifetimeMin = Mathf.Max(0f, lifetimeMinSD.value);
        if (lifetimeMaxSD) lifetimeMax = Mathf.Max(lifetimeMin, lifetimeMaxSD.value);
        if (noiseIntensitySD) noiseIntensity = Mathf.Max(0f, noiseIntensitySD.value);
        if (attractorStrengthSD) attractorStrength = Mathf.Max(0f, attractorStrengthSD.value);

        if (chiSimSpeedSD) chiSimSpeed = Mathf.Max(0f, chiSimSpeedSD.value);
        if (chiMaxParticlesSD) chiMaxParticles = Mathf.Max(1, Mathf.RoundToInt(chiMaxParticlesSD.value));
        if (chiNoiseStrengthSD) chiNoiseStrength = Mathf.Max(0f, chiNoiseStrengthSD.value);
        if (chiNoiseFreqSD) chiNoiseFrequency = Mathf.Max(0f, chiNoiseFreqSD.value);
        if (chiRateOverTimeSD) chiRateOverTime = Mathf.Max(0f, chiRateOverTimeSD.value);
        if (chiHueSD) chiHue = Mathf.Clamp01(chiHueSD.value);

        PushValuesToUI();
        ApplyAll();
        ApplyChidori();

        if (debugLogs) Debug.Log("[VDG] ApplyAllFromUI  aplicado a VFX y Chidori PS.");
    }

    void ApplyAll()
    {
        SetFloat(generadorVFX, ID_SpawnRate, spawnRate);
        SetFloat(generadorVFX, ID_LifetimeMin, lifetimeMin);
        SetFloat(generadorVFX, ID_LifetimeMax, lifetimeMax);
        SetFloat(generadorVFX, ID_NoiseIntensity, noiseIntensity);
        SetFloat(generadorVFX, ID_AttractorStrength, attractorStrength);

        if (varitaVFX)
        {
            SetFloat(varitaVFX, ID_SpawnRate, spawnRate * wandSpawnFactor);
            SetFloat(varitaVFX, ID_LifetimeMin, lifetimeMin * wandLifetimeMinFactor);
            SetFloat(varitaVFX, ID_LifetimeMax, lifetimeMax);
            SetFloat(varitaVFX, ID_NoiseIntensity, noiseIntensity * wandNoiseFactor);
        }
    }

    void SetFloat(VisualEffect vfx, int id, float v)
    {
        if (!vfx) return;
        if (vfx.HasFloat(id)) vfx.SetFloat(id, v);
        else if (debugLogs) Debug.LogWarning($"[VDG] ({vfx.name}) falta propiedad float id={id}.");
    }

    void PushValuesToUI()
    {
        if (spawnRateSD) spawnRateSD.SetValueWithoutNotify(spawnRate);
        if (lifetimeMinSD) lifetimeMinSD.SetValueWithoutNotify(lifetimeMin);
        if (lifetimeMaxSD)
        {
            if (lifetimeMaxSD.minValue > lifetimeMin) lifetimeMaxSD.minValue = lifetimeMin;
            lifetimeMaxSD.SetValueWithoutNotify(lifetimeMax);
        }
        if (noiseIntensitySD) noiseIntensitySD.SetValueWithoutNotify(noiseIntensity);
        if (attractorStrengthSD) attractorStrengthSD.SetValueWithoutNotify(attractorStrength);

        if (chiSimSpeedSD) chiSimSpeedSD.SetValueWithoutNotify(chiSimSpeed);
        if (chiMaxParticlesSD) chiMaxParticlesSD.SetValueWithoutNotify(chiMaxParticles);
        if (chiNoiseStrengthSD) chiNoiseStrengthSD.SetValueWithoutNotify(chiNoiseStrength);
        if (chiNoiseFreqSD) chiNoiseFreqSD.SetValueWithoutNotify(chiNoiseFrequency);
        if (chiRateOverTimeSD) chiRateOverTimeSD.SetValueWithoutNotify(chiRateOverTime);
        if (chiHueSD) chiHueSD.SetValueWithoutNotify(chiHue);
        //if (chiHueFill) chiHueFill.color = Color.HSVToRGB(chiHue, 1f, 1f); // como en tu patrón de slider de color :contentReference[oaicite:1]{index=1}
    }

    //  Handlers (Chidori PS) 
    void OnChiSimSpeedChanged(float v) { chiSimSpeed = Mathf.Max(0f, v); ApplyChidori(); }
    void OnChiMaxParticlesChanged(float v) { chiMaxParticles = Mathf.Max(1, Mathf.RoundToInt(v)); ApplyChidori(); }
    void OnChiNoiseStrengthChanged(float v) { chiNoiseStrength = Mathf.Max(0f, v); ApplyChidori(); }
    void OnChiNoiseFreqChanged(float v) { chiNoiseFrequency = Mathf.Max(0f, v); ApplyChidori(); }
    void OnChiRateChanged(float v) { chiRateOverTime = Mathf.Max(0f, v); ApplyChidori(); }
    /*void OnChiHueChanged(float v)
    {
        chiHue = Mathf.Clamp01(v);
        if (chiHueFill) chiHueFill.color = Color.HSVToRGB(chiHue, 1f, 1f); // actualiza el fill del slider (igual que tu ejemplo) :contentReference[oaicite:2]{index=2}
        ApplyChidori();
    }*/

    //  Aplicación a los ParticleSystems 
    void ApplyChidori()
    {
        ApplyChidoriTo(chidoriThinPS);
        ApplyChidoriTo(chidoriThickPS);
    }

    void ApplyChidoriTo(ParticleSystem ps)
    {
        if (!ps) return;

        var main = ps.main;
        main.simulationSpeed = chiSimSpeed;
        main.maxParticles = chiMaxParticles;

        // Color (Hue - RGB) aplicado a StartColor y, si existe, al material de trails
        Color c = Color.HSVToRGB(chiHue, 1f, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(c);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        if (rend)
        {
            // Material principal
            if (rend.material && rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", c);
            else if (rend.material) rend.material.color = c;

            // Trail material (si existe)
            if (rend.trailMaterial)
            {
                if (rend.trailMaterial.HasProperty("_BaseColor")) rend.trailMaterial.SetColor("_BaseColor", c);
                else rend.trailMaterial.color = c;
            }
        }

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(chiRateOverTime);

        // Noise
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(chiNoiseStrength);
        noise.frequency = chiNoiseFrequency;

        if (debugLogs) Debug.Log($"[VDG] Chidori aplicado  sim={chiSimSpeed}, max={chiMaxParticles}, noiseS={chiNoiseStrength}, noiseF={chiNoiseFrequency}, rate={chiRateOverTime}, hue={chiHue}");
    }
}
