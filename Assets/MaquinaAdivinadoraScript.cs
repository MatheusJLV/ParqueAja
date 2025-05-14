
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MaquinaAdivinadoraScript : MonoBehaviour
{
    [SerializeField] private TextMeshPro resultadoTxt;
    [SerializeField] private List<TablasScript> listadoTablas = new List<TablasScript>(6);

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
}
