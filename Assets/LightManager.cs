using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LightManager : MonoBehaviour
{
    [Header("Directional Light")]
    public Light directionalLight;
    public float intensityStep = 0.2f;
    public float transitionDuration = 1f;

    private Coroutine intensityCoroutine;


    [Header("Skybox control")]
    [SerializeField] private Material brightSkybox;     // assign your normal skybox
    [SerializeField] private Material darkSkybox;       // assign a dark/night skybox
    [SerializeField] private bool fadeSkybox = true;    // if false, swap immediately
    [SerializeField] private float skyboxFadeDuration = 0.6f;

    // internal state for fade
    private Material _fadeSkyboxMat;   // optional, only if you use a blend-capable skybox material
    private Coroutine _skyboxCo;
    private Cubemap _brightReflection; // optional cached reflection cubemap
    private Cubemap _darkReflection;   // optional cached reflection cubemap




    // Simple per-light intensity controls (kept)
    public void TurnOn()
    {
        SetIntensity(1f);
    }

    public void TurnOff()
    {
        SetIntensity(0f);
    }

    public void IncreaseIntensity()
    {
        if (directionalLight == null) return;
        float target = Mathf.Clamp(directionalLight.intensity + intensityStep, 0f, 1f);
        SetIntensity(target);
    }

    public void DecreaseIntensity()
    {
        if (directionalLight == null) return;
        float target = Mathf.Clamp(directionalLight.intensity - intensityStep, 0f, 1f);
        SetIntensity(target);
    }

    private void SetIntensity(float targetIntensity)
    {
        if (directionalLight == null) return;
        if (intensityCoroutine != null)
            StopCoroutine(intensityCoroutine);
        intensityCoroutine = StartCoroutine(LerpIntensity(targetIntensity));
    }

    private IEnumerator LerpIntensity(float target)
    {
        float start = directionalLight.intensity;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            directionalLight.intensity = Mathf.Lerp(start, target, elapsed / Mathf.Max(transitionDuration, 0.0001f));
            yield return null;
        }
        directionalLight.intensity = target;
    }

    [Header("Refs")]
    public Volume globalVolume;

    [Header("Mode 1: Targets - Dark")]
    public float sunOff = 0f;
    public float ambientOff = 0f;
    public float reflectionsOff = 0f;
    public float postExposureOff = 0f;

    // Baseline captured at Start and used for SetBright (Mode 1) and Mode 3 bright
    [Header("Baseline capture")]
    public bool captureBaselineOnStart = true;
    private float sunBaseline;
    private float ambientBaseline;
    private float reflectionsBaseline;
    private float exposureBaseline;
    private bool hasExposure;
    private ColorAdjustments exposureOverride;

    private void Awake()
    {
        if (directionalLight != null)
            RenderSettings.sun = directionalLight;

        if (globalVolume != null && globalVolume.profile != null)
        {
            hasExposure = globalVolume.profile.TryGet(out exposureOverride);
        }
        else
        {
            hasExposure = false;
            exposureOverride = null;
        }
    }

    private void Start()
    {
        if (captureBaselineOnStart)
            CaptureBaseline();
    }

    public void CaptureBaseline()
    {
        sunBaseline = directionalLight != null ? directionalLight.intensity : 0f;
        ambientBaseline = RenderSettings.ambientIntensity;
        reflectionsBaseline = RenderSettings.reflectionIntensity;
        exposureBaseline = (hasExposure && exposureOverride != null) ? exposureOverride.postExposure.value : 0f;
    }

    // Mode 1: original style but Bright restores baseline
    public void SetDark()
    {
        ApplyGlobalImmediate(sunOff, ambientOff, reflectionsOff, postExposureOff);
        ApplyDarkSkybox();
    }

    public void SetBright()
    {
        ApplyGlobalImmediate(sunBaseline, ambientBaseline, reflectionsBaseline, exposureBaseline);
        ApplyBrightSkybox();
    }

    // Helper used by all modes to apply global targets
    private void ApplyGlobalImmediate(float sun, float ambient, float reflections, float exposure)
    {
        if (directionalLight) directionalLight.intensity = sun;

        // Ensure ambient mode supports ambientIntensity
        if (RenderSettings.ambientMode != AmbientMode.Skybox && RenderSettings.ambientMode != AmbientMode.Flat)
            RenderSettings.ambientMode = AmbientMode.Skybox;

        RenderSettings.ambientIntensity = ambient;
        RenderSettings.reflectionIntensity = reflections;

        if (hasExposure && exposureOverride != null)
            exposureOverride.postExposure.value = exposure;

        DynamicGI.UpdateEnvironment();
    }

    // Mode 2: toggle a container GO on/off
    [Header("Mode 2: Environment Lights GO")]
    public GameObject environmentLightsGO;

    public void SetBright_Mode2()
    {
        if (environmentLightsGO) environmentLightsGO.SetActive(true);
    }

    public void SetDark_Mode2()
    {
        if (environmentLightsGO) environmentLightsGO.SetActive(false);
    }

    // Mode 3: mixed (toggle GO and partial scene changes)
    [Header("Mode 3: Partial dark targets")]
    public float partialSunDark = 0.1f;
    public float partialAmbientDark = 0.15f;
    public float partialReflectionsDark = 0.15f;
    public float partialExposureDark = 0.0f;

    public void SetDark_Mode3()
    {
        if (environmentLightsGO) environmentLightsGO.SetActive(false);
        ApplyGlobalImmediate(partialSunDark, partialAmbientDark, partialReflectionsDark, partialExposureDark);
    }

    public void SetBright_Mode3()
    {
        if (environmentLightsGO) environmentLightsGO.SetActive(true);
        // Restore to the captured baseline so there is no drift and no unintentional dim
        ApplyGlobalImmediate(sunBaseline, ambientBaseline, reflectionsBaseline, exposureBaseline);
    }

    // Diagnostics and blackout helpers (optional but kept)
    private struct SavedLightState
    {
        public Light light;
        public bool enabled;
        public float intensity;
        public LightShadows shadows;
    }

    private List<SavedLightState> _savedLights;
    private Material _savedSkybox;

    public void ForceBlackoutNow(Volume vol)
    {
        if (_savedLights == null) _savedLights = new List<SavedLightState>();
        _savedLights.Clear();
        foreach (var lt in FindObjectsOfType<Light>(true))
        {
            _savedLights.Add(new SavedLightState
            {
                light = lt,
                enabled = lt.enabled,
                intensity = lt.intensity,
                shadows = lt.shadows
            });
            lt.enabled = false;
            lt.intensity = 0f;
            lt.shadows = LightShadows.None;
        }

        if (RenderSettings.ambientMode != AmbientMode.Skybox && RenderSettings.ambientMode != AmbientMode.Flat)
            RenderSettings.ambientMode = AmbientMode.Skybox;

        RenderSettings.ambientIntensity = 0f;
        RenderSettings.reflectionIntensity = 0f;

        if (vol != null && vol.profile != null &&
            vol.profile.TryGet<ColorAdjustments>(out var adj))
        {
            adj.postExposure.value = 0f;
        }

        _savedSkybox = RenderSettings.skybox;
        RenderSettings.skybox = null;

        LightmapSettings.lightmaps = new LightmapData[0];
        DynamicGI.UpdateEnvironment();
    }

    public void RestoreFromBlackout(Volume vol)
    {
        if (_savedLights != null)
        {
            foreach (var s in _savedLights)
            {
                if (s.light == null) continue;
                s.light.enabled = s.enabled;
                s.light.intensity = s.intensity;
                s.light.shadows = s.shadows;
            }
            _savedLights.Clear();
        }

        if (_savedSkybox != null)
        {
            RenderSettings.skybox = _savedSkybox;
            _savedSkybox = null;
        }

        DynamicGI.UpdateEnvironment();
    }

    private void ApplyBrightSkybox()
    {
        if (!fadeSkybox)
        {
            if (brightSkybox != null) RenderSettings.skybox = brightSkybox;
            DynamicGI.UpdateEnvironment();
            return;
        }

        // If your skybox material supports a _Blend float, we can fade it.
        if (_skyboxCo != null) StopCoroutine(_skyboxCo);
        _skyboxCo = StartCoroutine(FadeSkybox(bright: true));
    }

    // Call when making scene dark
    private void ApplyDarkSkybox()
    {
        if (!fadeSkybox)
        {
            if (darkSkybox != null) RenderSettings.skybox = darkSkybox;
            DynamicGI.UpdateEnvironment();
            return;
        }

        if (_skyboxCo != null) StopCoroutine(_skyboxCo);
        _skyboxCo = StartCoroutine(FadeSkybox(bright: false));
    }

    // Fades between skyboxes by dimming reflections to 0, swapping, then restoring.
    // If your skybox shader supports a _Blend float, uncomment the blend lines and
    // assign a material instance that lerps between two textures.
    private IEnumerator FadeSkybox(bool bright)
    {
        // Step 1: fade reflections down to 0 so specular does not pop
        float startRefl = RenderSettings.reflectionIntensity;
        float t = 0f;
        while (t < skyboxFadeDuration * 0.5f)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.0001f, skyboxFadeDuration * 0.5f));
            RenderSettings.reflectionIntensity = Mathf.Lerp(startRefl, 0f, a);
            yield return null;
        }
        RenderSettings.reflectionIntensity = 0f;

        // Step 2: swap skybox
        if (bright && brightSkybox != null) RenderSettings.skybox = brightSkybox;
        if (!bright && darkSkybox != null) RenderSettings.skybox = darkSkybox;
        DynamicGI.UpdateEnvironment();

        // Optional blend if your skybox supports _Blend
        // Example assumes _Blend 0 = bright, 1 = dark in the same material instance.
        // float blendStart = bright ? 1f : 0f;
        // float blendEnd   = bright ? 0f : 1f;
        // t = 0f;
        // while (t < skyboxFadeDuration)
        // {
        //     t += Time.deltaTime;
        //     float a = Mathf.Clamp01(t / Mathf.Max(0.0001f, skyboxFadeDuration));
        //     if (RenderSettings.skybox.HasProperty("_Blend"))
        //         RenderSettings.skybox.SetFloat("_Blend", Mathf.Lerp(blendStart, blendEnd, a));
        //     yield return null;
        // }

        // Step 3: restore reflections smoothly to match your current global target
        // We do not know which mode called us, so we restore toward whatever is set
        // in RenderSettings.reflectionIntensity by your ApplyGlobalImmediate calls.
        // To coordinate, call ApplyDark/ApplyBright first, then call ApplyDarkSkybox/ApplyBrightSkybox.
        float targetRefl = RenderSettings.reflectionIntensity; // after your global call
        float restoreStart = 0f;
        t = 0f;
        while (t < skyboxFadeDuration * 0.5f)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.0001f, skyboxFadeDuration * 0.5f));
            RenderSettings.reflectionIntensity = Mathf.Lerp(restoreStart, targetRefl, a);
            yield return null;
        }
        RenderSettings.reflectionIntensity = targetRefl;
    }

}