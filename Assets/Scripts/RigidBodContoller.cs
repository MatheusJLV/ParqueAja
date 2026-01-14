using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Controlador para activar/desactivar componentes XRGrabInteractable y Rigidbody
// Permite alternar entre modos de interacción y física del objeto
public class RigidBodContoller : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable; // Componente de agarrado XR
    private Rigidbody rb; // Rigidbody del objeto

    // Obtiene referencias a los componentes necesarios y valida su existencia
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

    // Update se llama una vez por frame
    void Update()
    {

    }

    // Desactiva el componente XRGrabInteractable y hace el Rigidbody kinemático
    // Esto hace que el objeto no pueda ser agarrado pero pueda ser influenciado por otros objetos
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

    // Reactiva el componente XRGrabInteractable y hace el Rigidbody no kinemático
    // Esto restaura la capacidad de agarrar el objeto y su comportamiento físico normal
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
