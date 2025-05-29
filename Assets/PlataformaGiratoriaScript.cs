using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class PlataformaGiratoriaScript : MonoBehaviour
{
    public GameObject plataforma; // Referencia a la plataforma

    public float velocidadMaxima = 100f; // Velocidad máxima de rotación
    public float aceleracion = 20f; // Aceleración de la rotación
    private float rotationSpeed = 0f; // Velocidad actual de la plataforma

    public float influencia = 1f; // Factor de influencia basado en la distancia entre los controladores

    public GameObject controlDer; // Controlador derecho (asignado en el editor)
    public GameObject controlIzq; // Controlador izquierdo (asignado en el editor)

    // Sliders para controlar velocidad y aceleración
    public Slider velocidadSlider; // Slider para controlar la velocidad máxima
    public Slider aceleracionSlider; // Slider para controlar la aceleración

    public int duracion = 5; // Duración predeterminada de la animación
    public Button iniciarPlataformaBtn; // Botón para iniciar la animación automática

    public bool manualControl = false; // Control manual activado/desactivado
    public GameObject jugadorRig; // GameObject que representa al jugador

    // Botones para teletransportación
    public Button entradaBtn; // Botón para teletransportarse a la plataforma
    public Button salidaBtn; // Botón para teletransportarse al suelo

    // Variables para teletransportación
    public TeleportationAnchor plataformaTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP; // TeleportationAnchor para el suelo

    private bool canGirar = false;
    void Start()
    {
        // Suscribirse a los eventos de los sliders
        if (velocidadSlider != null)
        {
            velocidadSlider.onValueChanged.AddListener(OnVelocidadSliderChanged);
        }

        if (aceleracionSlider != null)
        {
            aceleracionSlider.onValueChanged.AddListener(OnAceleracionSliderChanged);
        }

        // Suscribirse al evento del botón para iniciar la animación automática
        if (iniciarPlataformaBtn != null)
        {
            iniciarPlataformaBtn.onClick.AddListener(OnIniciarPlataformaButtonClicked);
        }

        // Suscribirse a los eventos de los botones de teletransportación
        if (entradaBtn != null)
        {
            entradaBtn.onClick.AddListener(OnEntradaButtonClicked);
        }

        if (salidaBtn != null)
        {
            salidaBtn.onClick.AddListener(OnSalidaButtonClicked);
        }
    }

    void Update()
    {
        // Actualizar la influencia basada en la distancia entre los controladores
        UpdateInfluencia();

        if (manualControl)
        {
            ControlManual();
        }

        // Aplicar la rotación a la plataforma
        if (rotationSpeed != 0f)
        {
            plataforma.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void UpdateInfluencia()
    {
        if (controlDer != null && controlIzq != null)
        {
            // Calcular la distancia entre los controladores
            float distance = Vector3.Distance(controlDer.transform.position, controlIzq.transform.position);

            // Actualizar la influencia (puedes ajustar la fórmula según sea necesario)
            influencia = 1f / Mathf.Clamp(distance, 0.5f, 2f); // Limitar la influencia entre 0.5 y 2
        }
        else
        {
            Debug.LogWarning("Los controladores no están asignados. Asignarlos en el editor.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Asegúrate de usar el tag correcto
            canGirar = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            canGirar = false;
    }

    private void ControlManual()
    {

        if (!canGirar) return;
        // Obtener los dispositivos de la mano derecha
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        // Variables para verificar los estados de los botones
        bool rightPrimaryPressed = false;
        bool rightSecondaryPressed = false;

        // Verificar los botones del controlador derecho
        foreach (var device in rightHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondaryPressed);
        }

        // Control de la rotación de la plataforma
        if (rightPrimaryPressed)
        {
            // Acelerar en la dirección positiva
            rotationSpeed = Mathf.Min(rotationSpeed + (aceleracion * influencia) * Time.deltaTime, velocidadMaxima * influencia);
        }
        else if (rightSecondaryPressed)
        {
            // Acelerar en la dirección negativa
            rotationSpeed = Mathf.Max(rotationSpeed - (aceleracion * influencia) * Time.deltaTime, -velocidadMaxima * influencia);
        }
        else
        {
            // Reducir la velocidad gradualmente hacia 0
            rotationSpeed = Mathf.MoveTowards(rotationSpeed, 0f, (aceleracion * influencia) * Time.deltaTime);
        }
    }

    private void OnVelocidadSliderChanged(float value)
    {
        velocidadMaxima = value;
        Debug.Log($"Velocidad máxima actualizada a: {velocidadMaxima}");
    }

    private void OnAceleracionSliderChanged(float value)
    {
        aceleracion = value;
        Debug.Log($"Aceleración actualizada a: {aceleracion}");
    }

    private void OnIniciarPlataformaButtonClicked()
    {
        StartCoroutine(AnimarPlataforma());
        Debug.Log($"Animación automática de la plataforma iniciada con duración: {duracion} segundos.");
    }

    private IEnumerator AnimarPlataforma()
    {
        // Fase 1: Aceleración
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracion / 3f)
        {
            tiempoTranscurrido += Time.deltaTime;
            rotationSpeed = Mathf.Min(rotationSpeed + (aceleracion * influencia) * Time.deltaTime, velocidadMaxima * influencia);
            plataforma.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }

        // Fase 2: Mantener velocidad máxima
        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracion / 3f)
        {
            tiempoTranscurrido += Time.deltaTime;
            plataforma.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }

        // Fase 3: Desaceleración
        while (rotationSpeed > 0f)
        {
            rotationSpeed = Mathf.MoveTowards(rotationSpeed, 0f, (aceleracion * influencia) * Time.deltaTime);
            plataforma.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }

        Debug.Log("Animación automática de la plataforma completada.");
    }
    private void OnEntradaButtonClicked()
    {
        if (plataformaTP != null)
        {
            plataformaTP.RequestTeleport();
            Debug.Log("Jugador teletransportado a la plataforma.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'plataformaTP' no está asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(plataforma.transform);
            Debug.Log("Jugador ahora es hijo de la plataforma.");
        }
        else
        {
            Debug.LogWarning("El jugadorRig no está asignado.");
        }
    }

    private void OnSalidaButtonClicked()
    {
        if (sueloTP != null)
        {
            sueloTP.RequestTeleport();
            Debug.Log("Jugador teletransportado al suelo.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'sueloTP' no está asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            Debug.Log("Jugador liberado de la plataforma.");
        }
        else
        {
            Debug.LogWarning("El jugadorRig no está asignado.");
        }
    }

    void OnDestroy()
    {
        // Desuscribirse de los eventos
        if (velocidadSlider != null)
        {
            velocidadSlider.onValueChanged.RemoveListener(OnVelocidadSliderChanged);
        }

        if (aceleracionSlider != null)
        {
            aceleracionSlider.onValueChanged.RemoveListener(OnAceleracionSliderChanged);
        }

        if (iniciarPlataformaBtn != null)
        {
            iniciarPlataformaBtn.onClick.RemoveListener(OnIniciarPlataformaButtonClicked);
        }

        if (entradaBtn != null)
        {
            entradaBtn.onClick.RemoveListener(OnEntradaButtonClicked);
        }

        if (salidaBtn != null)
        {
            salidaBtn.onClick.RemoveListener(OnSalidaButtonClicked);
        }
    }
}
