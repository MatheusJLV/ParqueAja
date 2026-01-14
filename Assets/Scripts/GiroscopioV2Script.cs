using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using System.Collections;

public class GiroscopioV2Script : MonoBehaviour
{
    /*
     Controla un giroscopio de dos anillos con rotación manual/automática,
     desaceleración, recuperación a posición inicial y sistema de embarque/desembarque.
    */

    [Header("Ring Transforms")]
    public Transform outerRing;
    public Transform innerRing;

    [Header("Rotation Axes")]
    public Vector3 outerLocalAxis = Vector3.right;     // Anillo exterior usa X local
    public Vector3 innerLocalAxis = Vector3.forward;   // Anillo interior usa Z local

    [Header("Manual Speeds (deg/s)")]
    public float outerSpeedDegPerSec = 120f;
    public float innerSpeedDegPerSec = 120f;

    [Header("Release Phases")]
    public float decelRateDegPerSec2 = 360f;
    public float recoveryTargetSpeedDegPerSec = 90f;
    public float recoveryAccelDegPerSec2 = 360f;

    [Header("Stopping Thresholds")]
    public float restAngleEpsilon = 0.5f;
    public float speedEpsilonNearZero = 0.05f;

    [Header("Controls")]
    public bool primaryInvertsDirection = true;

    [Header("Automation Settings")]
    public int defaultRunTimeSeconds = 8;
    public Button activateButton;

    [Header("XR Boarding References")]
    public TeleportationAnchor asiento;  // Ancla de entrada (dentro del giroscopio)
    public TeleportationAnchor suelo;    // Ancla de salida (fuera)
    public GameObject asientoGO;         // Punto de montaje del pasajero bajo el anillo interior
    public GameObject jugadorRig;        // Raíz del XR Origin / XR Rig

    [Header("Boarding Timing")]
    public float delayAfterTeleportIn = 0.35f; // Espera antes de parentear
    public float delayBeforeTeleportOut = 0.35f; // Espera después de desparentear

    [Header("UI (optional)")]
    public Button iniciarBtn;        // Botón alternativo de inicio
    public Slider velocidadSlider;   // Controla la velocidad de ambos anillos

    private InputDevice leftHand, rightHand;

    // Estados de cada anillo
    private enum RingState { Idle, Manual, Coasting, RecoverRamp, RecoverContinue }
    private RingState outerState = RingState.Idle;
    private RingState innerState = RingState.Idle;

    // Velocidades actuales de cada anillo
    private float outerCurrentSpeed = 0f;
    private float innerCurrentSpeed = 0f;
    private float outerRecoverySpeed = 0f;
    private float innerRecoverySpeed = 0f;

    // Control de automatización
    private bool autoRunning = false;
    private Coroutine autoCo;

    // Callbacks de UI
    private UnityEngine.Events.UnityAction _iniciarBtnCB;
    private UnityEngine.Events.UnityAction _activateBtnCB;
    private UnityEngine.Events.UnityAction<float> _velocidadCB;

    void Start()
    {
        // Adquiere referencias a los dispositivos de entrada XR
        AcquireDevices();

        // Botón ejecuta la secuencia completa: embarcar - auto - desembarcar
        if (activateButton != null)
        {
            _activateBtnCB = () => RunSequence(defaultRunTimeSeconds);
            activateButton.onClick.AddListener(_activateBtnCB);
        }

        // Botón alternativo para iniciar la secuencia
        if (iniciarBtn != null)
        {
            _iniciarBtnCB = () => RunSequence(defaultRunTimeSeconds);
            iniciarBtn.onClick.AddListener(_iniciarBtnCB);
        }

        // Slider controla la velocidad de ambos anillos
        if (velocidadSlider != null)
        {
            ApplyVelocidad(velocidadSlider.value);
            _velocidadCB = v => ApplyVelocidad(v);
            velocidadSlider.onValueChanged.AddListener(_velocidadCB);
        }
    }

    // Aplica el valor del slider a las velocidades de ambos anillos
    private void ApplyVelocidad(float v)
    {
        outerSpeedDegPerSec = v;
        innerSpeedDegPerSec = v;
    }

    void OnDestroy()
    {
        // Limpia listeners de botones y slider
        if (activateButton != null && _activateBtnCB != null)
            activateButton.onClick.RemoveListener(_activateBtnCB);
        if (iniciarBtn != null && _iniciarBtnCB != null)
            iniciarBtn.onClick.RemoveListener(_iniciarBtnCB);
        if (velocidadSlider != null && _velocidadCB != null)
            velocidadSlider.onValueChanged.RemoveListener(_velocidadCB);
    }

