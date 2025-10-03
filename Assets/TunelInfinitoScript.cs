using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public Axis placeAlong = Axis.Y;
    public float offset = 0.1f;

    [Header("Count Mapping (Reflection - Instances)")]
    [Min(1)] public int minCantidad = 4;
    [Min(1)] public int maxCantidad = 64;

    [Header("Per-Iteration Falloff")]
    public string baseColorProp = "_BaseColor";
    public string metallicProp = "_Metallic";
    public string smoothnessProp = "_Smoothness";
    public string emissionColorProp = "_EmissionColor";
    public string instanceIntensityProp = "_InstanceIntensity";

    [Range(0, 2)] public float baseIntensityNear = 1f;
    [Range(0, 2)] public float baseIntensityFar = 0f;
    [Range(0, 1)] public float metallicNear = 1f;
    [Range(0, 1)] public float metallicFar = 0f;
    [Range(0, 1)] public float smoothnessNear = 0.5f;
    [Range(0, 1)] public float smoothnessFar = 0f;
    [Range(0, 5)] public float emissionIntensityNear = 1f;
    [Range(0, 5)] public float emissionIntensityFar = 0f;

    // runtime
    readonly List<GameObject> _pool = new();
    Color _currentHueColor = Color.white;
    int _activeCount = 0;

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

    void OnDestroy()
    {
        if (ColorSD) ColorSD.onValueChanged.RemoveListener(OnColorSliderChanged);
        if (reflexionSD) reflexionSD.onValueChanged.RemoveListener(OnReflectionChanged);
    }

    // --- Slider handlers ---
    void OnReflectionChanged(float v01)
    {
        int target = Mathf.RoundToInt(Mathf.Lerp(minCantidad, maxCantidad, Mathf.Clamp01(v01)));
        SetActiveCount(target);
        RebuildLayoutAndAppearance();
    }

    void OnColorSliderChanged(float h)
    {
        _currentHueColor = Color.HSVToRGB(h, 1f, 1f);
        // FIX A: recompute full falloff per instance on color change
        for (int i = 0; i < _activeCount; i++)
        {
            ApplyPerInstanceFalloff(_pool[i], i, _activeCount);
        }
    }


    // --- Core ops ---
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
    bool HasProp(Renderer r, string prop)
    {
        if (string.IsNullOrEmpty(prop)) return false;
        var mats = r.sharedMaterials;
        if (mats == null || mats.Length == 0) return false;
        var m = mats[0];
        return m && m.HasProperty(prop);
    }
}

