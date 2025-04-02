using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RigidBodContoller : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Debug log
        Debug.Log("RigidBodContoller: Start");

        try
        {
            xrGrabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            if (xrGrabInteractable == null)
            {
                Debug.LogError("xrGrabInteractable is null in Start method of RigidBodContoller");
            }

            if (rb == null)
            {
                Debug.LogError("rb is null in Start method of RigidBodContoller");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in Start method of RigidBodContoller: " + ex.Message);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Public method to deactivate the XRGrabInteractable and set Rigidbody to be influenced by other objects
    public void DeactivateComponents()
    {
        // Debug log
        Debug.Log("RigidBodContoller: DeactivateComponents");

        try
        {
            if (xrGrabInteractable != null)
            {
                xrGrabInteractable.enabled = false;
            }
            else
            {
                Debug.LogError("xrGrabInteractable is null in DeactivateComponents method of RigidBodContoller");
            }

            if (rb != null)
            {
                rb.isKinematic = true; // Make the Rigidbody kinematic
                rb.detectCollisions = true; // Enable collision detection
            }
            else
            {
                Debug.LogError("rb is null in DeactivateComponents method of RigidBodContoller");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in DeactivateComponents method of RigidBodContoller: " + ex.Message);
        }
    }

    // Public method to reactivate the XRGrabInteractable and Rigidbody components
    public void ReactivateComponents()
    {
        // Debug log
        Debug.Log("RigidBodContoller: ReactivateComponents");

        try
        {
            if (xrGrabInteractable != null)
            {
                xrGrabInteractable.enabled = true;
            }
            else
            {
                Debug.LogError("xrGrabInteractable is null in ReactivateComponents method of RigidBodContoller");
            }

            if (rb != null)
            {
                rb.isKinematic = false; // Make the Rigidbody non-kinematic
                rb.detectCollisions = true; // Ensure collision detection is enabled
            }
            else
            {
                Debug.LogError("rb is null in ReactivateComponents method of RigidBodContoller");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in ReactivateComponents method of RigidBodContoller: " + ex.Message);
        }
    }
}
