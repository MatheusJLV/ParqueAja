using UnityEngine;
using UnityEngine.VFX;

public class VanDerGrafEnhanced : MonoBehaviour
{
    [Header("VFX References")]
    [SerializeField] private VisualEffect esferaPrt;  // Sphere static effect
    [SerializeField] private VisualEffect baritaPrt;  // Rod/Bar static effect

    static readonly int SpawnRateID = Shader.PropertyToID("Rate");
    static readonly int LifetimeMinID = Shader.PropertyToID("LifetimeMin");
    static readonly int LifetimeMaxID = Shader.PropertyToID("LifetimeMax");
    static readonly int SpeedID = Shader.PropertyToID("Speed");
    static readonly int TurbulenceIntensityID = Shader.PropertyToID("Intensity");
    static readonly int AttractorPosID = Shader.PropertyToID("Attractor1");

    public void SetChargeLevel(float charge)
    {
        // Map charge (0–1) to parameters
        esferaPrt.SetFloat(SpawnRateID, Mathf.Lerp(20, 200, charge));
        esferaPrt.SetFloat(SpeedID, Mathf.Lerp(2, 8, charge));
        esferaPrt.SetFloat(TurbulenceIntensityID, Mathf.Lerp(1, 10, charge));
    }

    private void Start()
    {
        // Optional: Ensure references are assigned
        if (esferaPrt == null || baritaPrt == null)
        {
            Debug.LogWarning("VanDerGrafEnhanced: Missing VisualEffect references!");
        }
    }

    /// <summary>
    /// Example method to trigger an effect on the sphere.
    /// Later we can hook player inputs here.
    /// </summary>
    public void TriggerSphereEffect()
    {
        if (esferaPrt != null)
        {
            esferaPrt.Play();
        }
    }

    /// <summary>
    /// Example method to trigger an effect on the rod/barita.
    /// </summary>
    public void TriggerRodEffect()
    {
        if (baritaPrt != null)
        {
            baritaPrt.Play();
        }
    }

    /// <summary>
    /// Example to stop effects.
    /// </summary>
    public void StopAllEffects()
    {
        if (esferaPrt != null) esferaPrt.Stop();
        if (baritaPrt != null) baritaPrt.Stop();
    }
}

