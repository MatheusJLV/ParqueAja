using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Controla una plataforma giratoria en VR con aceleración suave y control por distancia entre mandos
// Permite tanto control manual como paseos automatizados con teletransporte
public class PlataformaGiratoriaScript : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject plataforma;   // Base que gira
    [SerializeField] private GameObject controlDer;   // Controlador derecho
    [SerializeField] private GameObject controlIzq;   // Controlador izquierdo
    [SerializeField] private GameObject jugadorRig;   // XR Origin / Rig (se parenta a la plataforma)

    [Header("UI")]
    [SerializeField] private Slider velocidadSlider;         // opcional
    [SerializeField] private Slider aceleracionSlider;       // opcional
    [SerializeField] private Slider duracionSlider;          // opcional
    [SerializeField] private Button iniciarPlataformaBtn;    // "Iniciar giros" (todo-en-uno)

    [Header("Teleport Anchors")]
    [SerializeField] private TeleportationAnchor plataformaTP; // punto sobre la plataforma
    [SerializeField] private TeleportationAnchor sueloTP;      // punto fuera de la plataforma

    [Header("Giro")]
    [SerializeField] private float velocidadMaxima = 150f; // grados/seg (valor base)
    [SerializeField] private float aceleracion = 50f;      // grados/seg^2
    [SerializeField] private int duracion = 12;          // segundos (mitad acelera, mitad frena)
    [SerializeField] private bool manualControl = false;  // control con botones del mando

    // Enumeración de ejes de rotación disponibles
    public enum Eje { X, Y, Z }
    [SerializeField] private Eje ejeRotacion = Eje.Z;      // Z por defecto

    [Header("Opciones de entrada/salida")]
    [Tooltip("Tiempo de espera (seg) tras cada teleport para asegurar estabilidad.")]
    [SerializeField] private float pausaTrasTeleport = 0.25f;

    private float rotationSpeed = 0f; // velocidad actual (grados/seg)
    private float influencia = 1f;    // factor por distancia de controles (LIVE)
    private bool canGirar = false;    // indica si el jugador puede activar giros
    private bool rideEnCurso = false; // indica si hay un paseo automático en progreso

    [Header("Ajuste de influencia")]
    [SerializeField] private float influenciaMultiplicador = 2.5f; // Valor mayor para efecto m�s dr�stico


    // Validación de referencias en el Inspector para asegurar configuración correcta
    private void OnValidate()
    {
        if (plataforma == null) Debug.LogWarning($"{name}: 'plataforma' no asignada.");
        if (jugadorRig == null) Debug.LogWarning($"{name}: 'jugadorRig' no asignado.");
    }

    // Inicializa los listeners de la UI para controlar velocidad, aceleración, duración y botón de inicio
    private void Start()
    {
        if (velocidadSlider) velocidadSlider.onValueChanged.AddListener(v => velocidadMaxima = v);
        if (aceleracionSlider) aceleracionSlider.onValueChanged.AddListener(v => aceleracion = v);
        if (duracionSlider) duracionSlider.onValueChanged.AddListener(v => duracion = Mathf.Max(1, Mathf.RoundToInt(v)));

        if (iniciarPlataformaBtn)
            iniciarPlataformaBtn.onClick.AddListener(() => { if (!rideEnCurso) StartCoroutine(AutoRide_ManualLike()); });
    }

    // Actualiza la influencia y aplica control manual si está activado, luego aplica la rotación
    private void Update()
    {
        // Influencia se recalcula siempre (distancia entre controles)
        ActualizarInfluenciaPorDistancia();

        if (manualControl)
            ControlManualLikeStep(); // usa exactamente la misma lógica que el automático usa internamente

        // Aplicar giro según rotationSpeed
        if (plataforma != null && Mathf.Abs(rotationSpeed) > Mathf.Epsilon)
        {
            Vector3 axis = ejeRotacion == Eje.X ? Vector3.right :
                           ejeRotacion == Eje.Y ? Vector3.up : Vector3.forward;
            plataforma.transform.Rotate(axis * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    // Calcula la influencia basada en la distancia entre los controles VR (más cerca = mayor influencia)
    private void ActualizarInfluenciaPorDistancia()
    {
        if (controlDer != null && controlIzq != null)
        {
            float d = Vector3.Distance(controlDer.transform.position, controlIzq.transform.position);
            // cerca => más influencia, amplificada por el multiplicador
            influencia = influenciaMultiplicador * (1f / Mathf.Clamp(d, 0.5f, 2f));
        }
        else
        {
            influencia = influenciaMultiplicador * 1f; // fallback estable, también amplificado
        }
    }


    // Integra la velocidad de rotación hacia un objetivo con aceleración suave
    // Usa la misma fórmula que el modo manual original: aceleración * influencia * dt
    // y se clampa a +-(velocidadMaxima * influencia)
    private void ManualLikeIntegrate(float targetSign)
    {
        float targetSpeed = targetSign * (velocidadMaxima * influencia);
        float step = (aceleracion * influencia) * Time.deltaTime;

        // Si targetSign = 0 - desacelerar hacia 0
        rotationSpeed = Mathf.MoveTowards(rotationSpeed, targetSpeed, step);

        // clamp de seguridad (por si la influencia cambia bruscamente)
        float maxMag = velocidadMaxima * influencia;
        rotationSpeed = Mathf.Clamp(rotationSpeed, -maxMag, maxMag);
    }

    // Usado en Update cuando manualControl = true (simula botones)
    private void ControlManualLikeStep()
    {
        if (!canGirar) return;

        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        bool primary = false, secondary = false;
        foreach (var dev in rightHandDevices)
        {
            dev.TryGetFeatureValue(CommonUsages.primaryButton, out primary);
            dev.TryGetFeatureValue(CommonUsages.secondaryButton, out secondary);
        }

        if (primary) ManualLikeIntegrate(+1f);  // acelerar sentido positivo
        else if (secondary) ManualLikeIntegrate(-1f);  // acelerar sentido negativo
        else ManualLikeIntegrate(0f);   // soltar - frenar a 0
    }

    // Paseo automático que simula el comportamiento manual con temporizador
    // Primera mitad del tiempo: acelera como si mantuvieras PRIMARY
    // Segunda mitad del tiempo: desacelera suavemente a 0 como si soltaras el botón

    // if (manualControl)
    //     ControlManualLikeStep(); // usa exactamente la misma logica que el autom�tico usa internamente

    /*
    // Usado en Update cuando manualControl = true (simula botones)
    private void ControlManualLikeStep()
    {
        if (!canGirar) return;

        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        bool primary = false, secondary = false;
        foreach (var dev in rightHandDevices)
        {
            dev.TryGetFeatureValue(CommonUsages.primaryButton, out primary);
            dev.TryGetFeatureValue(CommonUsages.secondaryButton, out secondary);
        }

        if (primary) ManualLikeIntegrate(+1f);  // acelerar sentido positivo
        else if (secondary) ManualLikeIntegrate(-1f);  // acelerar sentido negativo
        else ManualLikeIntegrate(0f);   // soltar - frenar a 0
    }
    */
    // Corrutina que ejecuta un paseo automático completo: teletransporte a plataforma,
    // aceleración, desaceleración y teletransporte de vuelta
    private IEnumerator AutoRide_ManualLike()
    {
        rideEnCurso = true;
        if (iniciarPlataformaBtn) iniciarPlataformaBtn.interactable = false;

        // 1) Teleport IN + montar (parent)
        if (plataformaTP) plataformaTP.RequestTeleport();
        if (jugadorRig && plataforma) jugadorRig.transform.SetParent(plataforma.transform, true);
        canGirar = true;

        if (pausaTrasTeleport > 0f) yield return new WaitForSeconds(pausaTrasTeleport);

        rotationSpeed = 0f;
        float total = Mathf.Max(1f, duracion);
        float half = total * 0.5f;

        // 2) Fase A: acelerar como manual (PRIMARY held)
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            ManualLikeIntegrate(+1f); // botón "primary" virtual
            ApplyRotationStep();
            yield return null;
        }

        // 3) Fase B: frenar como manual (released)
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            ManualLikeIntegrate(0f); // soltar bot�n - frenar a 0
            ApplyRotationStep();
            yield return null;
        }

        // 4) Fase C: mantener giro suave durante la pausa antes de teleport OUT
        if (pausaTrasTeleport > 0f)
        {
            float tC = 0f;
            while (tC < pausaTrasTeleport)
            {
                tC += Time.deltaTime;
                ManualLikeIntegrate(0f); // mantener frenado suave
                ApplyRotationStep();
                yield return null;
            }
        }

        // 5) Teleport OUT + desmontar
        canGirar = false;
        if (sueloTP) sueloTP.RequestTeleport();
        // Ya se ha hecho la pausa con giro, así que no es necesario repetirla

        if (jugadorRig) jugadorRig.transform.SetParent(null, true);
        rotationSpeed = 0f;

        if (iniciarPlataformaBtn) iniciarPlataformaBtn.interactable = true;
        rideEnCurso = false;
    }

    // Aplica la rotación a la plataforma según el eje seleccionado y la velocidad actual
    private void ApplyRotationStep()
    {
        if (!plataforma) return;
        Vector3 axis = ejeRotacion == Eje.X ? Vector3.right :
                       ejeRotacion == Eje.Y ? Vector3.up : Vector3.forward;
        plataforma.transform.Rotate(axis * rotationSpeed * Time.deltaTime, Space.Self);
    }

    // Triggers opcionales para habilitar/deshabilitar control manual al entrar/salir de zona
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) canGirar = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) { canGirar = false; rotationSpeed = 0f; }
    }

    // Limpia los listeners de la UI al destruir el objeto para evitar referencias huérfanas
    private void OnDestroy()
    {
        if (velocidadSlider) velocidadSlider.onValueChanged.RemoveAllListeners();
        if (aceleracionSlider) aceleracionSlider.onValueChanged.RemoveAllListeners();
        if (duracionSlider) duracionSlider.onValueChanged.RemoveAllListeners();
        if (iniciarPlataformaBtn) iniciarPlataformaBtn.onClick.RemoveAllListeners();
    }
}
