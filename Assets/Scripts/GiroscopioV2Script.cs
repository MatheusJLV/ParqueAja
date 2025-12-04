using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using System.Collections;

public class GiroscopioV2Script : MonoBehaviour
{
    [Header("Ring Transforms")]
    public Transform outerRing;
    public Transform innerRing;

    [Header("Rotation Axes")]
    public Vector3 outerLocalAxis = Vector3.right;     // outer uses local X
    public Vector3 innerLocalAxis = Vector3.forward;   // inner uses local Z

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
    public TeleportationAnchor asiento;  // entry anchor (inside the gyro)
    public TeleportationAnchor suelo;    // exit anchor   (outside)
    public GameObject asientoGO;         // passenger mount under inner ring
    public GameObject jugadorRig;        // XR Origin / XR Rig root

    [Header("Boarding Timing")]
    public float delayAfterTeleportIn = 0.35f; // wait before parenting
    public float delayBeforeTeleportOut = 0.35f; // wait after unparenting

    [Header("UI (optional)")]
    public Button iniciarBtn;        // optional second trigger button
    public Slider velocidadSlider;   // controls both ring speeds

    private InputDevice leftHand, rightHand;

    private enum RingState { Idle, Manual, Coasting, RecoverRamp, RecoverContinue }
    private RingState outerState = RingState.Idle;
    private RingState innerState = RingState.Idle;

    private float outerCurrentSpeed = 0f;
    private float innerCurrentSpeed = 0f;
    private float outerRecoverySpeed = 0f;
    private float innerRecoverySpeed = 0f;

    private bool autoRunning = false;
    private Coroutine autoCo;

    private UnityEngine.Events.UnityAction _iniciarBtnCB;
    private UnityEngine.Events.UnityAction _activateBtnCB;
    private UnityEngine.Events.UnityAction<float> _velocidadCB;

    void Start()
    {
        AcquireDevices();

        // Button now runs the full board - auto - unboard sequence.
        if (activateButton != null)
        {
            _activateBtnCB = () => RunSequence(defaultRunTimeSeconds);
            activateButton.onClick.AddListener(_activateBtnCB);
        }

        if (iniciarBtn != null)
        {
            _iniciarBtnCB = () => RunSequence(defaultRunTimeSeconds);
            iniciarBtn.onClick.AddListener(_iniciarBtnCB);
        }

        if (velocidadSlider != null)
        {
            ApplyVelocidad(velocidadSlider.value);
            _velocidadCB = v => ApplyVelocidad(v);
            velocidadSlider.onValueChanged.AddListener(_velocidadCB);
        }
    }

    private void ApplyVelocidad(float v)
    {
        outerSpeedDegPerSec = v;
        innerSpeedDegPerSec = v;
    }

    void OnDestroy()
    {
        if (activateButton != null && _activateBtnCB != null)
            activateButton.onClick.RemoveListener(_activateBtnCB);
        if (iniciarBtn != null && _iniciarBtnCB != null)
            iniciarBtn.onClick.RemoveListener(_iniciarBtnCB);
        if (velocidadSlider != null && _velocidadCB != null)
            velocidadSlider.onValueChanged.RemoveListener(_velocidadCB);
    }

    void Update()
    {
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

    private void UpdateRing(ref RingState state, ref float currentSpeed, ref float recoverySpeed,
                            Transform ring, Vector3 localAxis, float dt)
    {
        float signedAngle = GetSignedTwistAngleDeg(ring.localRotation, localAxis);
        float angleToHome = Mathf.Abs(signedAngle);

        switch (state)
        {
            case RingState.Manual:
                if (angleToHome <= restAngleEpsilon)
                {
                    SnapHome(ring, ref currentSpeed, ref recoverySpeed, ref state);
                    break;
                }
                state = RingState.Coasting;
                break;

            case RingState.Coasting:
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
                if (angleToHome <= restAngleEpsilon)
                {
                    SnapHome(ring, ref currentSpeed, ref recoverySpeed, ref state);
                    break;
                }
                recoverySpeed = Mathf.MoveTowards(recoverySpeed, recoveryTargetSpeedDegPerSec, recoveryAccelDegPerSec2 * dt);
                float step = Mathf.Max(recoverySpeed, speedEpsilonNearZero) * dt;
                ring.localRotation = Quaternion.RotateTowards(ring.localRotation, Quaternion.identity, step);
                break;

            case RingState.Idle:
                if (angleToHome > restAngleEpsilon)
                {
                    currentSpeed = 0f;
                    recoverySpeed = 0f;
                    state = RingState.RecoverRamp;
                }
                break;
        }
    }

    // ===== Boarding / Unboarding =====

    public void BoardPlayer()
    {
        StartCoroutine(BoardRoutine());
    }

    public void UnboardPlayer()
    {
        StartCoroutine(UnboardRoutine());
    }

    private IEnumerator BoardRoutine()
    {
        if (asiento != null) asiento.RequestTeleport();
        if (delayAfterTeleportIn > 0f) yield return new WaitForSeconds(delayAfterTeleportIn);

        if (jugadorRig != null && asientoGO != null)
            jugadorRig.transform.SetParent(asientoGO.transform, true);
    }

    private IEnumerator UnboardRoutine()
    {
        if (jugadorRig != null)
            jugadorRig.transform.SetParent(null, true);

        if (delayBeforeTeleportOut > 0f) yield return new WaitForSeconds(delayBeforeTeleportOut);
        if (suelo != null) suelo.RequestTeleport();
    }

    // Full sequence: board - auto-run - unboard
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

    // ===== Automation =====

    public void RunForSeconds(int seconds)
    {
        if (seconds <= 0) return;
        if (autoCo != null) StopCoroutine(autoCo);
        autoCo = StartCoroutine(AutoRun(seconds));
    }

    private IEnumerator AutoRun(int seconds)
    {
        autoRunning = true;
        float oSpeed = Mathf.Abs(outerSpeedDegPerSec);
        float iSpeed = Mathf.Abs(innerSpeedDegPerSec);

        float endTime = Time.time + seconds;
        while (Time.time < endTime)
        {
            float dt = Time.deltaTime;
            if (outerRing) RotateLocal(outerRing, outerLocalAxis, oSpeed * dt);
            if (innerRing) RotateLocal(innerRing, innerLocalAxis, iSpeed * dt);
            yield return null;
        }

        autoRunning = false;
        autoCo = null;

        outerState = RingState.RecoverRamp;
        innerState = RingState.RecoverRamp;
    }

    // ===== Utilities =====
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

    private static void SnapHome(Transform ring, ref float currentSpeed, ref float recoverySpeed, ref RingState state)
    {
        ring.localRotation = Quaternion.identity;
        currentSpeed = 0f;
        recoverySpeed = 0f;
        state = RingState.Idle;
    }

    private static void RotateLocal(Transform t, Vector3 localAxis, float deltaDegrees)
    {
        if (Mathf.Abs(deltaDegrees) > 0f)
            t.Rotate(localAxis.normalized * deltaDegrees, Space.Self);
    }

    private void AcquireDevices()
    {
        var lefts = new System.Collections.Generic.List<InputDevice>();
        var rights = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, lefts);
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, rights);
        leftHand = lefts.Count > 0 ? lefts[0] : default;
        rightHand = rights.Count > 0 ? rights[0] : default;
    }

    private static bool GetButton(InputDevice device, InputFeatureUsage<bool> usage)
    {
        return device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed;
    }
}
