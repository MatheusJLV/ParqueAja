using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

// Sistema mejorado de Van Der Graaf que controla efectos visuales (VFX) del generador y varita,
// así como sistemas de partículas (Chidori). Gestiona parámetros en tiempo real mediante sliders de UI,
// aplicando proporciones diferentes entre el generador principal y la varita secundaria.
[DisallowMultipleComponent]
public class VanDerGrafEnhanced : MonoBehaviour
{
    //  VFX (cuerpo / varita) 
    [Header("VFX (Generador / Varita)")]
    [Tooltip("VisualEffect del cuerpo (EstaticVanDer.vfx)")]
    public VisualEffect generadorVFX;              // VFX principal del generador Van Der Graaf

    [Tooltip("VisualEffect de la varita (opcional)")]
    public VisualEffect varitaVFX;                 // VFX secundario de la varita (con proporciones distintas)

    [Header("Valores en runtime (cuerpo)")]
    [Min(0)] public float spawnRate = 60f;         // Tasa de generación de partículas por segundo
    [Min(0)] public float lifetimeMin = 0.5f;      // Tiempo de vida mínimo de las partículas (segundos)
    [Min(0)] public float lifetimeMax = 1.5f;      // Tiempo de vida máximo de las partículas (segundos)
    [Min(0)] public float noiseIntensity = 1f;     // Intensidad del ruido aplicado al movimiento
    [Min(0)] public float attractorStrength = 20f; // Fuerza del atractor (varita no lo usa por ahora)

    [Header("UI (Sliders Generador)")]
    public Slider spawnRateSD;                     // Slider para controlar la tasa de spawn
    public Slider lifetimeMinSD;                   // Slider para controlar el tiempo de vida mínimo
    public Slider lifetimeMaxSD;                   // Slider para controlar el tiempo de vida máximo
    public Slider noiseIntensitySD;                // Slider para controlar la intensidad del ruido
    public Slider attractorStrengthSD;             // Slider para controlar la fuerza del atractor

    [Header("Aplicar (ambos)")]
    public Button aplicarBTN;                      // Botón para aplicar todos los valores de los sliders a VFX y PS

    [Header("Opciones")]
    public bool debugLogs = true;                  // Activar/desactivar logs de depuración en consola

    [Header("Reset Transform")]
    [Tooltip("Objeto cuya posición y rotación se guardan al inicio y se restauran al resetear.")]
    public GameObject objetoParaReset;

    [Header("Proporciones para VARITA")]
    public float wandSpawnFactor = 0.5f;           // Factor multiplicador para spawn de varita (÷2)
    public float wandLifetimeMinFactor = 1.6f;     // Factor multiplicador para lifetime mínimo de varita (×8/5)
    public float wandNoiseFactor = 0.5f;           // Factor multiplicador para ruido de varita (÷2)

    // Blackboard IDs (mismos nombres en ambos VFX)
    static readonly int ID_SpawnRate = Shader.PropertyToID("SpawnRate");
    static readonly int ID_LifetimeMin = Shader.PropertyToID("LifetimeMin");
    static readonly int ID_LifetimeMax = Shader.PropertyToID("LifetimeMax");
    static readonly int ID_NoiseIntensity = Shader.PropertyToID("NoiseIntensity");
    static readonly int ID_AttractorStrength = Shader.PropertyToID("AttractorStrength");

    //  Chidori (ParticleSystem) 
    [Header("Chidori (ParticleSystem)")]
    [Tooltip("PS del Chidori fino (obligatorio para estos controles)")]
    public ParticleSystem chidoriThinPS;           // Sistema de partículas del efecto Chidori fino

    [Tooltip("PS del Chidori grueso (opcional; se aplica el mismo control)")]
    public ParticleSystem chidoriThickPS;          // Sistema de partículas del efecto Chidori grueso (opcional)

