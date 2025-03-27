using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RigidBodContoller : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        xrGrabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Public method to deactivate the XRGrabInteractable and set Rigidbody to be influenced by other objects
    public void DeactivateComponents()
    {
        if (xrGrabInteractable != null)
        {
            xrGrabInteractable.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = true; // Make the Rigidbody kinematic
            rb.detectCollisions = true; // Enable collision detection
        }
    }

    // Public method to reactivate the XRGrabInteractable and Rigidbody components
    public void ReactivateComponents()
    {
        if (xrGrabInteractable != null)
        {
            xrGrabInteractable.enabled = true;
        }

        if (rb != null)
        {
            rb.isKinematic = false; // Make the Rigidbody non-kinematic
            rb.detectCollisions = true; // Ensure collision detection is enabled
        }
    }
}
