using UnityEngine;
using System.Collections.Generic;

/*
 Controla el estado visual (tinte) de un cubo espacial.
 Evalúa condiciones internas y externas para mostrar
 un color verde o rojo en un material tipo vidrio.
*/

public class CuboEspacialEnhanced : MonoBehaviour
{
    // Objeto cuyo material cambiará de color
    [Header("Glass tint target")]
    public GameObject targetObject;

    // Material del objeto objetivo
    private Material targetMaterial;

    // Límites externos asociados al cubo
    public List<ExternalBoundriesCUBESP> externalBoundries = new List<ExternalBoundriesCUBESP>();
    
    // Límites internos del cubo
    public InternalBoundriesCUBESP internalBoundries;

    private void Awake()
    {
        // Obtiene el material del objeto objetivo
        if (targetObject != null)
            targetMaterial = targetObject.GetComponent<Renderer>().material;
    }

    // ---- Lógica de cambio de color ----
    public void TryUpdateTint()
    {
        //AJUSTAR ESTE METODO PARA QUE FUNCIONE CON LOS OTROS DOS SCRIPTS
        // Sale si no hay material asignado
        if (targetMaterial == null) return;
        // Verde si se cumple condición interna y no hay objetos externos
        if (internalBoundries.complete && AllExternalBoundriesClear())
            SetGreen();
        else
            SetRed();
    }

    // Aplica color verde translúcido
    private void SetGreen()
    {
        targetMaterial.color = new Color(0f, 1f, 0f, 0.3f); // translucent green
    }

    // Aplica color rojo translúcido
    public void SetRed()
    {
        targetMaterial.color = new Color(1f, 0f, 0f, 0.3f); // translucent red
    }

    // Verifica que no haya objetos dentro de los límites externos
    public bool AllExternalBoundriesClear()
    {
        foreach (var boundry in externalBoundries)
        {
            if (boundry != null && boundry.hayObjetoDentro)
                return false;
        }
        return true;
    }

}
