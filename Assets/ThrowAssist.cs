using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]

//EXPERIMENTAL: amplifica velocidad de lanzamiento en XR

public class ThrowAssist : MonoBehaviour
{
    [Header("Assist")]
    //Multiplicador de velocidad lineal
    [Range(1f, 3f)] public float linearBoost = 1.8f;
    //Multiplicador de velocidad angular
    [Range(1f, 2f)] public float angularBoost = 1.1f;
    //Velocidad mínima garantizada al lanzar
    public float minReleaseSpeed = 2.0f;   // clamp: ensures weak flicks still arc
    //Componentes cacheados
    XRGrabInteractable grab;
    Rigidbody rb;

    //Awake: cachear componentes y conectar listener
    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        grab.selectExited.AddListener(OnReleased);
    }
    //OnDestroy: desconectar listener
    void OnDestroy()
    {
        if (grab) grab.selectExited.RemoveListener(OnReleased);
    }
    //OnReleased: amplificar velocidades y aplicar clamp mínimo
    void OnReleased(SelectExitEventArgs args)
    {
        // Use the velocities XRIT computed, then boost
        Vector3 v = rb.linearVelocity * linearBoost;
        Vector3 w = rb.angularVelocity * angularBoost;

        // Minimum speed clamp (preserve direction)
        float speed = v.magnitude;
        if (speed < minReleaseSpeed && speed > 1e-4f)
            v = v.normalized * minReleaseSpeed;
        //Aplicar velocidades finales al Rigidbody
        rb.linearVelocity = v;
        rb.angularVelocity = w;
    }
}
