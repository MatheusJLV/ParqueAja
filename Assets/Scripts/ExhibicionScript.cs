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
    private float escala = 1f; // Scale factor for instantiated objects, default value is 1

    private List<Vector3> storedPositions = new List<Vector3>(); // List to store positions
    private List<Quaternion> storedRotations = new List<Quaternion>(); // List to store rotations

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Store the positions and rotations of the game objects in objetosContenidos
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                storedPositions.Add(obj.transform.position);
                storedRotations.Add(obj.transform.rotation);
            }
        }

        // Call SuspensionExhibicion to suspend the exhibition at the start
        SuspensionExhibicion();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Method to delete all game objects in objetosContenidos
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
    }

    // Method to load prefabs using the stored positions and rotations
    public void Cargar()
    {
        for (int i = 0; i < prefabsExhibicion.Count; i++)
        {
            if (i < storedPositions.Count && i < storedRotations.Count)
            {
                GameObject prefab = prefabsExhibicion[i];
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, storedPositions[i], storedRotations[i]);
                    instance.transform.localScale *= escala; // Scale the instance by the specified scale factor
                    objetosContenidos.Add(instance);
                }
            }
        }
    }

    // Method to reset the exhibition by calling Eliminar and then Cargar
    public void ResetExhibicion()
    {
        Eliminar();
        Cargar();
    }

    // Method to reactivate the exhibition when another collider enters the trigger collider
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

        foreach (GameObject obj in elementosPausa)
        {
            if (obj != null)
            {
                obj.SetActive(true); // Activate the game object
            }
        }
    }

    // Method to suspend the exhibition when another collider exits the trigger collider
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

        foreach (GameObject obj in elementosPausa)
        {
            if (obj != null)
            {
                obj.SetActive(false); // Deactivate the game object
            }
        }
    }

    // Method called when another collider enters the trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Check if the entering object is tagged as "Player"
        {
            ReactivacionExhibicion();
        }
    }

    // Method called when another collider exits the trigger collider
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) // Check if the exiting object is tagged as "Player"
        {
            SuspensionExhibicion();
        }
    }
}
