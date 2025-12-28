using System.Collections.Generic;
using UnityEngine;

/*
 * ExternalBoundriesCUBESP:
 * Gestiona límites externos detectando entrada/salida de objetos con un tag específico,
 * actualizando un contador y cambiando el estado de un CuboEspacialEnhanced.
 */

public class ExternalBoundriesCUBESP : MonoBehaviour
{
    public CuboEspacialEnhanced cuboEspacialEnhanced; // Referencia al cubo espacial a controlar

    public string trackedTag = "CubitosEspaciales"; // Tag de los objetos a rastrear
    //public bool active = false;   

    public int cantidad = 0; // Contador de objetos dentro

    public bool hayObjetoDentro = false; // Indica si hay objetos dentro

    private void OnTriggerEnter(Collider other)
    {
        // Detectar cuando un objeto entra en el trigger
        if (other.CompareTag(trackedTag)) // Si tiene el tag rastreado
        {
            cantidad++; // Incrementar contador
            cuboEspacialEnhanced.SetRed(); // Cambiar a rojo
            hayObjetoDentro = true; // Marcar que hay objetos dentro
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Detectar cuando un objeto sale del trigger
        if (other.CompareTag(trackedTag)) // Si tiene el tag rastreado
        {
            cantidad--; // Decrementar contador
            if (cantidad==0) // Si no quedan objetos
            {
                hayObjetoDentro = false; // Marcar que no hay objetos
                cuboEspacialEnhanced.TryUpdateTint(); // Intentar actualizar tinte
                
            }
        }
    }
}
