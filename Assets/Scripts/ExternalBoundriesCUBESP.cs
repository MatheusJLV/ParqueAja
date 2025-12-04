using System.Collections.Generic;
using UnityEngine;

public class ExternalBoundriesCUBESP : MonoBehaviour
{
    public CuboEspacialEnhanced cuboEspacialEnhanced;

    public string trackedTag = "CubitosEspaciales";
    //public bool active = false;   

    public int cantidad = 0;

    public bool hayObjetoDentro = false;

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag(trackedTag))
        {
            cantidad++;
            cuboEspacialEnhanced.SetRed();
            hayObjetoDentro = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
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
