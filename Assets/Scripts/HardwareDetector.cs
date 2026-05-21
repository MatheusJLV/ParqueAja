using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HardwareDetector : MonoBehaviour
{
    [Header("Estado del Sistema")]
    public bool isVRActive = false;

    void Awake()
    {
        // Se ejecuta antes que cualquier otra cosa en el juego
        List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displaySubsystems);

        if (displaySubsystems.Count > 0 && displaySubsystems[0].running)
        {
            isVRActive = true;
            Debug.Log("Hardware: Meta VR Detectado.");
        }
        else
        {
            isVRActive = false;
            Debug.Log("Hardware: Laptop / PC Detectado.");
        }
    }
}