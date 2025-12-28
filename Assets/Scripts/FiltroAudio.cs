using UnityEngine;

/*
 * FiltroAudio:
 * Controla un filtro de paso bajo para audio, permitiendo activar/desactivar
 * un efecto "muffled" cambiando la frecuencia de corte.
 */

public class FiltroAudio : MonoBehaviour
{
    public AudioLowPassFilter lowPass; // Filtro de paso bajo para controlar el audio

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ActivarFiltroMuffled()
    {
        // Activar efecto muffled bajando la frecuencia de corte
        if (lowPass != null)
            lowPass.cutoffFrequency = 500f; // Lower = more muffled - Más bajo = más muffled
    }

    public void DesactivarFiltroMuffled()
    {
        // Desactivar efecto muffled restaurando la frecuencia normal
        if (lowPass != null)
            lowPass.cutoffFrequency = 22000f; // Normal human hearing range - Rango normal de audición humana
    }
}