    [Header("UI (Sliders Chidori)")]
    public Slider chiSimSpeedSD;                   // Slider para velocidad de simulación (Main)
    public Slider chiMaxParticlesSD;               // Slider para número máximo de partículas (Main, entero)
    public Slider chiNoiseStrengthSD;              // Slider para fuerza del ruido (Noise)
    public Slider chiNoiseFreqSD;                  // Slider para frecuencia del ruido (Noise)
    public Slider chiRateOverTimeSD;               // Slider para tasa de emisión (Emission)
    public Slider chiHueSD;                        // Slider para tono de color (Hue 0..1)
    //public Image chiHueFill;                     // Imagen para tintar el fill del slider

    [Header("Valores en runtime (Chidori)")]
    [Min(0)] public float chiSimSpeed = 5f;        // Velocidad de simulación del sistema de partículas
    [Min(1)] public int chiMaxParticles = 15;      // Número máximo de partículas simultáneas
    [Min(0)] public float chiNoiseStrength = 5f;   // Fuerza del módulo de ruido
    [Min(0)] public float chiNoiseFrequency = 5f;  // Frecuencia del módulo de ruido
    [Min(0)] public float chiRateOverTime = 80f;   // Tasa de emisión de partículas por segundo
    [Range(0, 1)] public float chiHue = 0.6f;      // Tono del color (azul por defecto)
                                                   // ----------------------------
                                                   // Default snapshot / reset
                                                   // ----------------------------
    [System.Serializable]
    private struct DefaultState
    {
        // Generador
        public float spawnRate;
        public float lifetimeMin;
        public float lifetimeMax;
        public float noiseIntensity;
        public float attractorStrength;

        // Varita ratios
        public float wandSpawnFactor;
        public float wandLifetimeMinFactor;
        public float wandNoiseFactor;

        // Chidori
        public float chiSimSpeed;
        public int chiMaxParticles;
        public float chiNoiseStrength;
        public float chiNoiseFrequency;
        public float chiRateOverTime;
        public float chiHue;

        // Transform snapshot
        public bool hasTransform;
        public Vector3 savedPosition;
        public Quaternion savedRotation;



        // Options
        public bool debugLogs;
    }

    private DefaultState _defaults;
    private bool _defaultsCaptured = false;

    /// <summary>
    /// Captures the current public/runtime values as "defaults".
    /// Start() calls this automatically.
    /// </summary>
    public void SaveDefaults()
    {
        _defaults = new DefaultState
        {
            spawnRate = spawnRate,
            lifetimeMin = lifetimeMin,
            lifetimeMax = lifetimeMax,
            noiseIntensity = noiseIntensity,
            attractorStrength = attractorStrength,

            wandSpawnFactor = wandSpawnFactor,
            wandLifetimeMinFactor = wandLifetimeMinFactor,
            wandNoiseFactor = wandNoiseFactor,

            chiSimSpeed = chiSimSpeed,
            chiMaxParticles = chiMaxParticles,
            chiNoiseStrength = chiNoiseStrength,
            chiNoiseFrequency = chiNoiseFrequency,
            chiRateOverTime = chiRateOverTime,
            chiHue = chiHue,

            hasTransform = objetoParaReset != null,
            savedPosition = objetoParaReset != null ? objetoParaReset.transform.position : Vector3.zero,
            savedRotation = objetoParaReset != null ? objetoParaReset.transform.rotation : Quaternion.identity,

            debugLogs = debugLogs
        };

        _defaultsCaptured = true;
    }

