using UnityEngine;

public class FiltroAudio : MonoBehaviour
{
    public AudioLowPassFilter lowPass;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ActivarFiltroMuffled()
    {
        if (lowPass != null)
            lowPass.cutoffFrequency = 500f; // Lower = more muffled
    }

    public void DesactivarFiltroMuffled()
    {
        if (lowPass != null)
            lowPass.cutoffFrequency = 22000f; // Normal human hearing range
    }
}
