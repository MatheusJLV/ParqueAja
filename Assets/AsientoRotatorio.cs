using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class AsientoRotatorio : MonoBehaviour
{
    public GameObject asientoGO; // Referencia al asiento (este objeto)
    public GameObject jugadorRig; // Referencia al jugador (XR Rig)
    public float velocidadMaxima = 100f; // Velocidad máxima de rotación
    public float aceleracion = 20f; // Aceleración de la rotación

    public TeleportationAnchor asientoTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP;   // TeleportationAnchor para el suelo

    private float rotationSpeed = 0f; // Velocidad actual de rotación

    // Método para ingresar al asiento y teletransportar
    public void Ingresar()
    {
        if (asientoTP != null)
        {
            asientoTP.RequestTeleport();
            Debug.Log("Teletransportado al asiento.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'asientoTP' no está asignado.");
        }

        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRig.transform.SetParent(asientoGO.transform);
            Debug.Log("Jugador ahora es hijo del asiento.");
        }
        else
        {
            Debug.LogWarning("asientoGO o jugadorRig no están asignados.");
        }
    }

    // Método para salir del asiento y teletransportar
    public void Salir()
    {
        if (sueloTP != null)
        {
            sueloTP.RequestTeleport();
            Debug.Log("Teletransportado al suelo.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'sueloTP' no está asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            Debug.Log("Jugador liberado del asiento.");
        }
        else
        {
            Debug.LogWarning("jugadorRig no está asignado.");
        }
    }

    void Update()
    {
        ControlRotacion();

        // Aplicar la rotación al asiento
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
            // Acelerar en la dirección positiva
            rotationSpeed = Mathf.Min(rotationSpeed + aceleracion * Time.deltaTime, velocidadMaxima);
        }
        else if (rightSecondaryPressed)
        {
            // Acelerar en la dirección negativa
            rotationSpeed = Mathf.Max(rotationSpeed - aceleracion * Time.deltaTime, -velocidadMaxima);
        }
        else
        {
            // Reducir la velocidad gradualmente hacia 0
            rotationSpeed = Mathf.MoveTowards(rotationSpeed, 0f, aceleracion * Time.deltaTime);
        }
    }
}
