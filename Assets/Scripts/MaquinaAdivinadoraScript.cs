using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MaquinaAdivinadoraScript : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshPro resultadoTxt;

    [Header("Tablas")]
    [SerializeField] private List<TablasScript> listadoTablas = new List<TablasScript>(6);

    [Header("Audio Sources")]
    [SerializeField] private AudioSource selectAS;
    [SerializeField] private AudioSource actionAS;

    public void RevisarTablas()
    {
        int resultado = 0;
        foreach (var tabla in listadoTablas)
        {
            if (tabla != null && tabla.presionado)
            {
                resultado += tabla.valor;
            }
        }

        if (resultadoTxt != null)
        {
            resultadoTxt.text = resultado.ToString();
        }
    }

    /// <summary>
    /// Plays the "Select" audio source, if available.
    /// </summary>
    public void PlaySelectSound()
    {
        if (selectAS != null)
        {
            selectAS.Play();
        }
    }

    /// <summary>
    /// Plays the "Action" audio source, if available.
    /// </summary>
    public void PlayActionSound()
    {
        if (actionAS != null)
        {
            actionAS.Play();
        }
    }
}