    /// <summary>
    /// Resets all runtime values back to the last captured defaults, pushes them to UI, and reapplies to VFX/PS.
    /// </summary>
    public void ResetToSavedDefaults()
    {
        if (!_defaultsCaptured)
            SaveDefaults();

        // Generador
        spawnRate = _defaults.spawnRate;
        lifetimeMin = _defaults.lifetimeMin;
        lifetimeMax = _defaults.lifetimeMax;
        noiseIntensity = _defaults.noiseIntensity;
        attractorStrength = _defaults.attractorStrength;

        // Varita ratios
        wandSpawnFactor = _defaults.wandSpawnFactor;
        wandLifetimeMinFactor = _defaults.wandLifetimeMinFactor;
        wandNoiseFactor = _defaults.wandNoiseFactor;

        // Chidori
        chiSimSpeed = _defaults.chiSimSpeed;
        chiMaxParticles = _defaults.chiMaxParticles;
        chiNoiseStrength = _defaults.chiNoiseStrength;
        chiNoiseFrequency = _defaults.chiNoiseFrequency;
        chiRateOverTime = _defaults.chiRateOverTime;
        chiHue = _defaults.chiHue;

        // Options
        debugLogs = _defaults.debugLogs;

        // Transform
        if (_defaults.hasTransform && objetoParaReset != null)
        {
            objetoParaReset.transform.position = _defaults.savedPosition;
            objetoParaReset.transform.rotation = _defaults.savedRotation;
        }

        // Ensure constraints
        if (lifetimeMax < lifetimeMin) lifetimeMax = lifetimeMin;
        if (chiMaxParticles < 1) chiMaxParticles = 1;

        PushValuesToUI();
        ApplyAll();
        ApplyChidori();
    }

    // Inicializa el sistema suscribiendo listeners de los sliders y aplicando valores iniciales a VFX y PS
    void Start()
    {
        // Capture inspector/runtime defaults once at startup
        SaveDefaults();
        // Suscribir listeners de UI (Generador)
        if (spawnRateSD) spawnRateSD.onValueChanged.AddListener(OnSpawnRateChanged);
        if (lifetimeMinSD) lifetimeMinSD.onValueChanged.AddListener(OnLifetimeMinChanged);
        if (lifetimeMaxSD) lifetimeMaxSD.onValueChanged.AddListener(OnLifetimeMaxChanged);
        if (noiseIntensitySD) noiseIntensitySD.onValueChanged.AddListener(OnNoiseIntensityChanged);
        if (attractorStrengthSD) attractorStrengthSD.onValueChanged.AddListener(OnAttractorStrengthChanged);
        if (aplicarBTN) aplicarBTN.onClick.AddListener(ApplyAllFromUI);

        // Suscribir listeners de UI (Chidori)
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
        ApplyAll();       // Aplicar valores iniciales a VFX
        ApplyChidori();   // Aplicar valores iniciales a PS
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

    // Valida los valores en el Inspector y aplica los cambios automáticamente en modo edición
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

    //  Handlers de sliders (Generador / Varita) 

    // Maneja cambios en el slider de tasa de spawn, actualizando VFX del generador y varita con sus respectivas proporciones
    void OnSpawnRateChanged(float v)
    {
        spawnRate = Mathf.Max(0f, v);
        SetFloat(generadorVFX, ID_SpawnRate, spawnRate);
        SetFloat(varitaVFX, ID_SpawnRate, spawnRate * wandSpawnFactor);
        if (debugLogs) Debug.Log($"[VDG] SpawnRate cuerpo={spawnRate} varita={spawnRate * wandSpawnFactor}");
    }

    // Maneja cambios en el slider de tiempo de vida mínimo, asegurando que no exceda el máximo
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

    // Maneja cambios en el slider de tiempo de vida máximo, asegurando que no sea menor que el mínimo
    void OnLifetimeMaxChanged(float v)
    {
        lifetimeMax = Mathf.Max(lifetimeMin, v);
        SetFloat(generadorVFX, ID_LifetimeMax, lifetimeMax);
        SetFloat(varitaVFX, ID_LifetimeMax, lifetimeMax);
    }

    // Maneja cambios en el slider de intensidad de ruido, aplicando proporciones diferentes a generador y varita
    void OnNoiseIntensityChanged(float v)
    {
        noiseIntensity = Mathf.Max(0f, v);
        SetFloat(generadorVFX, ID_NoiseIntensity, noiseIntensity);
        SetFloat(varitaVFX, ID_NoiseIntensity, noiseIntensity * wandNoiseFactor);
    }

    // Maneja cambios en el slider de fuerza del atractor (solo aplica al generador)
    void OnAttractorStrengthChanged(float v)
    {
        attractorStrength = Mathf.Max(0f, v);
        SetFloat(generadorVFX, ID_AttractorStrength, attractorStrength);
    }

    // Lee todos los valores de los sliders, los valida y aplica a los VFX y sistemas de partículas
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

    // Aplica todos los valores actuales a los VisualEffect del generador y varita
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

    // Establece un valor float en el VisualEffect si existe la propiedad, con manejo de errores opcional
    void SetFloat(VisualEffect vfx, int id, float v)
    {
        if (!vfx) return;
        if (vfx.HasFloat(id)) vfx.SetFloat(id, v);
        else if (debugLogs) Debug.LogWarning($"[VDG] ({vfx.name}) falta propiedad float id={id}.");
    }

    // Actualiza los sliders con los valores actuales sin disparar eventos de cambio
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
        //if (chiHueFill) chiHueFill.color = Color.HSVToRGB(chiHue, 1f, 1f); // como en el patrón de slider de color
    }

