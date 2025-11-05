using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AsientoRotatorio : MonoBehaviour
{
    [Header("Seat & Player")]
    public GameObject asientoGO;         // Parent/mount for player
    public GameObject jugadorRig;        // XR Origin / Rig

    [Header("Rotating Mechanism")]
    public Transform mecanismo;          // What actually rotates
    public Vector3 rotateLocalAxis = Vector3.forward; // Local axis (Z by default)
    public float speedDegPerSec = 120f;  // Constant speed during auto run

    [Header("Boarding")]
    public TeleportationAnchor asientoTP;
    public TeleportationAnchor sueloTP;
    public float delayAfterTeleportIn = 0.35f;
    public float delayBeforeTeleportOut = 0.35f;

    [Header("Automation UI (like Giroscopio)")]
    public int defaultRunTimeSeconds = 8;
    //public Button activateButton;     // Starts full sequence using defaultRunTimeSeconds
    public Slider timerSlider;        // Controls current run duration (seconds)

    [Tooltip("If assigned, this button also starts the sequence.")]
    public Button iniciarBtn;

    [Header("Speed (NEW)")]
    [Tooltip("Slider that sets speedDegPerSec directly (degrees/second).")]
    public Slider velocidadSlider;

    [Header("Pelotas (kept from your original)")]
    public Transform posicionInstancia;
    public GameObject pelotaPrefab;
    public GameObject pelotas;        // container
    public GameObject pelotaActual;
    public bool pelotaNeeded = false;
    public bool pelotaWanted = false;

    [Header("Misc UI (optional from old script)")]
    public Button ingresarBtn;
    public Button salirBtn;

    // State
    public float tiempo = 8f;         // Current run time (seconds)
    private Coroutine runCo;
    private bool autoRunning = false;

    // cached listeners
    private UnityEngine.Events.UnityAction _activateCB;
    private UnityEngine.Events.UnityAction _iniciarCB;
    private UnityEngine.Events.UnityAction _ingresarCB;
    private UnityEngine.Events.UnityAction _salirCB;
    private UnityEngine.Events.UnityAction<float> _timerCB;
    private UnityEngine.Events.UnityAction<float> _velCB;   // NEW

    void Start()
    {
        // Button - start full sequence with default time
        /*if (activateButton)
        {
            _activateCB = () => RunSequence(defaultRunTimeSeconds);
            activateButton.onClick.AddListener(_activateCB);
        }*/
        // (Optional) iniciarBtn mirrors the same behavior
        /*if (iniciarBtn)
        {
            _iniciarCB = () => RunSequence(defaultRunTimeSeconds);
            iniciarBtn.onClick.AddListener(_iniciarCB);
        }*/
        if (iniciarBtn)
            iniciarBtn.onClick.AddListener(() =>
                RunSequence(Mathf.RoundToInt(tiempo)));
        // Slider - live update of 'tiempo'
        if (timerSlider)
        {
            timerSlider.SetValueWithoutNotify(Mathf.Max(1f, tiempo > 0 ? tiempo : defaultRunTimeSeconds));
            _timerCB = v => tiempo = Mathf.Max(1f, v);
            timerSlider.onValueChanged.AddListener(_timerCB);
        }
        // NEW: velocidad slider - sets speedDegPerSec directly
        if (velocidadSlider)
        {
            // Keep user’s existing slider range; just sync the current value.
            velocidadSlider.SetValueWithoutNotify(speedDegPerSec);
            _velCB = v => speedDegPerSec = Mathf.Max(0f, v);
            velocidadSlider.onValueChanged.AddListener(_velCB);
        }
        // Board/Unboard buttons (optional)
        if (ingresarBtn)
        {
            _ingresarCB = BoardPlayer;
            ingresarBtn.onClick.AddListener(_ingresarCB);
        }
        if (salirBtn)
        {
            _salirCB = UnboardPlayer;
            salirBtn.onClick.AddListener(_salirCB);
        }
    }

    void OnDestroy()
    {
        //if (activateButton && _activateCB != null) activateButton.onClick.RemoveListener(_activateCB);
        if (iniciarBtn && _iniciarCB != null) iniciarBtn.onClick.RemoveListener(_iniciarCB);
        if (timerSlider && _timerCB != null) timerSlider.onValueChanged.RemoveListener(_timerCB);
        if (velocidadSlider && _velCB != null) velocidadSlider.onValueChanged.RemoveListener(_velCB); // NEW
        if (ingresarBtn && _ingresarCB != null) ingresarBtn.onClick.RemoveListener(_ingresarCB);
        if (salirBtn && _salirCB != null) salirBtn.onClick.RemoveListener(_salirCB);
    }

    // ===== Public controls =====

    /// Starts a full sequence: Board - AutoRun(seconds) - Unboard
    public void RunSequence(int seconds)
    {
        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(RunSequenceCo(seconds));
    }

    /// Auto run without boarding/unboarding (useful for testing)
    public void RunForSeconds(int seconds)
    {
        if (seconds <= 0) return;
        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(AutoRun(seconds));
    }

    public void BoardPlayer()
    {
        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(BoardRoutine());
    }

    public void UnboardPlayer()
    {
        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(UnboardRoutine());
    }

    // ===== Core routines (adapted from Giroscopio) =====

    private IEnumerator RunSequenceCo(int seconds)
    {
        yield return BoardRoutine();
        yield return AutoRun(seconds);
        yield return UnboardRoutine();
    }

    private IEnumerator AutoRun(int seconds)
    {
        if (mecanismo == null) yield break;

        autoRunning = true;
        float endTime = Time.time + seconds;

        while (Time.time < endTime)
        {
            float dt = Time.deltaTime;
            RotateLocal(mecanismo, rotateLocalAxis, speedDegPerSec * dt);
            yield return null;
        }

        // Smoothly return to local identity (home)
        yield return ReturnToHome(mecanismo, 1.0f);

        autoRunning = false;
        runCo = null;
    }

    private IEnumerator ReturnToHome(Transform t, float duration)
    {
        if (t == null) yield break;

        Quaternion target = Quaternion.identity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float step = Time.deltaTime * speedDegPerSec; // reuse live speed
            t.localRotation = Quaternion.RotateTowards(t.localRotation, target, step);
            yield return null;
        }
        t.localRotation = Quaternion.identity;
    }

    private IEnumerator BoardRoutine()
    {
        if (asientoTP) asientoTP.RequestTeleport();
        if (delayAfterTeleportIn > 0f) yield return new WaitForSeconds(delayAfterTeleportIn);

        if (jugadorRig && asientoGO)
            jugadorRig.transform.SetParent(asientoGO.transform, true);
    }

    private IEnumerator UnboardRoutine()
    {
        if (jugadorRig)
            jugadorRig.transform.SetParent(null, true);

        if (delayBeforeTeleportOut > 0f) yield return new WaitForSeconds(delayBeforeTeleportOut);
        if (sueloTP) sueloTP.RequestTeleport();
    }

    // ===== Pelotas (unchanged behavior) =====

    private void OnTriggerExit(Collider other)
    {
        if (pelotaActual != null && other.gameObject == pelotaActual)
        {
            pelotaNeeded = true;
            InstanciarPelota();
        }
    }

    public void InstanciarPelota()
    {
        if (pelotaNeeded && pelotaWanted && pelotaPrefab && posicionInstancia && pelotas)
        {
            pelotaNeeded = false;
            GameObject nuevaPelota = Instantiate(
                pelotaPrefab,
                posicionInstancia.position,
                posicionInstancia.rotation,
                posicionInstancia.transform
            );
            pelotaActual = nuevaPelota;

            var grab = nuevaPelota.GetComponent<XRGrabInteractable>();
            if (grab != null)
                grab.selectExited.AddListener(OnPelotaSelectExited);
        }
    }

    private void OnPelotaSelectExited(SelectExitEventArgs args)
    {
        PelotaLanzada();
    }

    public void PelotaLanzada()
    {
        // hook for post-launch behavior if needed
    }

    // ===== Utilities =====

    private static void RotateLocal(Transform t, Vector3 localAxis, float deltaDegrees)
    {
        if (!t || Mathf.Approximately(deltaDegrees, 0f)) return;
        t.Rotate(localAxis.normalized * deltaDegrees, Space.Self);
    }
}
