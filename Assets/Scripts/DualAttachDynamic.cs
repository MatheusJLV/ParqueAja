using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class DualAttachDynamic : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        var interactor = args.interactorObject as IXRInteractor;
        string hand = interactor?.transform?.name ?? "Unknown";

        string mode = "Unknown";

        if (interactor is NearFarInteractor nearFar)
        {
            var endPointType = nearFar.TryGetCurveEndPoint(
                out Vector3 _,
                snapToSelectedAttachIfAvailable: false,
                snapToSnapVolumeIfAvailable: false
            );

            mode = endPointType switch
            {
                EndPointType.ValidCastHit => "Far (Laser)",
                EndPointType.AttachPoint => "Near (Touch)",
                EndPointType.None => "Near (Direct Range)",
                _ => "Unknown"
            };
        }
        else if (interactor is XRDirectInteractor)
        {
            mode = "Near (Direct Hand)";
        }
        else if (interactor is XRSocketInteractor)
        {
            mode = "Socket";
        }

        Debug.Log($"[HoverLogger] {gameObject.name} hovered by {hand} using {mode}");
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        var interactor = args.interactorObject as IXRInteractor;
        string hand = interactor?.transform?.name ?? "Unknown";
        Debug.Log($"[HoverLogger] {gameObject.name} hover exited by {hand}");
    }
}
