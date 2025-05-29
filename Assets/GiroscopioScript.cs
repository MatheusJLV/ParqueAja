using System.Collections;
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

    // Sliders para controlar velocidad y aceleración
    public Slider velocidadSlider; // Slider para controlar la velocidad máxima
    public Slider aceleracionSlider; // Slider para controlar la aceleración

    public int duracion = 10; // Duración predeterminada de la animación
    public Button iniciarGiroscopioBtn; // Botón para iniciar el giroscopio

    private bool canGirar = false;

    public bool manualContoller = false; // Variable para controlar el uso manual del controlador

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

        // Suscribirse a los eventos de cambio de valor de los sliders
        if (velocidadSlider != null)
        {
            velocidadSlider.onValueChanged.AddListener(OnVelocidadSliderChanged);
        }

        if (aceleracionSlider != null)
        {
            aceleracionSlider.onValueChanged.AddListener(OnAceleracionSliderChanged);
        }

        // Suscribirse al evento onClick del botón iniciarGiroscopioBtn
        if (iniciarGiroscopioBtn != null)
        {
            iniciarGiroscopioBtn.onClick.AddListener(OnIniciarGiroscopioButtonClicked);
        }
    }

    // Método para manejar el cambio de valor del slider de velocidad
    private void OnVelocidadSliderChanged(float value)
    {
        velocidadMaxima = value;
        Debug.Log($"Velocidad máxima actualizada a: {velocidadMaxima}");
    }

    // Método para manejar el cambio de valor del slider de aceleración
    private void OnAceleracionSliderChanged(float value)
    {
        aceleracion = value;
        Debug.Log($"Aceleración actualizada a: {aceleracion}");
    }

    private void OnIniciarGiroscopioButtonClicked()
    {
        iniciarGiroscopio(duracion);
        Debug.Log($"Giroscopio iniciado con duración: {duracion} segundos.");
    }

    void OnDestroy()
    {
        if (iniciarGiroscopioBtn != null)
        {
            iniciarGiroscopioBtn.onClick.RemoveListener(OnIniciarGiroscopioButtonClicked);
        }

        if (velocidadSlider != null)
        {
            velocidadSlider.onValueChanged.RemoveListener(OnVelocidadSliderChanged);
        }

        if (aceleracionSlider != null)
        {
            aceleracionSlider.onValueChanged.RemoveListener(OnAceleracionSliderChanged);
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
    private void controlacionManual()
    {
        if (!canGirar) return;

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

    void Update()
    {
        if (manualContoller)
        {
            controlacionManual();
        }
        
    }
    public void iniciarGiroscopio(int duracion)
    {
        StartCoroutine(AnimarGiroscopio(duracion));
    }

    private IEnumerator AnimarGiroscopio(int duracion)
    {
        yield return StartCoroutine(Fase1(duracion));
        yield return StartCoroutine(Fase2());
        yield return StartCoroutine(Fase3());

        Debug.Log("Animación del giroscopio completada.");
    }
    
    private IEnumerator Fase1(int duracion)
    {
        float tiempoTranscurrido = 0f;
        // Fase 1: Aceleración hasta la velocidad máxima
        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;

            // Incrementa la velocidad de los aros hasta la velocidad máxima
            rotationSpeedExterno = Mathf.Min(rotationSpeedExterno +  2*aceleracion * Time.deltaTime, velocidadMaxima);
            rotationSpeedInterno = Mathf.Min(rotationSpeedInterno +  2*aceleracion * Time.deltaTime, velocidadMaxima);

            // Aplica la rotación a los aros
            aroExterno.transform.Rotate(Vector3.right * rotationSpeedExterno * Time.deltaTime, Space.Self);
            aroInterno.transform.Rotate(Vector3.forward * rotationSpeedInterno * Time.deltaTime, Space.Self);

            yield return null; // Espera al siguiente frame
        }
    }

    private IEnumerator Fase2()
    {
        // Fase 2: Mantener la velocidad máxima durante un tiempo
        float tiempoMantener = 2f; // Tiempo en segundos para mantener la velocidad máxima
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < tiempoMantener)
        {
            tiempoTranscurrido += Time.deltaTime;

            // Aplica la rotación a los aros
            aroExterno.transform.Rotate(Vector3.right * rotationSpeedExterno * Time.deltaTime, Space.Self);
            aroInterno.transform.Rotate(Vector3.forward * rotationSpeedInterno * Time.deltaTime, Space.Self);

            yield return null; // Espera al siguiente frame
        }
    }

    private IEnumerator Fase3()
    {
        // Fase 3: Ajuste final de la rotación
        while (!EsRotacionCero(aroExterno.transform.localEulerAngles) || !EsRotacionCero(aroInterno.transform.localEulerAngles))
        {
            // Ajusta la rotación lentamente hacia (0, 0, 0)
            if (!EsRotacionCero(aroExterno.transform.localEulerAngles))
            {
                aroExterno.transform.localEulerAngles = Vector3.MoveTowards(
                    aroExterno.transform.localEulerAngles,
                    Vector3.zero,
                    aceleracion * Time.deltaTime
                );
            }

            if (!EsRotacionCero(aroInterno.transform.localEulerAngles))
            {
                aroInterno.transform.localEulerAngles = Vector3.MoveTowards(
                    aroInterno.transform.localEulerAngles,
                    Vector3.zero,
                    aceleracion * Time.deltaTime
                );
            }

            yield return null; // Espera al siguiente frame
        }
    }


    private bool EsRotacionCero(Vector3 rotacion)
    {
        // Consider the rotation as zero if each component is within a small threshold
        return Mathf.Abs(rotacion.x) < 0.01f &&
               Mathf.Abs(rotacion.y) < 0.01f &&
               Mathf.Abs(rotacion.z) < 0.01f;
    }
}
