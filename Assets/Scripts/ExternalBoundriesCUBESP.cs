using System.Collections.Generic;
using UnityEngine;

public class ExternalBoundriesCUBESP : MonoBehaviour
{
    /*
     Detecta entradas y salidas de objetos con una tag específica,
     actualizando conteo y el estado visual del Cubo Espacial.
    */

    public CuboEspacialEnhanced cuboEspacialEnhanced;

    // Tag de objetos que serán contados dentro del límite externo
    public string trackedTag = "CubitosEspaciales";
    //public bool active = false;   

    // Conteo de objetos dentro del trigger
    public int cantidad = 0;

    // Flag rápido para saber si hay al menos un objeto dentro
    public bool hayObjetoDentro = false;

    private void OnTriggerEnter(Collider other)
    {
        // Incrementa el conteo y marca presencia cuando entra un objeto válido
        if (other.CompareTag(trackedTag))
        {
            cantidad++;
            cuboEspacialEnhanced.SetRed();
            hayObjetoDentro = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Decrementa el conteo; si queda en cero, limpia el flag y actualiza tinte
        if (other.CompareTag(trackedTag))
        {
            cantidad--;
            if (cantidad==0)
            {
                hayObjetoDentro = false;
                cuboEspacialEnhanced.TryUpdateTint();
                
            }
        }
    }
}
