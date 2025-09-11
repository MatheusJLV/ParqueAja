using UnityEngine;
using System.Collections.Generic;

public class CuboEspacialEnhanced : MonoBehaviour
{
    [Header("Glass tint target")]
    public GameObject targetObject;
    private Material targetMaterial;

    public List<ExternalBoundriesCUBESP> externalBoundries = new List<ExternalBoundriesCUBESP>();
    public InternalBoundriesCUBESP internalBoundries;

    private void Awake()
    {
        if (targetObject != null)
            targetMaterial = targetObject.GetComponent<Renderer>().material;
    }

    // ---- Tint Logic ----
    public void TryUpdateTint()
    {
        //AJUSTAR ESTE METODO PARA QUE FUNCIONE CON LOS OTROS DOS SCRIPTS
        if (targetMaterial == null) return;

        if (internalBoundries.complete && AllExternalBoundriesClear())
            SetGreen();
        else
            SetRed();
    }

    private void SetGreen()
    {
        targetMaterial.color = new Color(0f, 1f, 0f, 0.3f); // translucent green
    }

    public void SetRed()
    {
        targetMaterial.color = new Color(1f, 0f, 0f, 0.3f); // translucent red
    }
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
