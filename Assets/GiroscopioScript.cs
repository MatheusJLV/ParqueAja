using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class GiroscopioScript : MonoBehaviour
{
    public GameObject aroExterno; // Referencia al aro externo
    public GameObject aroInterno; // Referencia al aro interno

    public float velocidadMaxima = 100f; // Velocidad máxima de rotación
    public float aceleracion = 20f; // Aceleración de la rotación
    private float rotationSpeedExterno = 0f; // Velocidad actual del aroExterno
    private float rotationSpeedInterno = 0f; // Velocidad actual del aroInterno

    // Variables para teletransportación
    public TeleportationAnchor asiento; // TeleportationAnchor para el asiento
    public TeleportationAnchor suelo; // TeleportationAnchor para el suelo

    // Botones para teletransportación
    public Button entradaBtn; // Botón para teletransportarse al asiento
    public Button salidaBtn; // Botón para teletransportarse al suelo

    // Nuevas variables
    public GameObject asientoGO; // GameObject que será el padre del jugador
    public GameObject jugadorRig; // GameObject que representa al jugador

    void Start()
    {
        // Suscribirse a los eventos onClick de los botones
        if (entradaBtn != null)
        {
            entradaBtn.onClick.AddListener(OnEntradaButtonClicked);
        }

        if (salidaBtn != null)
        {
            salidaBtn.onClick.AddListener(OnSalidaButtonClicked);
        }
    }

    // Método para manejar el evento del botón de entrada
    private void OnEntradaButtonClicked()
    {
        if (asiento != null)
        {
            asiento.RequestTeleport();
            Debug.Log("Teletransportado al asiento.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'asiento' no está asignado.");
        }

        // Establece asientoGO como el padre de jugadorRig
        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRig.transform.SetParent(asientoGO.transform);
            Debug.Log("Jugador ahora es hijo de asientoGO.");
        }
        else
        {
            Debug.LogWarning("asientoGO o jugadorRig no están asignados.");
        }
    }

    // Método para manejar el evento del botón de salida
    private void OnSalidaButtonClicked()
    {
        if (suelo != null)
        {
            suelo.RequestTeleport();
            Debug.Log("Teletransportado al suelo.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'suelo' no está asignado.");
        }

        // Libera jugadorRig como hijo, colocándolo en la escena principal
        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            Debug.Log("Jugador liberado de cualquier padre.");
        }
        else
        {
            Debug.LogWarning("jugadorRig no está asignado.");
        }
    }

    void Update()
    {
        // Obtén los dispositivos de las manos derecha e izquierda
        var rightHandDevices = new List<InputDevice>();
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);

        // Variables para verificar los estados de los botones
        bool rightPrimaryPressed = false;
        bool rightSecondaryPressed = false;
        bool leftPrimaryPressed = false;
        bool leftSecondaryPressed = false;

        // Verifica los botones del controlador derecho
        foreach (var device in rightHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondaryPressed);
        }

        // Verifica los botones del controlador izquierdo
        foreach (var device in leftHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out leftPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out leftSecondaryPressed);
        }

        // Control del aroExterno (mano derecha)
        if (rightPrimaryPressed)
        {
            // Acelera en la dirección positiva
            rotationSpeedExterno = Mathf.Min(rotationSpeedExterno + aceleracion * Time.deltaTime, velocidadMaxima);
        }
        else if (rightSecondaryPressed)
        {
            // Acelera en la dirección negativa
            rotationSpeedExterno = Mathf.Max(rotationSpeedExterno - aceleracion * Time.deltaTime, -velocidadMaxima);
        }
        else
        {
            // Reduce la velocidad gradualmente hacia 0
            rotationSpeedExterno = Mathf.MoveTowards(rotationSpeedExterno, 0f, aceleracion * Time.deltaTime);
        }

        // Control del aroInterno (mano izquierda)
        if (leftPrimaryPressed)
        {
            // Acelera en la dirección positiva
            rotationSpeedInterno = Mathf.Min(rotationSpeedInterno + aceleracion * Time.deltaTime, velocidadMaxima);
        }
        else if (leftSecondaryPressed)
        {
            // Acelera en la dirección negativa
            rotationSpeedInterno = Mathf.Max(rotationSpeedInterno - aceleracion * Time.deltaTime, -velocidadMaxima);
        }
        else
        {
            // Reduce la velocidad gradualmente hacia 0
            rotationSpeedInterno = Mathf.MoveTowards(rotationSpeedInterno, 0f, aceleracion * Time.deltaTime);
        }

        // Aplica la rotación a los objetos
        if (rotationSpeedExterno != 0f)
        {
            aroExterno.transform.Rotate(Vector3.right * rotationSpeedExterno * Time.deltaTime, Space.Self);
        }

        if (rotationSpeedInterno != 0f)
        {
            aroInterno.transform.Rotate(Vector3.forward * rotationSpeedInterno * Time.deltaTime, Space.Self);
        }
    }
}
