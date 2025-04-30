using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class GiroscopioScript : MonoBehaviour
{
    public GameObject aroExterno; // Referencia al aro externo
    public GameObject aroInterno; // Referencia al aro interno

    public float velocidadMaxima = 100f; // Velocidad m�xima de rotaci�n
    public float aceleracion = 20f; // Aceleraci�n de la rotaci�n
    private float rotationSpeedExterno = 0f; // Velocidad actual del aroExterno
    private float rotationSpeedInterno = 0f; // Velocidad actual del aroInterno

    // Variables para teletransportaci�n
    public TeleportationAnchor asiento; // TeleportationAnchor para el asiento
    public TeleportationAnchor suelo; // TeleportationAnchor para el suelo

    // Botones para teletransportaci�n
    public Button entradaBtn; // Bot�n para teletransportarse al asiento
    public Button salidaBtn; // Bot�n para teletransportarse al suelo

    // Nuevas variables
    public GameObject asientoGO; // GameObject que ser� el padre del jugador
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

    // M�todo para manejar el evento del bot�n de entrada
    private void OnEntradaButtonClicked()
    {
        if (asiento != null)
        {
            asiento.RequestTeleport();
            Debug.Log("Teletransportado al asiento.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'asiento' no est� asignado.");
        }

        // Establece asientoGO como el padre de jugadorRig
        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRig.transform.SetParent(asientoGO.transform);
            Debug.Log("Jugador ahora es hijo de asientoGO.");
        }
        else
        {
            Debug.LogWarning("asientoGO o jugadorRig no est�n asignados.");
        }
    }

    // M�todo para manejar el evento del bot�n de salida
    private void OnSalidaButtonClicked()
    {
        if (suelo != null)
        {
            suelo.RequestTeleport();
            Debug.Log("Teletransportado al suelo.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'suelo' no est� asignado.");
        }

        // Libera jugadorRig como hijo, coloc�ndolo en la escena principal
        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            Debug.Log("Jugador liberado de cualquier padre.");
        }
        else
        {
            Debug.LogWarning("jugadorRig no est� asignado.");
        }
    }

    void Update()
    {
        // Obt�n los dispositivos de las manos derecha e izquierda
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
            // Acelera en la direcci�n positiva
            rotationSpeedExterno = Mathf.Min(rotationSpeedExterno + aceleracion * Time.deltaTime, velocidadMaxima);
        }
        else if (rightSecondaryPressed)
        {
            // Acelera en la direcci�n negativa
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
            // Acelera en la direcci�n positiva
            rotationSpeedInterno = Mathf.Min(rotationSpeedInterno + aceleracion * Time.deltaTime, velocidadMaxima);
        }
        else if (leftSecondaryPressed)
        {
            // Acelera en la direcci�n negativa
            rotationSpeedInterno = Mathf.Max(rotationSpeedInterno - aceleracion * Time.deltaTime, -velocidadMaxima);
        }
        else
        {
            // Reduce la velocidad gradualmente hacia 0
            rotationSpeedInterno = Mathf.MoveTowards(rotationSpeedInterno, 0f, aceleracion * Time.deltaTime);
        }

        // Aplica la rotaci�n a los objetos
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
