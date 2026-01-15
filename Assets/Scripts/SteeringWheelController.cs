using UnityEngine;

// Controlador de volante que gestiona la rotación y restringe el ángulo de giro dentro de un rango mínimo y máximo.
// Calcula el ángulo actual del volante basado en su rotación local y proporciona acceso público a este valor.
public class SteeringWheelController : MonoBehaviour
{
    [SerializeField] Transform centerAnchor;      // Transform del ancla central del volante
    [SerializeField] Vector3 rotationAxis = Vector3.up;  // Eje de rotación del volante (por defecto el eje Y)
    [SerializeField] float minAngle = -90f;       // Ángulo mínimo de rotación permitido (grados)
    [SerializeField] float maxAngle = 90f;        // Ángulo máximo de rotación permitido (grados)        // Ángulo máximo de rotación permitido (grados)

    float currentAngle = 0f;                      // Ángulo actual del volante
    Quaternion initialRotation;                   // Rotación inicial del volante guardada al inicio                   // Rotación inicial del volante guardada al inicio

    // Guarda la rotación inicial del volante al comenzar
    void Start()
    {
        initialRotation = transform.localRotation;
    }

    // Calcula el ángulo actual del volante y aplica restricciones de rango mínimo/máximo
    void Update()
    {
        Vector3 localForward = transform.localRotation * Vector3.forward;
        float angle = Vector3.SignedAngle(Vector3.forward, localForward, rotationAxis);

        angle = Mathf.Clamp(angle, minAngle, maxAngle);
        currentAngle = angle;

        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }

    // Devuelve el ángulo actual del volante
    public float GetSteeringAngle() => currentAngle;
}
