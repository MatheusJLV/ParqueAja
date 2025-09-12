using System.Collections;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public Light directionalLight;
    public float intensityStep = 0.2f;      // Paso de cambio de intensidad
    public float transitionDuration = 1f;   // Duración de la transición en segundos

    private Coroutine intensityCoroutine;

    // Enciende la luz (intensidad máxima)
    public void TurnOn()
    {
        SetIntensity(1f);
    }

    // Apaga la luz (intensidad cero)
    public void TurnOff()
    {
        SetIntensity(0f);
    }

    // Aumenta la intensidad progresivamente
    public void IncreaseIntensity()
    {
        if (directionalLight == null) return;
        float target = Mathf.Clamp(directionalLight.intensity + intensityStep, 0f, 1f);
        SetIntensity(target);
    }

    // Disminuye la intensidad progresivamente
    public void DecreaseIntensity()
    {
        if (directionalLight == null) return;
        float target = Mathf.Clamp(directionalLight.intensity - intensityStep, 0f, 1f);
        SetIntensity(target);
    }

    // Cambia la intensidad de forma progresiva
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
            directionalLight.intensity = Mathf.Lerp(start, target, elapsed / transitionDuration);
            yield return null;
        }
        directionalLight.intensity = target;
    }
}