    //  Handlers de sliders (Chidori PS) 

    // Maneja cambios en el slider de velocidad de simulación de Chidori
    void OnChiSimSpeedChanged(float v) { chiSimSpeed = Mathf.Max(0f, v); ApplyChidori(); }

    // Maneja cambios en el slider de número máximo de partículas de Chidori
    void OnChiMaxParticlesChanged(float v) { chiMaxParticles = Mathf.Max(1, Mathf.RoundToInt(v)); ApplyChidori(); }

    // Maneja cambios en el slider de fuerza de ruido de Chidori
    void OnChiNoiseStrengthChanged(float v) { chiNoiseStrength = Mathf.Max(0f, v); ApplyChidori(); }

    // Maneja cambios en el slider de frecuencia de ruido de Chidori
    void OnChiNoiseFreqChanged(float v) { chiNoiseFrequency = Mathf.Max(0f, v); ApplyChidori(); }

    // Maneja cambios en el slider de tasa de emisión de Chidori
    void OnChiRateChanged(float v) { chiRateOverTime = Mathf.Max(0f, v); ApplyChidori(); }

    // Maneja cambios en el slider de tono de color de Chidori (comentado)
    /*void OnChiHueChanged(float v)
    {
        chiHue = Mathf.Clamp01(v);
        if (chiHueFill) chiHueFill.color = Color.HSVToRGB(chiHue, 1f, 1f); // actualiza el fill del slider (igual que tu ejemplo) :contentReference[oaicite:2]{index=2}
        ApplyChidori();
    }*/

    //  Aplicación a los ParticleSystems 

    // Aplica los valores actuales de Chidori a ambos sistemas de partículas (fino y grueso)
    void ApplyChidori()
    {
        ApplyChidoriTo(chidoriThinPS);
        ApplyChidoriTo(chidoriThickPS);
    }

    // Aplica todos los parámetros de Chidori a un sistema de partículas específico (velocidad, color, emisión, ruido)
    void ApplyChidoriTo(ParticleSystem ps)
    {
        if (!ps) return;

        var main = ps.main;
        main.simulationSpeed = chiSimSpeed;
        main.maxParticles = chiMaxParticles;

        // Color (Hue -> RGB) aplicado a StartColor y, si existe, al material de trails
        Color c = Color.HSVToRGB(chiHue, 1f, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(c);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        if (rend)
        {
            // Aplicar color al material principal
            if (rend.material && rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", c);
            else if (rend.material) rend.material.color = c;

            // Aplicar color al material de trail (si existe)
            if (rend.trailMaterial)
            {
                if (rend.trailMaterial.HasProperty("_BaseColor")) rend.trailMaterial.SetColor("_BaseColor", c);
                else rend.trailMaterial.color = c;
            }
        }

        // Configurar módulo de emisión
        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(chiRateOverTime);

        // Configurar módulo de ruido
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(chiNoiseStrength);
        noise.frequency = chiNoiseFrequency;

        if (debugLogs) Debug.Log($"[VDG] Chidori aplicado  sim={chiSimSpeed}, max={chiMaxParticles}, noiseS={chiNoiseStrength}, noiseF={chiNoiseFrequency}, rate={chiRateOverTime}, hue={chiHue}");
    }
}
