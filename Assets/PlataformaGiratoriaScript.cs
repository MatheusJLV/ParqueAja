using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

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
    [SerializeField] private float velocidadMaxima = 100f; // grados/seg (valor base)
    [SerializeField] private float aceleracion = 20f;      // grados/seg^2
    [SerializeField] private int duracion = 5;           // segundos (para Acelerar-Mantener-Frenar)
    [SerializeField] private bool manualControl = false;  // si quieres además controlar con botones del mando

    public enum Eje { X, Y, Z }
    [SerializeField] private Eje ejeRotacion = Eje.Z;      // << Z por defecto

    [Header("Opciones de entrada/salida")]
    [Tooltip("Tiempo de espera (seg) tras cada teleport para asegurar estabilidad.")]
    [SerializeField] private float pausaTrasTeleport = 0.25f;

    private float rotationSpeed = 0f; // velocidad actual (grados/seg)
    private float influencia = 1f;    // factor por distancia de controles (se recalcula)
    private bool canGirar = false;
    private bool rideEnCurso = false;

    // ---------------- Inspector sanity ----------------
    private void OnValidate()
    {
        if (plataforma == null) Debug.LogWarning($"{name}: 'plataforma' no asignada.");
        if (jugadorRig == null) Debug.LogWarning($"{name}: 'jugadorRig' no asignado.");
    }

    private void Start()
    {
        if (velocidadSlider) velocidadSlider.onValueChanged.AddListener(v => velocidadMaxima = v);
        if (aceleracionSlider) aceleracionSlider.onValueChanged.AddListener(v => aceleracion = v);
        if (duracionSlider) duracionSlider.onValueChanged.AddListener(v => duracion = Mathf.Max(1, Mathf.RoundToInt(v)));

        if (iniciarPlataformaBtn)
            iniciarPlataformaBtn.onClick.AddListener(() => { if (!rideEnCurso) StartCoroutine(AutoRide()); });
    }

    private void Update()
    {
        // Mantén influencia actualizada para control manual / preview;
        // la rutina automática TOMARÁ UNA FOTO de esta influencia al iniciar para mantenerla estable.
        ActualizarInfluenciaPorDistancia();

        if (manualControl)
            ControlManual();

        if (plataforma != null && Mathf.Abs(rotationSpeed) > Mathf.Epsilon)
        {
            Vector3 axis = ejeRotacion == Eje.X ? Vector3.right :
                           ejeRotacion == Eje.Y ? Vector3.up : Vector3.forward;
            plataforma.transform.Rotate(axis * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    // ---------------- Núcleo ----------------
    private void ActualizarInfluenciaPorDistancia()
    {
        if (controlDer != null && controlIzq != null)
        {
            float d = Vector3.Distance(controlDer.transform.position, controlIzq.transform.position);
            influencia = 1f / Mathf.Clamp(d, 0.5f, 2f); // cerca => más influencia
        }
        else
        {
            influencia = 1f; // fallback estable
        }
    }

    private void ControlManual()
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

        if (primary)
        {
            rotationSpeed = Mathf.Min(rotationSpeed + (aceleracion * influencia) * Time.deltaTime,
                                      velocidadMaxima * influencia);
        }
        else if (secondary)
        {
            rotationSpeed = Mathf.Max(rotationSpeed - (aceleracion * influencia) * Time.deltaTime,
                                      -velocidadMaxima * influencia);
        }
        else
        {
            rotationSpeed = Mathf.MoveTowards(rotationSpeed, 0f, (aceleracion * influencia) * Time.deltaTime);
        }
    }

    // ---------------- Paseo automático todo-en-uno ----------------
    private IEnumerator AutoRide()
    {
        rideEnCurso = true;
        if (iniciarPlataformaBtn) iniciarPlataformaBtn.interactable = false;

        // 1) Teleport IN + montar (parent)
        if (plataformaTP) plataformaTP.RequestTeleport();
        if (jugadorRig && plataforma) jugadorRig.transform.SetParent(plataforma.transform, true);
        canGirar = true;

        // Espera un instante a que el teleport se estabilice
        if (pausaTrasTeleport > 0f) yield return new WaitForSeconds(pausaTrasTeleport);

        // 2) Tomar foto de parámetros actuales
        float influSnapshot = influencia;                                       // influencia congelada para este paseo
        float vmaxSnapshot = velocidadMaxima * Mathf.Max(0.1f, influSnapshot); // velocidad objetivo del paseo
        float accelSnapshot = Mathf.Max(0.01f, aceleracion);                    // seguridad
        int durSnapshot = Mathf.Max(1, duracion);                           // seguridad
        rotationSpeed = 0f;

        // 3) Acelerar -> Mantener -> Frenar (usa la duración configurada)
        float fase = durSnapshot / 3f;

        // Acelerar
        float t = 0f;
        while (t < fase)
        {
            t += Time.deltaTime;
            rotationSpeed = Mathf.Min(rotationSpeed + accelSnapshot * Time.deltaTime, vmaxSnapshot);
            AplicarPaso();
            yield return null;
        }

        // Mantener
        t = 0f;
        while (t < fase)
        {
            t += Time.deltaTime;
            rotationSpeed = Mathf.MoveTowards(rotationSpeed, vmaxSnapshot, accelSnapshot * 0.5f * Time.deltaTime);
            AplicarPaso();
            yield return null;
        }

        // Frenar
        while (rotationSpeed > 0f)
        {
            rotationSpeed = Mathf.MoveTowards(rotationSpeed, 0f, accelSnapshot * Time.deltaTime);
            AplicarPaso();
            yield return null;
        }

        // 4) Teleport OUT + desmontar (unparent) y limpiar
        canGirar = false;
        if (sueloTP) sueloTP.RequestTeleport();
        if (pausaTrasTeleport > 0f) yield return new WaitForSeconds(pausaTrasTeleport);

        if (jugadorRig) jugadorRig.transform.SetParent(null, true);
        rotationSpeed = 0f;

        // Fin
        if (iniciarPlataformaBtn) iniciarPlataformaBtn.interactable = true;
        rideEnCurso = false;
    }

    private void AplicarPaso()
    {
        if (!plataforma) return;
        Vector3 axis = ejeRotacion == Eje.X ? Vector3.right :
                       ejeRotacion == Eje.Y ? Vector3.up : Vector3.forward;
        plataforma.transform.Rotate(axis * rotationSpeed * Time.deltaTime, Space.Self);
    }

    private void OnDestroy()
    {
        if (velocidadSlider) velocidadSlider.onValueChanged.RemoveAllListeners();
        if (aceleracionSlider) aceleracionSlider.onValueChanged.RemoveAllListeners();
        if (duracionSlider) duracionSlider.onValueChanged.RemoveAllListeners();
        if (iniciarPlataformaBtn) iniciarPlataformaBtn.onClick.RemoveAllListeners();
    }
}
