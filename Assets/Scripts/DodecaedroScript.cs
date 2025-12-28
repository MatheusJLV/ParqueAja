using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.UI;

/*
 * DodecaedroScript:
 * Gestiona la colocación de pines en un dodecaedro, conectándolos con líneas,
 * reproduciendo audio y manejando la lógica de inmersión.
 */

public class DodecaedroScript : MonoBehaviour
{
    [Header("Materials")]
    public Material normalMaterial; // Material normal para los pines
    public Material lastPlacedMaterial; // Material para resaltar el último pin colocado

    [Header("Line Settings")]
    public GameObject linePrefab; // Prefab con LineRenderer para las líneas

    public LinkedList<PinData> placedPins = new LinkedList<PinData>(); // Lista enlazada de pines colocados

    [Header("Parenting")]
    public Transform dodecahedronRoot; // Raíz del dodecaedro para parenting

    [Header("Immersion UI")]
    public Button immersionUIButton; // Botón de UI para inmersión

    //  simple audio plumbing for hook/unhook
    [Header("Audio (Hook/Unhook)")]
    public AudioClip hookClip;    // Clip de audio al insertar pin
    public AudioClip unhookClip;  // Clip de audio al remover pin
    [Range(0f, 1f)] public float hookVolume = 1f; // Volumen para hook
    [Range(0f, 1f)] public float unhookVolume = 1f; // Volumen para unhook
    [Tooltip("If null, we'll reuse/add an AudioSource on this GameObject.")]
    public AudioSource audioSource; // Fuente de audio, se crea si es null
    [Tooltip("3D mix defaults if we autocreate an AudioSource.")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // Mezcla espacial para audio 3D
    public float minDistance = 0.35f; // Distancia mínima para audio
    public float maxDistance = 8f; // Distancia máxima para audio

    void Awake()
    {
        // Asegurar que hay una fuente de audio al iniciar
        EnsureAudioSource();
    }

    private void EnsureAudioSource()
    {
        // Crear o reutilizar AudioSource si es necesario
        if (audioSource != null) return; // Ya asignado, salir

        audioSource = GetComponent<AudioSource>(); // Intentar obtener del componente
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>(); // Crear si no existe

        // Configurar para audio 3D
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;   // 3D por defecto
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
    }

    private void PlayOneShotSafe(AudioClip clip, float volume)
    {
        // Reproducir clip de audio de forma segura
        if (clip == null || audioSource == null) return; // Verificar que existan
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume)); // Reproducir con volumen clamped
    }
    // 

    private void ValidateImmersionButton()
    {
        // Habilitar botón de inmersión si hay más de un pin colocado
        if (immersionUIButton != null)
            immersionUIButton.interactable = placedPins.Count > 1;
    }

    // Called by XRSocketInteractor's OnSelectEntered event
    public void OnPinInserted(SelectEnterEventArgs args)
    {
        // Manejar inserción de pin en socket
        GameObject pin = args.interactableObject.transform.gameObject;
        Transform anchor = args.interactorObject.transform; // the socket

        AddPin(pin, anchor);
        ValidateImmersionButton();

        // play hook sound
        PlayOneShotSafe(hookClip, hookVolume);
    }

    public void AddPin(GameObject pin, Transform anchor)
    {
        // Agregar pin a la lista y conectar con línea si es necesario
        // Reset material on previous
        if (placedPins.Last != null)
            SetMaterial(placedPins.Last.Value.pinObject, normalMaterial); // Resetear material del anterior

        PinData newPinData = new PinData(pin, anchor); // Crear datos del nuevo pin

        // Connect to previous with a line
        if (placedPins.Count > 0) // Si ya hay pines, conectar con línea
        {
            GameObject lineGO = Instantiate(linePrefab, dodecahedronRoot); // Instanciar línea

            LineRenderer line = lineGO.GetComponent<LineRenderer>();
            line.useWorldSpace = false; // Usar espacio local

            var prevPin = placedPins.Last.Value; // Obtener pin anterior
            line.positionCount = 2; // Dos puntos para la línea
            line.SetPosition(0, dodecahedronRoot.InverseTransformPoint(prevPin.pinObject.transform.position)); // Posición inicial
            line.SetPosition(1, dodecahedronRoot.InverseTransformPoint(anchor.transform.position)); // Posición final

            newPinData.lineFromPrevious = line; // Asignar línea al pin
        }

        placedPins.AddLast(newPinData); // Agregar a la lista

        // Highlight this pin
        SetMaterial(pin, lastPlacedMaterial); // Resaltar el nuevo pin
    }

    private void SetMaterial(GameObject pin, Material mat)
    {
        // Cambiar material del pin
        Renderer rend = pin.GetComponentInChildren<Renderer>();
        if (rend != null) rend.material = mat;
    }

    // Called by XRSocketInteractors OnSelectExited event
    public void OnRemovePin(SelectExitEventArgs args)
    {
        // Manejar remoción de pin de socket
        GameObject pin = args.interactableObject.transform.gameObject;
        SetMaterial(pin, normalMaterial);
        RemovePin(pin); // Calls existing logic

        // play unhook sound
        PlayOneShotSafe(unhookClip, unhookVolume);
    }

    public void RemovePin(GameObject pin)
    {
        // Buscar y remover pin de la lista
        LinkedListNode<PinData> node = placedPins.First; // Empezar desde el primero

        while (node != null) // Recorrer la lista
        {
            if (node.Value.pinObject == pin) // Encontrar el pin
            {
                RemoveFromNode(node); // Remover desde este nodo
                ValidateImmersionButton(); // Actualizar botón
                return; // Salir
            }
            node = node.Next; // Siguiente nodo
        }
    }


    /*private void RemoveFromNode(LinkedListNode<PinData> startNode)
    {
        LinkedListNode<PinData> node = startNode;

        while (node != null)
        {
            // 1. Destroy line
            if (node.Value.lineFromPrevious != null)
            {
                Destroy(node.Value.lineFromPrevious.gameObject);
            }

            // 2. Try to release from socket
            if (node.Value.anchor.TryGetComponent(out XRSocketInteractor socket))
            {
                if (socket.hasSelection)
                {
                    var selected = socket.firstInteractableSelected;
                    socket.interactionManager.SelectExit(socket, selected);
                }
            }

            // 3. Drop the pin
            GameObject pin = node.Value.pinObject;
            pin.transform.SetParent(null); // Unparent from dodecahedron
            if (pin.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // 4. Remove from list
            var next = node.Next;
            placedPins.Remove(node);
            node = next;
        }

        // 5. Reset highlight on new last pin
        if (placedPins.Last != null)
        {
            SetMaterial(placedPins.Last.Value.pinObject, lastPlacedMaterial);
        }
    }*/

    private void RemoveFromNode(LinkedListNode<PinData> startNode)
    {
        // Remover nodos desde el inicio, destruyendo líneas y liberando pines
        LinkedListNode<PinData> node = startNode;

        while (node != null)
        {
            // 1. Destroy line
            if (node.Value.lineFromPrevious != null)
            {
                Destroy(node.Value.lineFromPrevious.gameObject); // Destruir línea conectada
            }

            // 2. Try to release from socket
            if (node.Value.anchor.TryGetComponent(out XRSocketInteractor socket))
            {
                if (socket.hasSelection) // Si hay selección
                {
                    var selected = socket.firstInteractableSelected; // Obtener seleccionado
                    socket.interactionManager.SelectExit(socket, selected); // Liberar selección
                }

                // 3. Drop the pin
                GameObject pin = node.Value.pinObject;
                pin.transform.SetParent(null); // Desparentar del dodecaedro

                if (pin.TryGetComponent(out Rigidbody rb)) // Si tiene Rigidbody
                {
                    rb.isKinematic = false; // Permitir física
                    rb.useGravity = true; // Activar gravedad

                    // Apply a small force away from socket
                    Vector3 awayFromSocket = (pin.transform.position - socket.transform.position).normalized; // Dirección alejada
                    rb.AddForce(awayFromSocket * 0.015f, ForceMode.Impulse); // Aplicar impulso
                }

                // Temporarily disable the socket to avoid resocketing
                StartCoroutine(TemporarilyDisableSocket(socket, 0.5f)); // Deshabilitar temporalmente
            }

            // 4. Remove from list
            var next = node.Next; // Guardar siguiente
            placedPins.Remove(node); // Remover de lista
            node = next; // Avanzar
        }

        // 5. Reset highlight on new last pin
        if (placedPins.Last != null)
        {
            SetMaterial(placedPins.Last.Value.pinObject, lastPlacedMaterial); // Resaltar nuevo último pin
        }
    }



    private IEnumerator TemporarilyDisableSocket(XRSocketInteractor socket, float delay)
    {
        // Deshabilitar socket temporalmente para evitar re-socketing
        Collider col = socket.GetComponent<Collider>(); // Obtener collider
        if (col != null) col.enabled = false; // Deshabilitar collider
        socket.enabled = false; // Deshabilitar socket

        yield return new WaitForSeconds(delay); // Esperar delay

        if (col != null) col.enabled = true; // Rehabilitar collider
        socket.enabled = true; // Rehabilitar socket
    }

    public void RemoveFirstPin()
    {
        // Remover el primer pin de la lista
        if (placedPins.First != null)
        {
            RemoveFromNode(placedPins.First);
        }
    }



    // Placeholder for line intersection logic
    private bool LineCrossesOthers(PinData newPin)
    {
        // Podrías hacer una proyección 2D y verificar intersección de líneas aquí
        return false;
    }
}
