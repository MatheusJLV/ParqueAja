using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ExhibicionScript : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> objetosContenidos; // List of contained game objects

    [SerializeField]
    private List<GameObject> prefabsExhibicion; // List of exhibition prefabs

    [SerializeField]
    private List<GameObject> elementosPausa; // List of game objects to pause

    [SerializeField]
    private List<GameObject> objetosContenidosParents; // List of parent game objects containing children

    [SerializeField]
    private List<GameObject> prefabsExhibicionParents; // List of parent prefabs containing children

    [SerializeField]
    private float escala = 1f; // Scale factor for instantiated objects, default value is 1

    private List<Vector3> storedPositions = new List<Vector3>(); // List to store positions
    private List<Quaternion> storedRotations = new List<Quaternion>(); // List to store rotations

    private List<Vector3> storedPositionsParents = new List<Vector3>(); // List to store parent positions
    private List<Quaternion> storedRotationsParents = new List<Quaternion>(); // List to store parent rotations

    /*// NUEVAS VARIABLES
    public Inmersible inmersible;
    public InmersiveManager inmersiveManager;*/

    void Start()
    {
        // Check if objetosContenidos and prefabsExhibicion are the same size
        if (objetosContenidos.Count != prefabsExhibicion.Count)
        {
            Debug.LogWarning("objetosContenidos and prefabsExhibicion are not the same size.");
        }

        // Check if objetosContenidosParents and prefabsExhibicionParents are the same size
        if (objetosContenidosParents.Count != prefabsExhibicionParents.Count)
        {
            Debug.LogWarning("objetosContenidosParents and prefabsExhibicionParents are not the same size.");
        }

        // Store the positions and rotations of the game objects in objetosContenidos
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                storedPositions.Add(obj.transform.position);
                storedRotations.Add(obj.transform.rotation);
            }
        }

        // Store the positions and rotations of the parent game objects in objetosContenidosParents
        foreach (GameObject parent in objetosContenidosParents)
        {
            if (parent != null)
            {
                storedPositionsParents.Add(parent.transform.position);
                storedRotationsParents.Add(parent.transform.rotation);
            }
        }

        // Call SuspensionExhibicion to suspend the exhibition at the start
        SuspensionExhibicion();
    }

    /*// NUEVOS MÉTODOS
    public void SetInmersivo()
    {
        if (inmersiveManager != null)
        {
            inmersiveManager.inmersible = inmersible;
        }
    }

    public void UnsetInmersivo()
    {
        if (inmersiveManager != null)
        {
            inmersiveManager.inmersible = null;
        }
    }*/

    public void Eliminar()
    {
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        objetosContenidos.Clear();

        foreach (GameObject parent in objetosContenidosParents)
        {
            if (parent != null)
            {
                Destroy(parent);
            }
        }
        objetosContenidosParents.Clear();
    }

    public void Cargar()
    {
        int index = 0;

        for (int i = 0; i < prefabsExhibicion.Count; i++)
        {
            if (index < storedPositions.Count && index < storedRotations.Count)
            {
                GameObject prefab = prefabsExhibicion[i];
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, storedPositions[index], storedRotations[index]);
                    instance.transform.localScale *= escala; // Scale the instance by the specified scale factor
                    objetosContenidos.Add(instance);
                    index++;
                }
            }
        }

        for (int i = 0; i < prefabsExhibicionParents.Count; i++)
        {
            if (i < storedPositionsParents.Count && i < storedRotationsParents.Count)
            {
                GameObject parentPrefab = prefabsExhibicionParents[i];
                if (parentPrefab != null)
                {
                    GameObject parentInstance = Instantiate(parentPrefab, storedPositionsParents[i], storedRotationsParents[i]);
                    parentInstance.transform.localScale *= escala; // Scale the instance by the specified scale factor
                    objetosContenidosParents.Add(parentInstance);
                }
            }
        }
    }

    public void ResetExhibicion()
    {
        Eliminar();
        Cargar();
    }

    public void ReactivacionExhibicion()
    {
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                // Enable Rigidbody components
                Rigidbody[] rigidbodies = obj.GetComponents<Rigidbody>();
                foreach (Rigidbody rb in rigidbodies)
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;
                }
            }
        }

        foreach (GameObject parent in objetosContenidosParents)
        {
            if (parent != null)
            {
                foreach (Transform child in parent.transform)
                {
                    // Enable Rigidbody components
                    Rigidbody[] rigidbodies = child.GetComponents<Rigidbody>();
                    foreach (Rigidbody rb in rigidbodies)
                    {
                        rb.isKinematic = false;
                        rb.detectCollisions = true;
                    }
                }
            }
        }

        foreach (GameObject obj in elementosPausa)
        {
            if (obj != null)
            {
                obj.SetActive(true); // Activate the game object
            }
        }
    }

    public void SuspensionExhibicion()
    {
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                XRGrabInteractable grabInteractable = obj.GetComponent<XRGrabInteractable>();
                if (grabInteractable == null || !grabInteractable.isSelected)
                {
                    // Disable Rigidbody components
                    Rigidbody[] rigidbodies = obj.GetComponents<Rigidbody>();
                    foreach (Rigidbody rb in rigidbodies)
                    {
                        rb.isKinematic = true;
                        rb.detectCollisions = false;
                    }
                }
            }
        }

        foreach (GameObject parent in objetosContenidosParents)
        {
            if (parent != null)
            {
                foreach (Transform child in parent.transform)
                {
                    XRGrabInteractable grabInteractable = child.GetComponent<XRGrabInteractable>();
                    if (grabInteractable == null || !grabInteractable.isSelected)
                    {
                        // Disable Rigidbody components
                        Rigidbody[] rigidbodies = child.GetComponents<Rigidbody>();
                        foreach (Rigidbody rb in rigidbodies)
                        {
                            rb.isKinematic = true;
                            rb.detectCollisions = false;
                        }
                    }
                }
            }
        }

        foreach (GameObject obj in elementosPausa)
        {
            if (obj != null)
            {
                obj.SetActive(false); // Deactivate the game object
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ReactivacionExhibicion();
            /*if (inmersiveManager != null && inmersible != null)
            {
                inmersiveManager.enabled = true; // Activar el componente primero
                SetInmersivo();
            }*/
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SuspensionExhibicion();
            /*if (inmersiveManager != null && inmersible != null)
            {
                UnsetInmersivo();
                inmersiveManager.enabled = false; // Desactivar el componente después
            }*/
        }
    }
}
