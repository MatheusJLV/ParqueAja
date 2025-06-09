using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

[RequireComponent(typeof(LocomotionSystem))]
public class DriveLocomotion : LocomotionProvider
{
    public Transform volante; // Steering wheel object
    public float acceleration = 1.5f;
    public float reverseAcceleration = 1.0f;
    public float maxSpeed = 5f;
    public float steeringSensitivity = 1f;

    private float currentSpeed = 0f;
    private CharacterController driver;
    private Transform rigTransform;

    void Start()
    {
        rigTransform = system.xrOrigin.CameraFloorOffsetObject.transform;
        driver = system.xrOrigin.GetComponent<CharacterController>();
    }

    void Update()
    {
        // Only move if no other locomotion is active
        if (!CanBeginLocomotion()) return;

        // Get right hand input
        bool rightPrimary = false;
        bool rightSecondary = false;
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimary);
        rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondary);

        float delta = 0f;

        if (rightPrimary)
            delta += acceleration * Time.deltaTime;
        if (rightSecondary)
            delta -= reverseAcceleration * Time.deltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed + delta, -maxSpeed, maxSpeed);

        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            BeginLocomotion();

            // Get steering input from volante rotation
            float steering = volante != null ? volante.localEulerAngles.y : 0f;

            // Normalize to range [-180, 180]
            if (steering > 180f) steering -= 360f;

            Vector3 forward = Quaternion.Euler(0f, steering * steeringSensitivity, 0f) * rigTransform.forward;
            Vector3 motion = forward.normalized * currentSpeed * Time.deltaTime;

            driver.Move(motion);

            EndLocomotion();
        }
    }
}

