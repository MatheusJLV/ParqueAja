using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Linq;

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

        // Validate move
        if (!IsValidMove(donut, stack))
        {
            // Cancel placement
            if (args.interactorObject is XRSocketInteractor socket && socket.interactionManager != null)
            {
                socket.interactionManager.SelectExit(socket, interactable);
            }

            // Optionally push donut back up a bit
            Rigidbody rb = donut.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(Vector3.up * 3f, ForceMode.Impulse);
            }

            return;
        }

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
        yield return null; // allow any current interactions to settle this frame

        IXRSelectInteractable interactable = donut.GetComponent<IXRSelectInteractable>();
        if (interactable == null)
        {
            Debug.LogWarning("Interactable not found on donut.");
            yield break;
        }

        // 1) Detach from current (top) socket
        XRSocketInteractor currentTopSocket = stack[stack.Count - 1];
        var manager = currentTopSocket.interactionManager;
        if (manager != null)
        {
            manager.SelectExit(currentTopSocket, interactable);
        }

        // 2) Stop physics immediately and clear velocities
        Rigidbody rb = donut.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Make kinematic to prevent physics from moving the object while we snap it
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3) Optionally disable the target socket collider to avoid any collision response
        Collider targetCollider = null;
        bool disabledTargetCollider = false;
        if (targetSocket != null)
        {
            targetCollider = targetSocket.GetComponent<Collider>();
            if (targetCollider != null)
            {
                targetCollider.enabled = false;
                disabledTargetCollider = true;
            }
        }

        // 4) Position/rotate the donut to the socket's attach transform if present (more precise)
        Transform attach = targetSocket.attachTransform != null ? targetSocket.attachTransform : targetSocket.transform;
        donut.transform.SetPositionAndRotation(attach.position, attach.rotation);

        // Wait a fixed update so physics state updates cleanly before selecting
        yield return new WaitForFixedUpdate();

        // 5) Force attach to new lower socket via interaction manager
        if (manager != null)
        {
            manager.SelectEnter(targetSocket, interactable);
        }

        // Wait a frame so the XR system finalizes attachment
        yield return null;

        // 6) Re-enable the target collider (if we disabled it)
        if (disabledTargetCollider && targetCollider != null)
        {
            // Wait one more fixed update to avoid immediate re-collision
            yield return new WaitForFixedUpdate();
            targetCollider.enabled = true;
        }

        // 7) Keep it kinematic while seated in socket (recommended) OR
        //     set it dynamic if you prefer stacked objects to be simulated.
        if (rb != null)
        {
            // Keep kinematic while in a socket so it doesn't jitter
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 8) Refresh stack state and grabbability
        EnableOnlyTopSocket(stack);
        UpdateGrabbableState(stack);

        Debug.Log($"Donut moved to {targetSocket.name}. Stack updated.");
    }


    private void UpdateGrabbableState(List<XRSocketInteractor> stack)
    {
        bool topmostFound = false;

        // From top (6) to bottom (0)
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            XRSocketInteractor socket = stack[i];
            if (socket.hasSelection)
            {
                var interactable = socket.GetOldestInteractableSelected();

                if (interactable is XRGrabInteractable grab)
                {
                    // Allow grabbing only the topmost donut
                    grab.interactionLayers = topmostFound
                        ? InteractionLayerMask.GetMask("") // NOT interactable
                        : InteractionLayerMask.GetMask("Default"); // Interactable

                    topmostFound = true;
                    Debug.Log($"{grab.name} -> Layer: {grab.interactionLayers}, Selected: {grab.isSelected}, isKinematic: {grab.GetComponent<Rigidbody>().isKinematic}");

                }
            }
        }
    }

    public void ExitAttempt(GameObject donut)
    {
        Debug.Log($"ExitAttempt: {donut.name} attempting to exit.");

        if (stackA.Exists(s => s.hasSelection && s.GetOldestInteractableSelected()?.transform?.gameObject == donut))
        {
            PopTopDonut(stackA);
        }
        else if (stackB.Exists(s => s.hasSelection && s.GetOldestInteractableSelected()?.transform?.gameObject == donut))
        {
            PopTopDonut(stackB);
        }
        else if (stackC.Exists(s => s.hasSelection && s.GetOldestInteractableSelected()?.transform?.gameObject == donut))
        {
            PopTopDonut(stackC);
        }
        else
        {
            Debug.LogWarning($"ExitAttempt: {donut.name} was not found in any stack.");
        }
    }


    public void PopTopDonut(List<XRSocketInteractor> stack)
    {
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            var socket = stack[i];
            if (socket.hasSelection)
            {
                IXRSelectInteractable interactable = socket.GetOldestInteractableSelected();
                if (interactable is XRGrabInteractable grab)
                {
                    Rigidbody rb = grab.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Get top socket (which may block the exit path)
                        XRSocketInteractor topSocket = stack[stack.Count - 1];

                        // Start popping sequence with controlled delay
                        StartCoroutine(DelayedPop(rb, socket, interactable, topSocket, stack));
                    }
                    break;
                }
            }
        }
    }
    private IEnumerator DelayedPop(Rigidbody rb, XRSocketInteractor fromSocket, IXRSelectInteractable interactable, XRSocketInteractor topSocket, List<XRSocketInteractor> stack)
    {
        // 1. Disable top socket to clear path
        topSocket.enabled = false;

        // 2. Wait a short moment to ensure it deactivates before applying force
        yield return new WaitForSeconds(0.1f);

        // 3. Force exit from current socket
        if (fromSocket.interactionManager != null)
        {
            fromSocket.interactionManager.SelectExit(fromSocket, interactable);
        }

        // 4. Make sure physics is ready
        rb.isKinematic = false;

        // 5. Apply popping force
        Vector3 popDirection = Vector3.up * 6f + Random.insideUnitSphere * 1.5f;
        rb.AddForce(popDirection, ForceMode.Impulse);

        // 6. Re-enable top socket after full delay
        yield return new WaitForSeconds(1.2f);
        topSocket.enabled = true;

        // 7. Refresh the stack (in case structure changed)
        EnableOnlyTopSocket(stack);

        yield return new WaitForSeconds(1.2f);

        UpdateGrabbableState(stack);
    }

    private int ExtractNumberFromName(string name)
    {
        string numberStr = "";
        foreach (char c in name)
        {
            if (char.IsDigit(c))
                numberStr += c;
        }

        if (int.TryParse(numberStr, out int value))
        {
            Debug.Log("ExtractNumberFromName: " + name + " -> " + value);
            return value;
        }

        Debug.LogWarning("ExtractNumberFromName: No number found in name: " + name);
        return -1; // No valid number
    }

    private bool IsValidMove(GameObject incomingDonut, List<XRSocketInteractor> stack)
    {
        int incomingValue = ExtractNumberFromName(incomingDonut.name);
        if (incomingValue == -1)
        {
            Debug.LogWarning("IsValidMove: Could not determine size for " + incomingDonut.name + ", allowing move.");
            return true; // Fail-safe
        }

        Debug.Log("IsValidMove: Checking if " + incomingDonut.name + " (size " + incomingValue + ") can be placed on this stack...");

        // Look for the first occupied socket starting from the bottom, not including the one holding the incoming donut
        for (int i = 0; i < stack.Count; i++)
        {
            if (stack[i].hasSelection)
            {
                GameObject topDonut = stack[i].GetOldestInteractableSelected().transform.gameObject;

                // Ignore the incoming donut itself
                if (topDonut == incomingDonut)
                    continue;

                int topValue = ExtractNumberFromName(topDonut.name);
                Debug.Log("IsValidMove: Top donut in stack is " + topDonut.name + " (size " + topValue + ")");

                if (topValue != -1 && incomingValue > topValue)
                {
                    Debug.Log("Invalid move: " + incomingDonut.name + " (size " + incomingValue + ") is larger than " + topDonut.name + " (size " + topValue + ")");
                    return false;
                }
                else
                {
                    Debug.Log("Valid move: " + incomingDonut.name + " can be placed on top of " + topDonut.name);
                }

                break; // Found the first real donut to compare against
            }
        }

        Debug.Log("Valid move: " + incomingDonut.name + " can be placed on an empty stack.");
        return true;
    }


}

