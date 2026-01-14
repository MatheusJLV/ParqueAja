using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Gestor de iluminación: orquesta luz direccional, ambiente, reflejos, post-exposure y skybox.
public class LightManager : MonoBehaviour
{
    [Header("Directional Light")]
    public Light directionalLight;
    public float intensityStep = 0.2f;
    public float transitionDuration = 1f;

    private Coroutine intensityCoroutine;


    [Header("Skybox control")]
    [SerializeField] private Material brightSkybox;     // asigna tu skybox normal
    [SerializeField] private Material darkSkybox;       // asigna un skybox oscuro/nocturno
    [SerializeField] private bool fadeSkybox = true;    // si es false, cambia de inmediato
    [SerializeField] private float skyboxFadeDuration = 0.6f;

    // Estado interno para los fundidos
    private Material _fadeSkyboxMat;   // opcional, solo si usas un skybox con soporte de blend
    private Coroutine _skyboxCo;
    private Cubemap _brightReflection; // cubemap de reflexión cacheado (opcional)
    private Cubemap _darkReflection;   // cubemap de reflexión cacheado (opcional)




    // Controles simples de intensidad por luz direccional
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

    // Baseline capturado en Start y usado para SetBright (Modo 1) y modo 3 brillante
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

    // Modo 1: estilo original; Bright restaura el baseline
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

    // Ayudante usado por todos los modos para aplicar objetivos globales
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

    // Modo 2: alterna un GameObject contenedor de luces
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

    // Modo 3: mixto (toggle GO y cambios parciales de escena)
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

    // Diagnósticos y utilidades de blackout (opcional, se mantiene)
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

    // Llamar al oscurecer la escena
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

    // Hace fade entre skyboxes bajando reflejos a 0, cambiando y luego restaurando.
    // Si tu shader de skybox soporta _Blend, descomenta las líneas de mezcla y
    // usa un material que interpole entre dos texturas.
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