using UnityEngine;

public class SteeringWheelController : MonoBehaviour
{
    [SerializeField] Transform centerAnchor;
    [SerializeField] Vector3 rotationAxis = Vector3.up;
    [SerializeField] float minAngle = -90f;
    [SerializeField] float maxAngle = 90f;

    float currentAngle = 0f;
    Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        Vector3 localForward = transform.localRotation * Vector3.forward;
        float angle = Vector3.SignedAngle(Vector3.forward, localForward, rotationAxis);

        angle = Mathf.Clamp(angle, minAngle, maxAngle);
        currentAngle = angle;

        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }

    public float GetSteeringAngle() => currentAngle;
}
