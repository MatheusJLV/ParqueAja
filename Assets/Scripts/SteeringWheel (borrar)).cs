using UnityEngine;

public class SteeringWheelNop : MonoBehaviour
{
    [Header("Steering Settings")]
    [Tooltip("Local Y-axis angle when the wheel is fully turned to the left.")]
    public float minSteeringAngle = -90f;

    [Tooltip("Local Y-axis angle when the wheel is fully turned to the right.")]
    public float maxSteeringAngle = 90f;

    [Tooltip("The transform that rotates with the wheel.")]
    public Transform wheelTransform;

    /// <summary>
    /// Normalized steering input value between -1 (left) and 1 (right).
    /// </summary>
    public float SteeringInput { get; private set; }

    void Update()
    {
        if (wheelTransform == null)
            return;

        float yRotation = NormalizeAngle(wheelTransform.localEulerAngles.y);

        // Clamp and normalize
        float clampedY = Mathf.Clamp(yRotation, minSteeringAngle, maxSteeringAngle);
        SteeringInput = Mathf.InverseLerp(minSteeringAngle, maxSteeringAngle, clampedY) * 2f - 1f;
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
