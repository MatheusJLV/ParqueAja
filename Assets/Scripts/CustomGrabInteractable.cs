using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CustomGrabInteractable : XRGrabInteractable
{
    [SerializeField] private Transform primaryAnchor;   // Left-hand attach
    [SerializeField] private Transform secondaryAnchor; // Right-hand attach

    [SerializeField] private string leftHandTag = "LeftHand";
    [SerializeField] private string rightHandTag = "RightHand";

    private readonly Dictionary<IXRInteractor, Transform> interactorAnchorMap = new();

    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        // If we already assigned an anchor on grab, keep using it
        if (interactorAnchorMap.TryGetValue(interactor, out var anchor) && anchor != null)
            return anchor;

        // If not assigned yet, decide by hand identity
        if (IsLeftHand(interactor))
            return primaryAnchor != null ? primaryAnchor : base.GetAttachTransform(interactor);

        if (IsRightHand(interactor))
            return secondaryAnchor != null ? secondaryAnchor : base.GetAttachTransform(interactor);

        // Unknown interactor fallback
        return base.GetAttachTransform(interactor);
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        base.OnSelectEntering(args);

        var interactor = args.interactorObject;

        if (IsLeftHand(interactor))
            interactorAnchorMap[interactor] = primaryAnchor != null ? primaryAnchor : base.GetAttachTransform(interactor);
        else if (IsRightHand(interactor))
            interactorAnchorMap[interactor] = secondaryAnchor != null ? secondaryAnchor : base.GetAttachTransform(interactor);
        else
            interactorAnchorMap[interactor] = base.GetAttachTransform(interactor);
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);
        interactorAnchorMap.Remove(args.interactorObject);
    }

    private bool IsLeftHand(IXRInteractor interactor) =>
        interactor.transform.CompareTag(leftHandTag);

    private bool IsRightHand(IXRInteractor interactor) =>
        interactor.transform.CompareTag(rightHandTag);
}




