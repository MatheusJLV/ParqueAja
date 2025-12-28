using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/*
 * ExhibicionScript:
 * Gestiona una exhibición individual en el parque temático VR, permitiendo
 * cargar y eliminar objetos instanciados desde prefabs, almacenar posiciones
 * y rotaciones, y controlar el estado de físicas y elementos de pausa
 * basado en la proximidad del jugador.
 */

public class ExhibicionScript : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> objetosContenidos; // Lista de objetos de juego contenidos en la exhibición

    [SerializeField]
    private List<GameObject> prefabsExhibicion; // Lista de prefabs para instanciar en la exhibición

    [SerializeField]
    private List<GameObject> elementosPausa; // Lista de objetos de juego a pausar/activar

    [SerializeField]
    private List<GameObject> objetosContenidosParents; // Lista de objetos padre que contienen hijos

    [SerializeField]
    private List<GameObject> prefabsExhibicionParents; // Lista de prefabs padre para instanciar

    [SerializeField]
    private float escala = 1f; // Factor de escala para objetos instanciados, valor por defecto 1

    private List<Vector3> storedPositions = new List<Vector3>(); // Lista para almacenar posiciones de objetos
    private List<Quaternion> storedRotations = new List<Quaternion>(); // Lista para almacenar rotaciones de objetos

    private List<Vector3> storedPositionsParents = new List<Vector3>(); // Lista para almacenar posiciones de padres
    private List<Quaternion> storedRotationsParents = new List<Quaternion>(); // Lista para almacenar rotaciones de padres

    void Start()
    {
        // Verificar que objetosContenidos y prefabsExhibicion tengan el mismo tamaño
        if (objetosContenidos.Count != prefabsExhibicion.Count)
        {
            Debug.LogWarning("objetosContenidos and prefabsExhibicion are not the same size.");
        }

        // Verificar que objetosContenidosParents y prefabsExhibicionParents tengan el mismo tamaño
        if (objetosContenidosParents.Count != prefabsExhibicionParents.Count)
        {
            Debug.LogWarning("objetosContenidosParents and prefabsExhibicionParents are not the same size.");
        }

        // Almacenar posiciones y rotaciones de objetos en objetosContenidos
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                storedPositions.Add(obj.transform.position);
                storedRotations.Add(obj.transform.rotation);
            }
        }

        // Almacenar posiciones y rotaciones de objetos padre en objetosContenidosParents
        foreach (GameObject parent in objetosContenidosParents)
        {
            if (parent != null)
            {
                storedPositionsParents.Add(parent.transform.position);
                storedRotationsParents.Add(parent.transform.rotation);
            }
        }

        // Llamar a SuspensionExhibicion para suspender la exhibición al inicio
        SuspensionExhibicion();
    }


    public void Eliminar()
    {
        // Destruir todos los objetos en objetosContenidos
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        objetosContenidos.Clear();

        // Destruir todos los objetos padre en objetosContenidosParents
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

        // Instanciar prefabs en posiciones y rotaciones almacenadas
        for (int i = 0; i < prefabsExhibicion.Count; i++)
        {
            if (index < storedPositions.Count && index < storedRotations.Count)
            {
                GameObject prefab = prefabsExhibicion[i];
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, storedPositions[index], storedRotations[index]);
                    instance.transform.localScale *= escala; // Escalar la instancia por el factor especificado
                    instance.transform.SetParent(this.transform); 
                    objetosContenidos.Add(instance);
                    Debug.Log("Object  instanciado: " + instance.name);
                    index++;
                }
            }
        }

        // Instanciar prefabs padre en posiciones y rotaciones almacenadas
        for (int i = 0; i < prefabsExhibicionParents.Count; i++)
        {
            if (i < storedPositionsParents.Count && i < storedRotationsParents.Count)
            {
                GameObject parentPrefab = prefabsExhibicionParents[i];
                if (parentPrefab != null)
                {
                    GameObject parentInstance = Instantiate(parentPrefab, storedPositionsParents[i], storedRotationsParents[i]);
                    parentInstance.transform.localScale *= escala; // Escalar la instancia por el factor especificado
                    parentInstance.transform.SetParent(this.transform); // <- Add this line
                    objetosContenidosParents.Add(parentInstance);
                    Debug.Log("Parent  instanciado: " + parentInstance.name);
                }
            }
        }
    }

    public void ResetExhibicion()
    {
        // Eliminar y luego cargar la exhibición
        Eliminar();
        Cargar();
    }

    public void ReactivacionExhibicion()
    {
        // Activar físicas en objetos contenidos
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                // Habilitar componentes Rigidbody
                Rigidbody[] rigidbodies = obj.GetComponents<Rigidbody>();
                foreach (Rigidbody rb in rigidbodies)
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;
                }
            }
        }

        // Activar físicas en hijos de objetos padre
        foreach (GameObject parent in objetosContenidosParents)
        {
            if (parent != null)
            {
                foreach (Transform child in parent.transform)
                {
                    // Habilitar componentes Rigidbody
                    Rigidbody[] rigidbodies = child.GetComponents<Rigidbody>();
                    foreach (Rigidbody rb in rigidbodies)
                    {
                        rb.isKinematic = false;
                        rb.detectCollisions = true;
                    }
                }
            }
        }

        // Activar elementos de pausa
        foreach (GameObject obj in elementosPausa)
        {
            if (obj != null)
            {
                obj.SetActive(true); // Activar el objeto de juego
            }
        }
    }

    public void SuspensionExhibicion()
    {
        // Suspender físicas en objetos contenidos, si no están siendo agarrados
        foreach (GameObject obj in objetosContenidos)
        {
            if (obj != null)
            {
                XRGrabInteractable grabInteractable = obj.GetComponent<XRGrabInteractable>();
                if (grabInteractable == null || !grabInteractable.isSelected)
                {
                    // Deshabilitar componentes Rigidbody
                    Rigidbody[] rigidbodies = obj.GetComponents<Rigidbody>();
                    foreach (Rigidbody rb in rigidbodies)
                    {
                        rb.isKinematic = true;
                        rb.detectCollisions = false;
                    }
                }
            }
        }

        // Suspender físicas en hijos de objetos padre, si no están siendo agarrados
        foreach (GameObject parent in objetosContenidosParents)
        {
            if (parent != null)
            {
                foreach (Transform child in parent.transform)
                {
                    XRGrabInteractable grabInteractable = child.GetComponent<XRGrabInteractable>();
                    if (grabInteractable == null || !grabInteractable.isSelected)
                    {
                        // Deshabilitar componentes Rigidbody
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

        // Desactivar elementos de pausa
        foreach (GameObject obj in elementosPausa)
        {
            if (obj != null)
            {
                obj.SetActive(false); // Desactivar el objeto de juego
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Reactivar exhibición si el collider es el Player
        if (other.CompareTag("Player"))
        {
            ReactivacionExhibicion();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Suspender exhibición si el collider es el Player
        if (other.CompareTag("Player"))
        {
            SuspensionExhibicion();
        }
    }
}
