using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.UI;

public class DodecaedroScript : MonoBehaviour
{
    [Header("Materials")]
    public Material normalMaterial;
    public Material lastPlacedMaterial;

    [Header("Line Settings")]
    public GameObject linePrefab; // Prefab with a LineRenderer

    public LinkedList<PinData> placedPins = new LinkedList<PinData>();

    [Header("Parenting")]
    public Transform dodecahedronRoot;

    [Header("Immersion UI")]
    public Button immersionUIButton;

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

    void Awake()
    {
        EnsureAudioSource();
    }

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

    private void PlayOneShotSafe(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
    // 

    private void ValidateImmersionButton()
    {
        if (immersionUIButton != null)
            immersionUIButton.interactable = placedPins.Count > 1;
    }

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

        // Highlight this pin
        SetMaterial(pin, lastPlacedMaterial);
    }

    private void SetMaterial(GameObject pin, Material mat)
    {
        Renderer rend = pin.GetComponentInChildren<Renderer>();
        if (rend != null) rend.material = mat;
    }

    // Called by XRSocketInteractors OnSelectExited event
    public void OnRemovePin(SelectExitEventArgs args)
    {
        GameObject pin = args.interactableObject.transform.gameObject;
        SetMaterial(pin, normalMaterial);
        RemovePin(pin); // Calls existing logic

        // play unhook sound
        PlayOneShotSafe(unhookClip, unhookVolume);
    }

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
                GameObject pin = node.Value.pinObject;
                pin.transform.SetParent(null); // Unparent from dodecahedron

                if (pin.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    // Apply a small force away from socket
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



    private IEnumerator TemporarilyDisableSocket(XRSocketInteractor socket, float delay)
    {
        Collider col = socket.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        socket.enabled = false;

        yield return new WaitForSeconds(delay);

        if (col != null) col.enabled = true;
        socket.enabled = true;
    }

    public void RemoveFirstPin()
    {
        if (placedPins.First != null)
        {
            RemoveFromNode(placedPins.First);
        }
    }



    // Placeholder for line intersection logic
    private bool LineCrossesOthers(PinData newPin)
    {
        // You could do a 2D projection and check for line-line intersection here
        return false;
    }
}
