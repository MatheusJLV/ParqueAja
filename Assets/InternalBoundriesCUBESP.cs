using System.Collections.Generic;
using UnityEngine;

public class InternalBoundriesCUBESP : MonoBehaviour
{
    public CuboEspacialEnhanced cuboEspacialEnhanced;

    public bool complete = true;
    public string trackedTag = "CubitosEspaciales";

    public int cantidad = 0;

    private void Awake()
    {
        cantidad = 0; // ensure clean start
    }

    private void Start()
    {
        // Force sync with actual colliders present at scene start
        Collider[] colliders = Physics.OverlapBox(
            transform.position,
            transform.localScale * 0.5f,
            transform.rotation);

        foreach (var col in colliders)
        {
            if (col.CompareTag(trackedTag))
                cantidad++;
        }

        complete = (cantidad == 9);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(trackedTag))
        {
            cantidad++;
            complete = (cantidad == 9);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(trackedTag))
        {
            cantidad--;
            complete = (cantidad == 9);
            cuboEspacialEnhanced.SetRed();            
        }
        
    }
}