    void Update()
    {
        // Si está en modo automático, no procesa entrada manual
        if (autoRunning) return;

        //if (!leftHand.isValid || !rightHand.isValid) AcquireDevices();

        /*bool rightSecondary = GetButton(rightHand, CommonUsages.secondaryButton);
        bool leftSecondary = GetButton(leftHand, CommonUsages.secondaryButton);
        bool rightPrimary = primaryInvertsDirection && GetButton(rightHand, CommonUsages.primaryButton);
        bool leftPrimary = primaryInvertsDirection && GetButton(leftHand, CommonUsages.primaryButton);*/

        float dt = Time.deltaTime;

        /*if (outerRing != null)
        {
            if (rightSecondary)
            {
                float dir = rightPrimary ? -1f : 1f;
                outerCurrentSpeed = outerSpeedDegPerSec * dir;
                outerState = RingState.Manual;
                RotateLocal(outerRing, outerLocalAxis, outerCurrentSpeed * dt);
                outerRecoverySpeed = 0f;
            }
            else
            {
                UpdateRing(ref outerState, ref outerCurrentSpeed, ref outerRecoverySpeed, outerRing, outerLocalAxis, dt);
            }
        }

        if (innerRing != null)
        {
            if (leftSecondary)
            {
                float dir = leftPrimary ? -1f : 1f;
                innerCurrentSpeed = innerSpeedDegPerSec * dir;
                innerState = RingState.Manual;
                RotateLocal(innerRing, innerLocalAxis, innerCurrentSpeed * dt);
                innerRecoverySpeed = 0f;
            }
            else
            {
                UpdateRing(ref innerState, ref innerCurrentSpeed, ref innerRecoverySpeed, innerRing, innerLocalAxis, dt);
            }
        }*/
    }

