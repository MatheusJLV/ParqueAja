using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/* Controlador para manejar componentes XRGrabInteractable y Rigidbody en un mismo GameObject.
    Añade métodos públicos para desactivar/reactivar componentes y contiene logs para depuración.*/
public class RigidBodContoller : MonoBehaviour
{
    // Referencia al componente XRGrabInteractable (usado para interacción en XR).
    private XRGrabInteractable xrGrabInteractable;
    // Referencia al componente Rigidbody (usado para física).
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Log de depuración indicando que Start se ejecutó.
        Debug.Log("RigidBodContoller: Start");

        try
        {
            // Intentar obtener las referencias a los componentes en el mismo GameObject.
            xrGrabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            // Si no se encontró XRGrabInteractable, registrar error para facilitar la depuración.
            if (xrGrabInteractable == null)
            {
                Debug.LogError("xrGrabInteractable is null in Start method of RigidBodContoller");
            }

            // Si no se encontró Rigidbody, registrar error para facilitar la depuración.
            if (rb == null)
            {
                Debug.LogError("rb is null in Start method of RigidBodContoller");
            }
        }
        catch (System.Exception ex)
        {
            // Capturar cualquier excepción inesperada y registrarla.
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
        // Log de depuración indicando que se llamó al método.
        Debug.Log("RigidBodContoller: DeactivateComponents");

        try
        {
            // Desactivar la interacción XR si existe la referencia.
            if (xrGrabInteractable != null)
            {
                xrGrabInteractable.enabled = false;
            }
            else
            {
                // Registrar error si falta la referencia.
                Debug.LogError("xrGrabInteractable is null in DeactivateComponents method of RigidBodContoller");
            }
            // Ajustar el Rigidbody si existe la referencia.
            if (rb != null)
            {
                rb.isKinematic = true; // Make the Rigidbody kinematic
                rb.detectCollisions = true; // Enable collision detection
            }
            else
            {
                // Registrar error si falta la referencia al Rigidbody.
                Debug.LogError("rb is null in DeactivateComponents method of RigidBodContoller");
            }
        }
        catch (System.Exception ex)
        {
            // Capturar y registrar cualquier excepción que ocurra al desactivar componentes.
            Debug.LogError("Exception in DeactivateComponents method of RigidBodContoller: " + ex.Message);
        }
    }

    // Public method to reactivate the XRGrabInteractable and Rigidbody components
    public void ReactivateComponents()
    {
        // Log de depuración indicando que se llamó al método.
        Debug.Log("RigidBodContoller: ReactivateComponents");

        try
        {
            // Reactivar la interacción XR si existe la referencia.
            if (xrGrabInteractable != null)
            {
                xrGrabInteractable.enabled = true;
            }
            else
            {
                // Registrar error si falta la referencia.
                Debug.LogError("xrGrabInteractable is null in ReactivateComponents method of RigidBodContoller");
            }

            // Restaurar el comportamiento del Rigidbody si existe la referencia.
            if (rb != null)
            {
                rb.isKinematic = false; // Make the Rigidbody non-kinematic
                rb.detectCollisions = true; // Ensure collision detection is enabled
            }
            else
            {
                // Registrar error si falta la referencia al Rigidbody.
                Debug.LogError("rb is null in ReactivateComponents method of RigidBodContoller");
            }
        }
        catch (System.Exception ex)
        {
            // Capturar y registrar cualquier excepción que ocurra al reactivar componentes.
            Debug.LogError("Exception in ReactivateComponents method of RigidBodContoller: " + ex.Message);
        }
    }
}
