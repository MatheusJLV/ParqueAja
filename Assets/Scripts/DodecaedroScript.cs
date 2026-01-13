using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.UI;

// Gestiona la colocación de pins en un dodecaedro con seguimiento de conexiones y audio
public class DodecaedroScript : MonoBehaviour
{
    // Materiales visuales para pins: normal y resaltado (último colocado)
    [Header("Materials")]
    public Material normalMaterial;
    public Material lastPlacedMaterial;

    // Configuración de visualización de líneas que conectan pins consecutivos
    [Header("Line Settings")]
    public GameObject linePrefab; // Prefab with a LineRenderer

    // Lista enlazada de pins colocados, mantiene orden de inserción
    public LinkedList<PinData> placedPins = new LinkedList<PinData>();

    // Referencia al transform raíz del dodecaedro para jerarquía de objetos
    [Header("Parenting")]
    public Transform dodecahedronRoot;

    // Botón UI que se activa cuando hay suficientes pins colocados
    [Header("Immersion UI")]
    public Button immersionUIButton;

    // Configuración de audio: efectos de sonido para inserción y extracción de pins
    //  simple audio plumbing for hook/unhook
    [Header("Audio (Hook/Unhook)")]
    public AudioClip hookClip;    // plays on OnPinInserted
    public AudioClip unhookClip;  // plays on OnRemovePin
    [Range(0f, 1f)] public float hookVolume = 1f;
    [Range(0f, 1f)] public float unhookVolume = 1f;
    [Tooltip("If null, we'll reuse/add an AudioSource on this GameObject.")]
    public AudioSource audioSource;
    [Tooltip("3D mix defaults if we autocreate an AudioSource.")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 0.35f;
    public float maxDistance = 8f;

    // Inicialización: asegura que exista una fuente de audio
    void Awake()
    {
        EnsureAudioSource();
    }

    // Crea o configura una fuente de audio para reproducir efectos de sonido
    private void EnsureAudioSource()
    {
        if (audioSource != null) return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;   // 3D by default
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
    }

    // Reproduce un clip de audio de manera segura (verifica null)
    private void PlayOneShotSafe(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
    // 

    // Valida si el botón de inmersión puede ser activado (requiere al menos 2 pins)
    private void ValidateImmersionButton()
    {
        if (immersionUIButton != null)
            immersionUIButton.interactable = placedPins.Count > 1;
    }

    // Manejador de evento XRSocketInteractor: cuando se inserta un pin en un socket
    // Called by XRSocketInteractor's OnSelectEntered event
    public void OnPinInserted(SelectEnterEventArgs args)
    {
        GameObject pin = args.interactableObject.transform.gameObject;
        Transform anchor = args.interactorObject.transform; // the socket

        AddPin(pin, anchor);
        ValidateImmersionButton();

        // play hook sound
        PlayOneShotSafe(hookClip, hookVolume);
    }

    // Añade un nuevo pin a la lista, conectado con línea al pin anterior (si existe)
    // Destaca el nuevo pin con material especial
    public void AddPin(GameObject pin, Transform anchor)
    {
        // Reset material on previous
        if (placedPins.Last != null)
            SetMaterial(placedPins.Last.Value.pinObject, normalMaterial);

        PinData newPinData = new PinData(pin, anchor);

        // Connect to previous with a line
        if (placedPins.Count > 0)
        {
            GameObject lineGO = Instantiate(linePrefab, dodecahedronRoot);

            LineRenderer line = lineGO.GetComponent<LineRenderer>();
            line.useWorldSpace = false;

            var prevPin = placedPins.Last.Value;
            line.positionCount = 2;
            line.SetPosition(0, dodecahedronRoot.InverseTransformPoint(prevPin.pinObject.transform.position));
            line.SetPosition(1, dodecahedronRoot.InverseTransformPoint(anchor.transform.position));

            newPinData.lineFromPrevious = line;
        }

        placedPins.AddLast(newPinData);

        // Resalta este nuevo pin como el último colocado
        SetMaterial(pin, lastPlacedMaterial);
    }

    // Aplica material a un pin usando su Renderer hijo
    private void SetMaterial(GameObject pin, Material mat)
    {
        Renderer rend = pin.GetComponentInChildren<Renderer>();
        if (rend != null) rend.material = mat;
    }

    // Manejador de evento XRSocketInteractor: cuando se retira un pin del socket
    // Called by XRSocketInteractors OnSelectExited event
    public void OnRemovePin(SelectExitEventArgs args)
    {
        GameObject pin = args.interactableObject.transform.gameObject;
        SetMaterial(pin, normalMaterial);
        RemovePin(pin); // Calls existing logic

        // play unhook sound
        PlayOneShotSafe(unhookClip, unhookVolume);
    }

    // Busca y elimina un pin de la lista
    public void RemovePin(GameObject pin)
    {
        LinkedListNode<PinData> node = placedPins.First;

        while (node != null)
        {
            if (node.Value.pinObject == pin)
            {
                RemoveFromNode(node);
                ValidateImmersionButton();
                return;
            }
            node = node.Next;
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

    // Elimina un nodo y todos los posteriores: destruye líneas, libera pins y desactiva sockets
    // Esto mantiene consistencia: no puedes tener huecos en la cadena
    private void RemoveFromNode(LinkedListNode<PinData> startNode)
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

                // 3. Drop the pin
                // Libera el pin del dodecaedro y restaura física (gravedad)
                GameObject pin = node.Value.pinObject;
                pin.transform.SetParent(null); // Unparent from dodecahedron

                if (pin.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    // Aplica pequeño impulso para alejar el pin del socket
                    Vector3 awayFromSocket = (pin.transform.position - socket.transform.position).normalized;
                    rb.AddForce(awayFromSocket * 0.015f, ForceMode.Impulse);
                }

                // Temporarily disable the socket to avoid resocketing
                StartCoroutine(TemporarilyDisableSocket(socket, 0.5f));
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
    }



    // Desactiva temporalmente un socket para evitar que el pin se resockete inmediatamente
    private IEnumerator TemporarilyDisableSocket(XRSocketInteractor socket, float delay)
    {
        Collider col = socket.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        socket.enabled = false;

        yield return new WaitForSeconds(delay);

        if (col != null) col.enabled = true;
        socket.enabled = true;
    }

    // Conveniencia: elimina el primer pin de la secuencia
    public void RemoveFirstPin()
    {
        if (placedPins.First != null)
        {
            RemoveFromNode(placedPins.First);
        }
    }



    // Placeholder for line intersection logic
    // Verifica si una línea nueva cruza con otras líneas existentes (placeholder)
    private bool LineCrossesOthers(PinData newPin)
    {
        // You could do a 2D projection and check for line-line intersection here
        return false;
    }
}
