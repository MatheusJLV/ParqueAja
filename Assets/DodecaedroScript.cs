using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

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

    // Called by XRSocketInteractor's OnSelectEntered event
    public void OnPinInserted(SelectEnterEventArgs args)
    {
        GameObject pin = args.interactableObject.transform.gameObject;
        Transform anchor = args.interactorObject.transform; // the socket

        AddPin(pin, anchor);
    }

    public void AddPin(GameObject pin, Transform anchor)
    {
        // Reset material on previous
        if (placedPins.Last != null)
        {
            SetMaterial(placedPins.Last.Value.pinObject, normalMaterial);
        }

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

        // Optional: check for line crossings or other rules
        // if (LineCrossesOthers(newPinData)) { UndoLastPin(); }
    }

    private void SetMaterial(GameObject pin, Material mat)
    {
        Renderer rend = pin.GetComponentInChildren<Renderer>();
        if (rend != null)
            rend.material = mat;
    }

    public void OnRemovePin(SelectExitEventArgs args)
    {
        GameObject pin = args.interactableObject.transform.gameObject;
        SetMaterial(pin,normalMaterial);
        RemovePin(pin); // Calls the existing logic
    }
    public void RemovePin(GameObject pin)
    {
        LinkedListNode<PinData> node = placedPins.First;

        while (node != null)
        {
            if (node.Value.pinObject == pin)
            {
                RemoveFromNode(node);
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


    // Placeholder for line intersection logic
    private bool LineCrossesOthers(PinData newPin)
    {
        // You could do a 2D projection and check for line-line intersection here
        return false;
    }
}
