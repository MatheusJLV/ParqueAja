/*using System.Collections;
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

    // Sliders para controlar velocidad y aceleraci�n
    public Slider velocidadSlider; // Slider para controlar la velocidad m�xima
    public Slider aceleracionSlider; // Slider para controlar la aceleraci�n

    public int duracion = 10; // Duraci�n predeterminada de la animaci�n
    public Button iniciarGiroscopioBtn; // Bot�n para iniciar el giroscopio

    private bool canGirar = false;

    public bool manualContoller = false; // Variable para controlar el uso manual del controlador

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Aseg�rate de usar el tag correcto
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

        // Suscribirse al evento onClick del bot�n iniciarGiroscopioBtn
        if (iniciarGiroscopioBtn != null)
        {
            iniciarGiroscopioBtn.onClick.AddListener(OnIniciarGiroscopioButtonClicked);
        }
    }

    // M�todo para manejar el cambio de valor del slider de velocidad
    private void OnVelocidadSliderChanged(float value)
    {
        velocidadMaxima = value;
    }

    // M�todo para manejar el cambio de valor del slider de aceleraci�n
    private void OnAceleracionSliderChanged(float value)
    {
        aceleracion = value;
    }

    private void OnIniciarGiroscopioButtonClicked()
    {
        iniciarGiroscopio(duracion);
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

    // M�todo para manejar el evento del bot�n de entrada
    private void OnEntradaButtonClicked()
    {
        if (asiento != null)
        {
            asiento.RequestTeleport();
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'asiento' no est� asignado.");
        }

        // Establece asientoGO como el padre de jugadorRig
        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRig.transform.SetParent(asientoGO.transform);
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
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'suelo' no est� asignado.");
        }

        // Libera jugadorRig como hijo, coloc�ndolo en la escena principal
        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
        }
        else
        {
            Debug.LogWarning("jugadorRig no est� asignado.");
        }
    }
    private void controlacionManual()
    {
        if (!canGirar) return;

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

    }
    
    private IEnumerator Fase1(int duracion)
    {
        float tiempoTranscurrido = 0f;
        // Fase 1: Aceleraci�n hasta la velocidad m�xima
        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;

            // Incrementa la velocidad de los aros hasta la velocidad m�xima
            rotationSpeedExterno = Mathf.Min(rotationSpeedExterno +  2*aceleracion * Time.deltaTime, velocidadMaxima);
            rotationSpeedInterno = Mathf.Min(rotationSpeedInterno +  2*aceleracion * Time.deltaTime, velocidadMaxima);

            // Aplica la rotaci�n a los aros
            aroExterno.transform.Rotate(Vector3.right * rotationSpeedExterno * Time.deltaTime, Space.Self);
            aroInterno.transform.Rotate(Vector3.forward * rotationSpeedInterno * Time.deltaTime, Space.Self);

            yield return null; // Espera al siguiente frame
        }
    }

    private IEnumerator Fase2()
    {
        // Fase 2: Mantener la velocidad m�xima durante un tiempo
        float tiempoMantener = 2f; // Tiempo en segundos para mantener la velocidad m�xima
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < tiempoMantener)
        {
            tiempoTranscurrido += Time.deltaTime;

            // Aplica la rotaci�n a los aros
            aroExterno.transform.Rotate(Vector3.right * rotationSpeedExterno * Time.deltaTime, Space.Self);
            aroInterno.transform.Rotate(Vector3.forward * rotationSpeedInterno * Time.deltaTime, Space.Self);

            yield return null; // Espera al siguiente frame
        }
    }

    private IEnumerator Fase3()
    {
        // Fase 3: Ajuste final de la rotaci�n
        while (!EsRotacionCero(aroExterno.transform.localEulerAngles) || !EsRotacionCero(aroInterno.transform.localEulerAngles))
        {
            // Ajusta la rotaci�n lentamente hacia (0, 0, 0)
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
*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Giroscopio: Presiona Iniciar  teleporta al asiento  gira por "duracion"  desacelera libera al jugador.
/// - Mantiene solo lo esencial. Sin validaciones complejas.
/// - "velocidadMaxima" y "aceleracion" controlan la rampa.
/// - "duracion" es el tiempo de crucero (sin contar rampas).
/// - (Opcional) Slider para ajustar velocidad antes/durante.
/// </summary>
public class GiroscopioScript : MonoBehaviour
{
    [Header("Aros")]
    public GameObject aroExterno;      // rota en X local
    public GameObject aroInterno;      // rota en Z local

    [Header("Teletransporte")]
    public TeleportationAnchor asiento; // adentro del giroscopio
    public TeleportationAnchor suelo;   // afuera para liberar
    public GameObject asientoGO;        // padre al que se "sujeta" el rig mientras gira
    public GameObject jugadorRig;       // XR Origin / XR Rig

    [Header("UI (opcional)")]
    public Button iniciarBtn;
    public Slider velocidadSlider;      // si est� asignado, controla velocidadMaxima

    [Header("Par�metros")]
    public float velocidadMaxima = 120f;   // grados/seg a crucero
    public float aceleracion = 180f;       // grados/seg2 (rampa)
    public float duracion = 10f;           // tiempo en segundos a velocidad de crucero
    public float esperaEntrada = 0.35f;    // para dejar que el teletransporte se asiente
    public float esperaSalida = 0.35f;     // (opcional) pausa antes de teletransportar afuera

    private float _speedExterno, _speedInterno;
    private Coroutine _runCo;

    void Start()
    {
        if (iniciarBtn) iniciarBtn.onClick.AddListener(Iniciar);
        if (velocidadSlider)
        {
            velocidadMaxima = velocidadSlider.value;
            velocidadSlider.onValueChanged.AddListener(v => velocidadMaxima = v);
        }
    }

    public void Iniciar()
    {
        if (_runCo != null) StopCoroutine(_runCo);
        _runCo = StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        // 1) Entrar: teleporta al asiento y hace hijo del asiento
        if (asiento) asiento.RequestTeleport();
        if (jugadorRig && asientoGO) jugadorRig.transform.SetParent(asientoGO.transform, true);
        yield return new WaitForSeconds(esperaEntrada);

        // 2) Acelerar hasta velocidadMaxima
        while ((_speedExterno < velocidadMaxima - 0.01f) || (_speedInterno < velocidadMaxima - 0.01f))
        {
            float delta = aceleracion * Time.deltaTime;
            _speedExterno = Mathf.Min(_speedExterno + delta, velocidadMaxima);
            _speedInterno = Mathf.Min(_speedInterno + delta, velocidadMaxima);
            RotarPaso();
            yield return null;
        }

        // 3) Mantener por 'duracion'
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            RotarPaso();
            yield return null;
        }

        // 4) Desacelerar a 0
        while (_speedExterno > 0.01f || _speedInterno > 0.01f)
        {
            float delta = aceleracion * Time.deltaTime;
            _speedExterno = Mathf.Max(_speedExterno - delta, 0f);
            _speedInterno = Mathf.Max(_speedInterno - delta, 0f);
            RotarPaso();
            yield return null;
        }

        // 5) Ajuste suave de rotaci�n a identidad (liberar lerps)
        yield return StartCoroutine(ResetSuaveAIdentidad());

        // 6) Liberar: quitar parent y teletransportar a suelo (si existe)
        if (jugadorRig) jugadorRig.transform.SetParent(null, true);
        yield return new WaitForSeconds(esperaSalida);
        if (suelo) suelo.RequestTeleport();

        _runCo = null;
    }

    private void RotarPaso()
    {
        if (aroExterno)
            aroExterno.transform.Rotate(Vector3.right * _speedExterno * Time.deltaTime, Space.Self);
        if (aroInterno)
            aroInterno.transform.Rotate(Vector3.forward * _speedInterno * Time.deltaTime, Space.Self);
    }

    private IEnumerator ResetSuaveAIdentidad()
    {
        // M�x 2 s para "asentar" a rotaci�n cero
        float timeout = 2f;
        float t = 0f;
        var target = Quaternion.identity;

        while (t < timeout &&
               ((aroExterno && Quaternion.Angle(aroExterno.transform.localRotation, target) > 0.5f) ||
                (aroInterno && Quaternion.Angle(aroInterno.transform.localRotation, target) > 0.5f)))
        {
            t += Time.deltaTime;
            float step = aceleracion * Time.deltaTime; // usa "aceleracion" como ritmo de correcci�n

            if (aroExterno)
                aroExterno.transform.localRotation = Quaternion.RotateTowards(aroExterno.transform.localRotation, target, step);
            if (aroInterno)
                aroInterno.transform.localRotation = Quaternion.RotateTowards(aroInterno.transform.localRotation, target, step);

            yield return null;
        }
    }

    void OnDestroy()
    {
        if (iniciarBtn) iniciarBtn.onClick.RemoveListener(Iniciar);
        if (velocidadSlider) velocidadSlider.onValueChanged.RemoveAllListeners();
    }
}
