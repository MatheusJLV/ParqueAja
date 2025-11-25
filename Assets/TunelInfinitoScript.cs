using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/*
TunelInfinitoScript: controla un sistema de túnel infinito con reflejo/refracción
Gestiona un pool de instancias que se posicionan y aplican efectos visuales falloff
basados en sliders de UI (reflexión, refracción y color).

Funcionalidades:
- Pool dinámico de GameObjects con control de cantidad activa
- Aplicar falloff de intensidad, metallic, smoothness y emisión por instancia
- Control de color por HSV a través de slider
- Posicionamiento automático a lo largo de un eje (X, Y, Z)
*/
public class TunelInfinitoScript : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider reflexionSD;     // Drives instance count (0..1)
    public Slider RefraccionSD;    // Reserved for later (refraction strength)
    public Slider ColorSD;         // Hue [0..1]

    [Header("Scene References")]
    public GameObject Lights;      // Parent
    public GameObject Prefab;      // To pool/instantiate

    public enum Axis { X, Y, Z }

    [Header("Placement")]
    public Axis placeAlong = Axis.Y;   // Eje a lo largo del cual se posicionan las instancias
    public float offset = 0.1f;   // Espaciado entre instancias

    [Header("Count Mapping (Reflection - Instances)")]
    [Min(1)] public int minCantidad = 4;  // Mínima cantidad de instancias
    [Min(1)] public int maxCantidad = 64;  // Máxima cantidad de instancias


    [Header("Per-Iteration Falloff")]
    public string baseColorProp = "_BaseColor";    // Nombre de propiedad del color base
    public string metallicProp = "_Metallic";    // Nombre de propiedad metallic
    public string smoothnessProp = "_Smoothness";   // Nombre de propiedad smoothness
    public string emissionColorProp = "_EmissionColor";   // Nombre de propiedad emisión
    public string instanceIntensityProp = "_InstanceIntensity";  // Intensidad por instancia
    //Parámetros de falloff de intensidad base
    [Range(0, 2)] public float baseIntensityNear = 1f;
    [Range(0, 2)] public float baseIntensityFar = 0f;
    //Parámetros de falloff de metallic
    [Range(0, 1)] public float metallicNear = 1f;
    [Range(0, 1)] public float metallicFar = 0f;
    //Parámetros de falloff de smoothness
    [Range(0, 1)] public float smoothnessNear = 0.5f;
    [Range(0, 1)] public float smoothnessFar = 0f;
    //Parámetros de falloff de emisión
    [Range(0, 5)] public float emissionIntensityNear = 1f;
    [Range(0, 5)] public float emissionIntensityFar = 0f;

    // runtime
    readonly List<GameObject> _pool = new();   // Pool de instancias reutilizables
    Color _currentHueColor = Color.white;   // Color actual basado en HSV
    int _activeCount = 0;   // Cantidad de instancias activas

    //Start: inicializar pool, listeners de sliders y estado inicial
    //Valida referencias, prepara el pool y conecta los sliders a sus handlers    
    void Start()
    {
        if (!Lights || !Prefab)
        {
            Debug.LogWarning("[TunelInfinito] Assign 'Lights' and 'Prefab'.");
            return;
        }

        // Prepare pool up to maxCantidad
        EnsurePoolSize(maxCantidad);

        // Wire sliders
        if (ColorSD) { ColorSD.onValueChanged.AddListener(OnColorSliderChanged); OnColorSliderChanged(ColorSD.value); }
        if (reflexionSD)
        {
            reflexionSD.onValueChanged.AddListener(OnReflectionChanged);
            OnReflectionChanged(reflexionSD.value); // initialize active count/layout
        }
        else
        {
            // Fallback: start with minCantidad if no slider connected
            SetActiveCount(minCantidad);
        }
    }

    //OnDestroy: limpiar listeners de sliders
    //Desconectar eventos para evitar memory leaks
    void OnDestroy()
    {
        if (ColorSD) ColorSD.onValueChanged.RemoveListener(OnColorSliderChanged);
        if (reflexionSD) reflexionSD.onValueChanged.RemoveListener(OnReflectionChanged);
    }

    /*
    OnReflectionChanged: handler del slider de reflexión
    Mapea valor [0..1] a cantidad de instancias [minCantidad..maxCantidad]
    */
    void OnReflectionChanged(float v01)
    {
        int target = Mathf.RoundToInt(Mathf.Lerp(minCantidad, maxCantidad, Mathf.Clamp01(v01)));
        SetActiveCount(target);
        RebuildLayoutAndAppearance();
    }

    //OnColorSliderChanged: handler del slider de color
    //Convierte valor Hue [0..1] a color RGB y reaplica falloff a todas las instancias
    void OnColorSliderChanged(float h)
    {
        _currentHueColor = Color.HSVToRGB(h, 1f, 1f);
        // FIX A: recompute full falloff per instance on color change
        for (int i = 0; i < _activeCount; i++)
        {
            ApplyPerInstanceFalloff(_pool[i], i, _activeCount);
        }
    }


    //EnsurePoolSize: crear instancias en el pool hasta alcanzar targetSize
    //Cada instancia se nombra, posiciona y desactiva inicialmente
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
    //SetActiveCount: activar/desactivar instancias según cantidad deseada
    //Habilita las primeras 'count' instancias y desactiva el resto
    void SetActiveCount(int count)
    {
        count = Mathf.Clamp(count, 0, _pool.Count);
        // enable needed
        for (int i = 0; i < count; i++)
            if (!_pool[i].activeSelf) _pool[i].SetActive(true);
        // disable the rest
        for (int i = count; i < _pool.Count; i++)
            if (_pool[i].activeSelf) _pool[i].SetActive(false);

        _activeCount = count;
    }

    //RebuildLayoutAndAppearance: posicionar instancias activas y aplicar falloff
    //Distribuye instancias a lo largo del eje seleccionado con espaciado uniforme
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

    // --- Appearance helpers ---
    /*
    ApplyPerInstanceFalloff: aplicar propiedades de material con interpolación por índice
    Interpola intensidad, metallic, smoothness y emisión de cercano a lejano
    Utiliza MaterialPropertyBlock para evitar instancias de material
    */
    void ApplyPerInstanceFalloff(GameObject instance, int index, int total)
    {
        float t01 = (total <= 1) ? 0f : (float)index / (total - 1);
        //Interpolar parámetros desde cercano (t=0) a lejano (t=1)
        float baseI = Mathf.Lerp(baseIntensityNear, baseIntensityFar, t01);
        float metal = Mathf.Lerp(metallicNear, metallicFar, t01);
        float smooth = Mathf.Lerp(smoothnessNear, smoothnessFar, t01);
        float emisI = Mathf.Lerp(emissionIntensityNear, emissionIntensityFar, t01);
        
        //Calcular colores base y emisión con intensidad
        Color baseCol = _currentHueColor * baseI; baseCol.a = 1f;
        Color emisCol = _currentHueColor * emisI; emisCol.a = 1f;

        //Aplicar propiedades a todos los renderers de la instancia
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

    //ReapplyColorKeepingFalloff: actualizar color manteniendo valores de falloff existentes
    //Reaplica el color HSV actual preservando la intensidad por instancia
    void ReapplyColorKeepingFalloff(Renderer r)
    {
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);

        float baseI = mpb.GetFloat(instanceIntensityProp);
        if (baseI <= 0f) baseI = 1f; // default if not set

        Color baseCol = _currentHueColor * baseI; baseCol.a = 1f;

        if (HasProp(r, baseColorProp)) mpb.SetColor(baseColorProp, baseCol);
        if (HasProp(r, emissionColorProp)) mpb.SetColor(emissionColorProp, baseCol); // same intensity by default

        r.SetPropertyBlock(mpb);
    }

    // --- utils ---
    //HasProp: validar si un renderer tiene una propiedad en sus materiales
    //Verifica el primer material compartido para la existencia de la propiedad
    bool HasProp(Renderer r, string prop)
    {
        if (string.IsNullOrEmpty(prop)) return false;
        var mats = r.sharedMaterials;
        if (mats == null || mats.Length == 0) return false;
        var m = mats[0];
        return m && m.HasProperty(prop);
    }
}