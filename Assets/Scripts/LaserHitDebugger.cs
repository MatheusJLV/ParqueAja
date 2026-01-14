using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Herramienta de depuración que rastrea y visualiza el punto de impacto del rayo láser
// Sigue el tip del rayo en tiempo real y muestra información de depuración
public class LaserHitDebugger : MonoBehaviour
{
    [Header("Near-Far Interactors")]
    // Referencia al interactor del láser izquierdo
    [SerializeField] private NearFarInteractor leftInteractor;

    [Header("Follower Target")]
    // Objeto que se mueve y rota según el tip del rayo láser
    [SerializeField] private Transform target;

    [Header("Debug UI")]
    // Texto que muestra información de depuración del rayo
    [SerializeField] private TMP_Text debugText;

    // Actualiza la posición y rotación del objeto objetivo según el rayo láser cada frame
    private void Update()
    {
        // Valida que el interactor esté disponible
        if (leftInteractor == null)
            return;

        // 1. Obtiene el origen del rayo (posición del controlador)
        Vector3 origin = leftInteractor.transform.position;

        // 2. Obtiene el punto final del rayo láser (dónde impacta)
        leftInteractor.TryGetCurveEndPoint(
            out Vector3 end,
            snapToSelectedAttachIfAvailable: false,
            snapToSnapVolumeIfAvailable: false);

        // 3. Calcula la dirección forward del rayo (normalizada)
        Vector3 forward = (end - origin).normalized;

        // 4. Construye una base de rotación ortogonal
        // Usa el eje up como referencia para mantener la orientación correcta
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        // Recalcula up asegurando ortogonalidad con forward y right
        up = Vector3.Cross(forward, right).normalized;
        // Crea la rotación que orienta el objeto según el rayo
        Quaternion rotation = Quaternion.LookRotation(forward, up);

        // === Aplica transformación al objeto objetivo ===
        if (target != null)
        {
            // Posiciona el objeto en el tip del rayo
            target.position = end;
            // Orienta el objeto según la dirección del rayo
            target.rotation = rotation;
        }

        // === Salida de depuración (opcional) ===
        if (debugText != null)
        {
            // Muestra información detallada del rayo en pantalla
            debugText.text =
                $"Left Laser:\n" +
                $" Origin: {origin}\n" +
                $" End: {end}\n" +
                $" Direction: {forward}\n" +
                $" Rotation (Euler): {rotation.eulerAngles}\n";
        }
    }
}