    // Actualiza el estado y rotación de un anillo según su fase actual
    private void UpdateRing(ref RingState state, ref float currentSpeed, ref float recoverySpeed,
                            Transform ring, Vector3 localAxis, float dt)
    {
        // Calcula el ángulo actual respecto a la posición inicial
        float signedAngle = GetSignedTwistAngleDeg(ring.localRotation, localAxis);
        float angleToHome = Mathf.Abs(signedAngle);

        switch (state)
        {
            case RingState.Manual:
                // Si está muy cerca de la posición inicial, regresa de inmediato
                if (angleToHome <= restAngleEpsilon)
                {
                    SnapHome(ring, ref currentSpeed, ref recoverySpeed, ref state);
                    break;
                }
                // De lo contrario, comienza a desacelerar
                state = RingState.Coasting;
                break;

            case RingState.Coasting:
                // Desacelera gradualmente hasta detenerse
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decelRateDegPerSec2 * dt);
                if (Mathf.Abs(currentSpeed) > speedEpsilonNearZero)
                    RotateLocal(ring, localAxis, currentSpeed * dt);
                else
                {
                    currentSpeed = 0f;
                    recoverySpeed = 0f;
                    state = RingState.RecoverRamp;
                }
                break;

            case RingState.RecoverRamp:
                // Recuperación hacia posición inicial
                if (angleToHome <= restAngleEpsilon)
                {
                    SnapHome(ring, ref currentSpeed, ref recoverySpeed, ref state);
                    break;
                }
                // Acelera hasta velocidad de recuperación objetivo
                recoverySpeed = Mathf.MoveTowards(recoverySpeed, recoveryTargetSpeedDegPerSec, recoveryAccelDegPerSec2 * dt);
                float step = Mathf.Max(recoverySpeed, speedEpsilonNearZero) * dt;
                ring.localRotation = Quaternion.RotateTowards(ring.localRotation, Quaternion.identity, step);
                break;

            case RingState.Idle:
                // Si se desvió de la posición inicial, comienza recuperación
                if (angleToHome > restAngleEpsilon)
                {
                    currentSpeed = 0f;
                    recoverySpeed = 0f;
                    state = RingState.RecoverRamp;
                }
                break;
        }
    }

    // ===== Embarque / Desembarque =====

    public void BoardPlayer()
    {
        StartCoroutine(BoardRoutine());
    }

    public void UnboardPlayer()
    {
        StartCoroutine(UnboardRoutine());
    }

    // Rutina de embarque: teletransporta y parentea al jugador al asiento
    private IEnumerator BoardRoutine()
    {
        // Teletransporta al anclaje del asiento
        if (asiento != null) asiento.RequestTeleport();
        // Espera breve antes de parentear
        if (delayAfterTeleportIn > 0f) yield return new WaitForSeconds(delayAfterTeleportIn);

        // Parentea el rig del jugador al asiento para que gire con él
        if (jugadorRig != null && asientoGO != null)
            jugadorRig.transform.SetParent(asientoGO.transform, true);
    }

    // Rutina de desembarque: desparentea y teletransporta al jugador fuera
    private IEnumerator UnboardRoutine()
    {
        // Desparentea el rig del jugador
        if (jugadorRig != null)
            jugadorRig.transform.SetParent(null, true);

        // Espera breve antes de teletransportar
        if (delayBeforeTeleportOut > 0f) yield return new WaitForSeconds(delayBeforeTeleportOut);
        // Teletransporta al anclaje del suelo (salida)
        if (suelo != null) suelo.RequestTeleport();
    }

    // Secuencia completa: embarcar - ejecución automática - desembarcar
    public void RunSequence(int seconds)
    {
        if (autoCo != null) StopCoroutine(autoCo);
        autoCo = StartCoroutine(RunSequenceCo(seconds));
    }

    private IEnumerator RunSequenceCo(int seconds)
    {
        yield return BoardRoutine();
        yield return AutoRun(seconds);
        yield return UnboardRoutine();
    }

    // ===== Automatización =====

    public void RunForSeconds(int seconds)
    {
        if (seconds <= 0) return;
        if (autoCo != null) StopCoroutine(autoCo);
        autoCo = StartCoroutine(AutoRun(seconds));
    }

    // Ejecuta ambos anillos automáticamente durante un tiempo determinado
    private IEnumerator AutoRun(int seconds)
    {
        autoRunning = true;
        float oSpeed = Mathf.Abs(outerSpeedDegPerSec);
        float iSpeed = Mathf.Abs(innerSpeedDegPerSec);

        float endTime = Time.time + seconds;
        // Rota ambos anillos continuamente hasta que termine el tiempo
        while (Time.time < endTime)
        {
            float dt = Time.deltaTime;
            if (outerRing) RotateLocal(outerRing, outerLocalAxis, oSpeed * dt);
            if (innerRing) RotateLocal(innerRing, innerLocalAxis, iSpeed * dt);
            yield return null;
        }

        autoRunning = false;
        autoCo = null;

        // Al terminar, inicia recuperación hacia posición inicial
        outerState = RingState.RecoverRamp;
        innerState = RingState.RecoverRamp;
    }

    // ===== Utilidades =====
    // Calcula el ángulo de torsión (twist) con signo sobre un eje local
    private static float GetSignedTwistAngleDeg(Quaternion localRotation, Vector3 axisLocal)
    {
        Vector3 n = axisLocal.normalized;
        float w = localRotation.w;
        Vector3 v = new Vector3(localRotation.x, localRotation.y, localRotation.z);
        float vDot = Vector3.Dot(v, n);
        Vector3 vParallel = n * vDot;
        Quaternion twist = new Quaternion(vParallel.x, vParallel.y, vParallel.z, w);
        twist = NormalizeSafe(twist);

        float len = vParallel.magnitude;
        float angleRad = 2f * Mathf.Atan2(len, Mathf.Abs(w) + 1e-8f);
        float angleDeg = angleRad * Mathf.Rad2Deg;
        float sign = Mathf.Sign(vDot);
        return angleDeg * sign;
    }

    // Normaliza un quaternion de forma segura
    private static Quaternion NormalizeSafe(Quaternion q)
    {
        float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (mag > 1e-8f)
        {
            float inv = 1f / mag;
            return new Quaternion(q.x * inv, q.y * inv, q.z * inv, q.w * inv);
        }
        return Quaternion.identity;
    }

    // Regresa el anillo a su posición inicial y resetea velocidades
    private static void SnapHome(Transform ring, ref float currentSpeed, ref float recoverySpeed, ref RingState state)
    {
        ring.localRotation = Quaternion.identity;
        currentSpeed = 0f;
        recoverySpeed = 0f;
        state = RingState.Idle;
    }

    // Rota un transform en espacio local
    private static void RotateLocal(Transform t, Vector3 localAxis, float deltaDegrees)
    {
        if (Mathf.Abs(deltaDegrees) > 0f)
            t.Rotate(localAxis.normalized * deltaDegrees, Space.Self);
    }

    // Adquiere referencias a los dispositivos XR de las manos
    private void AcquireDevices()
    {
        var lefts = new System.Collections.Generic.List<InputDevice>();
        var rights = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, lefts);
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, rights);
        leftHand = lefts.Count > 0 ? lefts[0] : default;
        rightHand = rights.Count > 0 ? rights[0] : default;
    }

    // Verifica si un botón está presionado en un dispositivo XR
    private static bool GetButton(InputDevice device, InputFeatureUsage<bool> usage)
    {
        return device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed;
    }
}
