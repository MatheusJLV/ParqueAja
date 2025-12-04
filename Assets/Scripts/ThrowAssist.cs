using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]

//EXPERIMENTAL
public class ThrowAssist : MonoBehaviour
{
    [Header("Assist")]
    [Range(1f, 3f)] public float linearBoost = 1.8f;
    [Range(1f, 2f)] public float angularBoost = 1.1f;
    public float minReleaseSpeed = 2.0f;   // clamp: ensures weak flicks still arc

    XRGrabInteractable grab;
    Rigidbody rb;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (grab) grab.selectExited.RemoveListener(OnReleased);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Use the velocities XRIT computed, then boost
        Vector3 v = rb.linearVelocity * linearBoost;
        Vector3 w = rb.angularVelocity * angularBoost;

        // Minimum speed clamp (preserve direction)
        float speed = v.magnitude;
        if (speed < minReleaseSpeed && speed > 1e-4f)
            v = v.normalized * minReleaseSpeed;

        rb.linearVelocity = v;
        rb.angularVelocity = w;
    }
}
