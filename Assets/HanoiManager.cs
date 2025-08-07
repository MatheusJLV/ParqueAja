using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HanoiManager : MonoBehaviour
{
    [Header("Stacks of 7 sockets (Top = index 6, Bottom = index 0)")]
    public List<XRSocketInteractor> stackA = new List<XRSocketInteractor>();
    public List<XRSocketInteractor> stackB = new List<XRSocketInteractor>();
    public List<XRSocketInteractor> stackC = new List<XRSocketInteractor>();

    private void Start()
    {
        EnableOnlyTopSocket(stackA);
        EnableOnlyTopSocket(stackB);
        EnableOnlyTopSocket(stackC);

        stackA[6].selectEntered.AddListener(ctx => OnTopSocketReceived(ctx, stackA));
        stackB[6].selectEntered.AddListener(ctx => OnTopSocketReceived(ctx, stackB));
        stackC[6].selectEntered.AddListener(ctx => OnTopSocketReceived(ctx, stackC));
    }

    private void OnDestroy()
    {
        stackA[6].selectEntered.RemoveAllListeners();
        stackB[6].selectEntered.RemoveAllListeners();
        stackC[6].selectEntered.RemoveAllListeners();
    }

    private void OnEnable()
    {
        EnableOnlyTopSocket(stackA);
        EnableOnlyTopSocket(stackB);
        EnableOnlyTopSocket(stackC);
    }

    private void OnDisable()
    {
        SetSocketsActive(stackA, false);
        SetSocketsActive(stackB, false);
        SetSocketsActive(stackC, false);
    }

    private void EnableOnlyTopSocket(List<XRSocketInteractor> stack)
    {
        // Disable all
        foreach (var socket in stack)
        {
            socket.enabled = false;
            socket.selectEntered.RemoveAllListeners();  // Clean up previous listeners
        }

        // Find the topmost empty socket
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            if (!stack[i].hasSelection)
            {
                XRSocketInteractor newTopSocket = stack[i];
                newTopSocket.enabled = true;

                // Attach listener again
                newTopSocket.selectEntered.AddListener(ctx => OnTopSocketReceived(ctx, stack));
                break;
            }
        }
    }



    private void SetSocketsActive(List<XRSocketInteractor> stack, bool active)
    {
        foreach (var socket in stack)
        {
            socket.enabled = active;
        }
    }

    private void OnTopSocketReceived(SelectEnterEventArgs args, List<XRSocketInteractor> stack)
    {
        IXRSelectInteractable interactable = args.interactableObject;
        GameObject donut = interactable.transform.gameObject;

        Debug.Log($"Top socket received {donut.name}, looking for lower slot...");

        XRSocketInteractor targetSlot = GetNextAvailableSlotBelow(stack);

        if (targetSlot != null)
        {
            StartCoroutine(SwapDonutToLowerSocket(donut, stack, targetSlot));
        }
        else
        {
            Debug.LogWarning("No available lower slot found.");
        }
    }

    private XRSocketInteractor GetNextAvailableSlotBelow(List<XRSocketInteractor> stack)
    {
        // Search from bottom to just below top (index 0 to 5)
        for (int i = 0; i < stack.Count - 1; i++)
        {
            if (!stack[i].hasSelection)
                return stack[i];
        }
        return null;
    }

    private IEnumerator SwapDonutToLowerSocket(GameObject donut, List<XRSocketInteractor> stack, XRSocketInteractor targetSocket)
    {
        yield return null; // Wait one frame to allow existing interactions to settle

        IXRSelectInteractable interactable = donut.GetComponent<IXRSelectInteractable>();
        if (interactable == null)
        {
            Debug.LogWarning("Interactable not found on donut.");
            yield break;
        }

        // Detach from current (top) socket
        XRSocketInteractor currentTopSocket = stack[6];
        var manager = currentTopSocket.interactionManager;
        if (manager != null)
        {
            manager.SelectExit(currentTopSocket, interactable);
        }

        // Move donut to target socket's position
        donut.transform.position = targetSocket.transform.position;
        donut.transform.rotation = targetSocket.transform.rotation;

        // Force attach to new lower socket
        if (manager != null)
        {
            manager.SelectEnter(targetSocket, interactable);
        }

        // Let the system find the next available socket and assign it as new top
        EnableOnlyTopSocket(stack);

        Debug.Log($"Donut moved to {targetSocket.name}. Stack updated.");

    }
}
