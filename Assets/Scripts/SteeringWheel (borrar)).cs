using UnityEngine;

// Sistema de volante de dirección que calcula la entrada normalizada basada en el ángulo de rotación del volante.
// Lee la rotación local del transform del volante y la convierte en un valor entre -1 (izquierda) y 1 (derecha).
public class SteeringWheelNop : MonoBehaviour
{
    [Header("Steering Settings")]
    [Tooltip("Local Y-axis angle when the wheel is fully turned to the left.")]
    public float minSteeringAngle = -90f;    // Ángulo en el eje Y local cuando el volante está totalmente girado a la izquierda

    [Tooltip("Local Y-axis angle when the wheel is fully turned to the right.")]
    public float maxSteeringAngle = 90f;     // Ángulo en el eje Y local cuando el volante está totalmente girado a la derecha

    [Tooltip("The transform that rotates with the wheel.")]
    public Transform wheelTransform;         // Transform del volante que rota con la entrada del jugador         // Transform del volante que rota con la entrada del jugador

    // Valor de entrada de dirección normalizado entre -1 (izquierda) y 1 (derecha)
    public float SteeringInput { get; private set; }

    // Lee el ángulo de rotación del volante y calcula el valor de entrada normalizado
    void Update()
    {
        if (wheelTransform == null)
            return;

        float yRotation = NormalizeAngle(wheelTransform.localEulerAngles.y);

        // Clamp and normalize
        float clampedY = Mathf.Clamp(yRotation, minSteeringAngle, maxSteeringAngle);
        SteeringInput = Mathf.InverseLerp(minSteeringAngle, maxSteeringAngle, clampedY) * 2f - 1f;
    }

    // Normaliza un ángulo al rango -180 a 180 grados
    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
