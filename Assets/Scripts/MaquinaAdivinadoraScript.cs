using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Máquina adivinadora: suma valores de tablas seleccionadas y reproduce sonidos de selección/acción.
public class MaquinaAdivinadoraScript : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshPro resultadoTxt; // Texto donde se muestra el número adivinado

    [Header("Tablas")]
    [SerializeField] private List<TablasScript> listadoTablas = new List<TablasScript>(6); // Colección de tablas a evaluar

    [Header("Audio Sources")]
    [SerializeField] private AudioSource selectAS;  // Sonido de selección
    [SerializeField] private AudioSource actionAS;  // Sonido de acción

    // Recorre las tablas marcadas, suma sus valores y actualiza el texto de resultado.
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
            // Muestra el total acumulado como cadena
            resultadoTxt.text = resultado.ToString();
        }
    }

    // Reproduce el audio de "Select" si está disponible.
    public void PlaySelectSound()
    {
        if (selectAS != null)
        {
            selectAS.Play();
        }
    }

    // Reproduce el audio de "Action" si está disponible.
    public void PlayActionSound()
    {
        if (actionAS != null)
        {
            actionAS.Play();
        }
    }
}
