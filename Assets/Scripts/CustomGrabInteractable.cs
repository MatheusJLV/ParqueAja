using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/*
 Interactable personalizado que permite definir
 puntos de agarre distintos para mano izquierda y derecha.
*/

public class CustomGrabInteractable : XRGrabInteractable
{
    // Punto de agarre para la mano izquierda
    [SerializeField] private Transform primaryAnchor;   
    // Punto de agarre para la mano derecha
    [SerializeField] private Transform secondaryAnchor; 
    // Tags usados para identificar cada mano
    [SerializeField] private string leftHandTag = "LeftHand";
    [SerializeField] private string rightHandTag = "RightHand";

    // Relación entre interactores y su punto de agarre asignado    
    private readonly Dictionary<IXRInteractor, Transform> interactorAnchorMap = new();

    // Define qué Transform se usará como punto de agarre
    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        // Si ya se asignó un anchor a este interactor, se reutiliza
        if (interactorAnchorMap.TryGetValue(interactor, out var anchor) && anchor != null)
            return anchor;

        // Selecciona anchor según la mano
        if (IsLeftHand(interactor))
            return primaryAnchor != null ? primaryAnchor : base.GetAttachTransform(interactor);

        if (IsRightHand(interactor))
            return secondaryAnchor != null ? secondaryAnchor : base.GetAttachTransform(interactor);

        // Unknown interactor fallback
        return base.GetAttachTransform(interactor);
    }

    // Se ejecuta al iniciar el agarre
    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        base.OnSelectEntering(args);

        var interactor = args.interactorObject;

         // Asigna el anchor correspondiente según la mano
        if (IsLeftHand(interactor))
            interactorAnchorMap[interactor] = primaryAnchor != null ? primaryAnchor : base.GetAttachTransform(interactor);
        else if (IsRightHand(interactor))
            interactorAnchorMap[interactor] = secondaryAnchor != null ? secondaryAnchor : base.GetAttachTransform(interactor);
        else
            interactorAnchorMap[interactor] = base.GetAttachTransform(interactor);
    }

    // Se ejecuta al soltar el objeto
    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);
        // Limpia la referencia del interactor
        interactorAnchorMap.Remove(args.interactorObject);
    }

    // Verifica si el interactor corresponde a la mano izquierda
    private bool IsLeftHand(IXRInteractor interactor) =>
        interactor.transform.CompareTag(leftHandTag);
    // Verifica si el interactor corresponde a la mano derecha
    private bool IsRightHand(IXRInteractor interactor) =>
        interactor.transform.CompareTag(rightHandTag);
}




