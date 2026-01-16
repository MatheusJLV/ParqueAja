using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Sistema de túnel infinito que genera y posiciona dinámicamente instancias de objetos con efectos de atenuación.
// Controla el número de instancias, su color (tono HSV), y propiedades de material (metallic, smoothness, emisión)
// basadas en sliders de UI, aplicando una atenuación gradual desde la instancia más cercana hasta la más lejana.
public class TunelInfinitoScript : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider reflexionSD;     // Controla el número de instancias (0..1 mapeado a minCantidad..maxCantidad)
    public Slider RefraccionSD;    // Reservado para uso futuro (intensidad de refracción)
    public Slider ColorSD;         // Controla el tono (Hue) del color [0..1]

    [Header("Scene References")]
    public GameObject Lights;      // GameObject padre que contendrá todas las instancias
    public GameObject Prefab;      // Prefab a instanciar y agregar al pool

    public enum Axis { X, Y, Z }   // Eje a lo largo del cual se posicionan las instancias

    [Header("Placement")]
    public Axis placeAlong = Axis.Y;  // Eje seleccionado para la colocación de instancias
    public float offset = 0.1f;       // Distancia entre cada instancia consecutiva

    [Header("Count Mapping (Reflection - Instances)")]
    [Min(1)] public int minCantidad = 4;   // Número mínimo de instancias (cuando slider en 0)
    [Min(1)] public int maxCantidad = 64;  // Número máximo de instancias (cuando slider en 1)  // Número máximo de instancias (cuando slider en 1)

    [Header("Per-Iteration Falloff")]
    public string baseColorProp = "_BaseColor";               // Nombre de la propiedad del color base en el shader
    public string metallicProp = "_Metallic";                 // Nombre de la propiedad metálica en el shader
    public string smoothnessProp = "_Smoothness";             // Nombre de la propiedad de suavidad en el shader
    public string emissionColorProp = "_EmissionColor";       // Nombre de la propiedad del color de emisión en el shader
    public string instanceIntensityProp = "_InstanceIntensity"; // Nombre de la propiedad de intensidad por instancia // Nombre de la propiedad de intensidad por instancia

    [Range(0, 2)] public float baseIntensityNear = 1f;     // Intensidad del color base en la instancia más cercana
    [Range(0, 2)] public float baseIntensityFar = 0f;      // Intensidad del color base en la instancia más lejana
    [Range(0, 1)] public float metallicNear = 1f;          // Valor metálico en la instancia más cercana
    [Range(0, 1)] public float metallicFar = 0f;           // Valor metálico en la instancia más lejana
    [Range(0, 1)] public float smoothnessNear = 0.5f;      // Suavidad en la instancia más cercana
    [Range(0, 1)] public float smoothnessFar = 0f;         // Suavidad en la instancia más lejana
    [Range(0, 5)] public float emissionIntensityNear = 1f; // Intensidad de emisión en la instancia más cercana
    [Range(0, 5)] public float emissionIntensityFar = 0f;  // Intensidad de emisión en la instancia más lejana

    readonly List<GameObject> _pool = new();  // Pool de instancias del prefab
    Color _currentHueColor = Color.white;     // Color actual basado en el tono del slider
    int _activeCount = 0;                     // Número actual de instancias activas                     // Número actual de instancias activas

    // Inicializa el sistema preparando el pool de instancias y conectando los listeners de los sliders
    void Start()
    {
        if (!Lights || !Prefab)
        {
            Debug.LogWarning("[TunelInfinito] Assign 'Lights' and 'Prefab'.");
            return;
        }

        // Preparar el pool hasta maxCantidad
        EnsurePoolSize(maxCantidad);

        // Conectar sliders
        if (ColorSD) { ColorSD.onValueChanged.AddListener(OnColorSliderChanged); OnColorSliderChanged(ColorSD.value); }
        if (reflexionSD)
        {
            reflexionSD.onValueChanged.AddListener(OnReflectionChanged);
            OnReflectionChanged(reflexionSD.value); // inicializar cantidad activa y disposición
        }
        else
        {
            // Fallback: comenzar con minCantidad si no hay slider conectado
            SetActiveCount(minCantidad);
        }
    }

    // Desconecta los listeners de los sliders cuando el objeto se destruye
    void OnDestroy()
    {
        if (ColorSD) ColorSD.onValueChanged.RemoveListener(OnColorSliderChanged);
        if (reflexionSD) reflexionSD.onValueChanged.RemoveListener(OnReflectionChanged);
    }

    // --- Manejadores de sliders ---
    
    // Maneja cambios en el slider de reflexión, ajustando el número de instancias activas y reconstruyendo el layout
    void OnReflectionChanged(float v01)
    {
        int target = Mathf.RoundToInt(Mathf.Lerp(minCantidad, maxCantidad, Mathf.Clamp01(v01)));
        SetActiveCount(target);
        RebuildLayoutAndAppearance();
    }

    // Maneja cambios en el slider de color, actualizando el tono y reaplicando la atenuación a todas las instancias activas
    void OnColorSliderChanged(float h)
    {
        _currentHueColor = Color.HSVToRGB(h, 1f, 1f);
        // CORRECCIÓN A: recalcular la atenuación completa por instancia al cambiar el color
        for (int i = 0; i < _activeCount; i++)
        {
            ApplyPerInstanceFalloff(_pool[i], i, _activeCount);
        }
    }


    // --- Operaciones principales ---
    
    // Asegura que el pool tenga al menos el tamaño objetivo, instanciando nuevos objetos si es necesario
    void EnsurePoolSize(int targetSize)
    {
        var parent = Lights.transform;
        while (_pool.Count < targetSize)
        {
            var inst = Instantiate(Prefab, parent);
            inst.name = $"{Prefab.name}_{_pool.Count:D2}";
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            inst.SetActive(false);
            _pool.Add(inst);
        }
    }

    // Establece el número de instancias activas, activando o desactivando GameObjects según sea necesario
    void SetActiveCount(int count)
    {
        count = Mathf.Clamp(count, 0, _pool.Count);
        // activar las necesarias
        for (int i = 0; i < count; i++)
            if (!_pool[i].activeSelf) _pool[i].SetActive(true);
        // desactivar el resto
        for (int i = count; i < _pool.Count; i++)
            if (_pool[i].activeSelf) _pool[i].SetActive(false);

        _activeCount = count;
    }

    // Reconstruye el layout posicionando las instancias a lo largo del eje seleccionado y aplicando la atenuación de apariencia
    void RebuildLayoutAndAppearance()
    {
        Vector3 dir = placeAlong == Axis.X ? Vector3.right : (placeAlong == Axis.Y ? Vector3.up : Vector3.forward);

        for (int i = 0; i < _activeCount; i++)
        {
            var inst = _pool[i];
            var t = inst.transform;
            t.localPosition = dir * (i * offset);

            ApplyPerInstanceFalloff(inst, i, _activeCount);
        }
    }

    // --- Ayudantes de apariencia ---
    
    // Aplica la atenuación gradual de propiedades de material (color, metallic, smoothness, emisión) a una instancia específica
    void ApplyPerInstanceFalloff(GameObject instance, int index, int total)
    {
        float t01 = (total <= 1) ? 0f : (float)index / (total - 1);

        float baseI = Mathf.Lerp(baseIntensityNear, baseIntensityFar, t01);
        float metal = Mathf.Lerp(metallicNear, metallicFar, t01);
        float smooth = Mathf.Lerp(smoothnessNear, smoothnessFar, t01);
        float emisI = Mathf.Lerp(emissionIntensityNear, emissionIntensityFar, t01);

        Color baseCol = _currentHueColor * baseI; baseCol.a = 1f;
        Color emisCol = _currentHueColor * emisI; emisCol.a = 1f;

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);

            mpb.SetFloat(instanceIntensityProp, baseI);

            if (HasProp(r, baseColorProp)) mpb.SetColor(baseColorProp, baseCol);
            if (HasProp(r, emissionColorProp)) mpb.SetColor(emissionColorProp, emisCol);
            if (HasProp(r, metallicProp)) mpb.SetFloat(metallicProp, metal);
            if (HasProp(r, smoothnessProp)) mpb.SetFloat(smoothnessProp, smooth);

            r.SetPropertyBlock(mpb);
        }
    }

    // Reaaplica el color manteniendo la intensidad de atenuación previamente calculada para un Renderer específico
    void ReapplyColorKeepingFalloff(Renderer r)
    {
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);

        float baseI = mpb.GetFloat(instanceIntensityProp);
        if (baseI <= 0f) baseI = 1f; // por defecto si no está establecido

        Color baseCol = _currentHueColor * baseI; baseCol.a = 1f;

        if (HasProp(r, baseColorProp)) mpb.SetColor(baseColorProp, baseCol);
        if (HasProp(r, emissionColorProp)) mpb.SetColor(emissionColorProp, baseCol); // misma intensidad por defecto

        r.SetPropertyBlock(mpb);
    }

    // --- Utilidades ---
    
    // Verifica si un Renderer tiene una propiedad específica en su primer material compartido
    bool HasProp(Renderer r, string prop)
    {
        if (string.IsNullOrEmpty(prop)) return false;
        var mats = r.sharedMaterials;
        if (mats == null || mats.Length == 0) return false;
        var m = mats[0];
        return m && m.HasProperty(prop);
    }
}

