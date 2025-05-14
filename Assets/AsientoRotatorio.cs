using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class AsientoRotatorio : MonoBehaviour
{
    public GameObject asientoGO; // Referencia al asiento (este objeto)
    public GameObject jugadorRig; // Referencia al jugador (XR Rig)
    public float velocidadMaxima = 100f; // Velocidad m�xima de rotaci�n
    public float aceleracion = 20f; // Aceleraci�n de la rotaci�n

    public TeleportationAnchor asientoTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP;   // TeleportationAnchor para el suelo

    private float rotationSpeed = 0f; // Velocidad actual de rotaci�n

    // M�todo para ingresar al asiento y teletransportar
    public void Ingresar()
    {
        if (asientoTP != null)
        {
            asientoTP.RequestTeleport();
            Debug.Log("Teletransportado al asiento.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'asientoTP' no est� asignado.");
        }

        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRig.transform.SetParent(asientoGO.transform);
            Debug.Log("Jugador ahora es hijo del asiento.");
        }
        else
        {
            Debug.LogWarning("asientoGO o jugadorRig no est�n asignados.");
        }
    }

    // M�todo para salir del asiento y teletransportar
    public void Salir()
    {
        if (sueloTP != null)
        {
            sueloTP.RequestTeleport();
            Debug.Log("Teletransportado al suelo.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'sueloTP' no est� asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            Debug.Log("Jugador liberado del asiento.");
        }
        else
        {
            Debug.LogWarning("jugadorRig no est� asignado.");
        }
    }

    void Update()
    {
        ControlRotacion();

        // Aplicar la rotaci�n al asiento
        if (rotationSpeed != 0f)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void ControlRotacion()
    {
        // Obtener los dispositivos de la mano derecha
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        bool rightPrimaryPressed = false;
        bool rightSecondaryPressed = false;

        foreach (var device in rightHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondaryPressed);
        }

        if (rightPrimaryPressed)
        {
            // Acelerar en la direcci�n positiva
            rotationSpeed = Mathf.Min(rotationSpeed + aceleracion * Time.deltaTime, velocidadMaxima);
        }
        else if (rightSecondaryPressed)
        {
            // Acelerar en la direcci�n negativa
            rotationSpeed = Mathf.Max(rotationSpeed - aceleracion * Time.deltaTime, -velocidadMaxima);
        }
        else
        {
            // Reducir la velocidad gradualmente hacia 0
            rotationSpeed = Mathf.MoveTowards(rotationSpeed, 0f, aceleracion * Time.deltaTime);
        }
    }
}
