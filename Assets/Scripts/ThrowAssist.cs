using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]

// Sistema experimental de asistencia de lanzamiento para objetos VR que amplifica las velocidades al soltar.
// Aplica multiplicadores a las velocidades lineal y angular, y asegura una velocidad mínima de lanzamiento.
// EXPERIMENTAL
public class ThrowAssist : MonoBehaviour
{
    [Header("Assist")]
    [Range(1f, 3f)] public float linearBoost = 1.8f;   // Multiplicador de la velocidad lineal al soltar
    [Range(1f, 2f)] public float angularBoost = 1.1f;  // Multiplicador de la velocidad angular al soltar
    public float minReleaseSpeed = 2.0f;               // Velocidad mínima garantizada: asegura que lanzamientos débiles aún tengan arco

    XRGrabInteractable grab;  // Referencia al componente de agarre XR
    Rigidbody rb;             // Referencia al Rigidbody del objeto             // Referencia al Rigidbody del objeto

    // Inicializa las referencias y suscribe el listener del evento de soltar
    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        grab.selectExited.AddListener(OnReleased);
    }

    // Desuscribe el listener cuando el objeto se destruye
    void OnDestroy()
    {
        if (grab) grab.selectExited.RemoveListener(OnReleased);
    }

    // Maneja el evento de soltar el objeto, aplicando los multiplicadores de velocidad y el clamp de velocidad mínima
    void OnReleased(SelectExitEventArgs args)
    {
        // Usar las velocidades calculadas por XRIT, luego amplificarlas
        Vector3 v = rb.linearVelocity * linearBoost;
        Vector3 w = rb.angularVelocity * angularBoost;

        // Clamp de velocidad mínima (preservando la dirección)
        float speed = v.magnitude;
        if (speed < minReleaseSpeed && speed > 1e-4f)
            v = v.normalized * minReleaseSpeed;

        rb.linearVelocity = v;
        rb.angularVelocity = w;
    }
}
