using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;


public class LaserAttachFollower : MonoBehaviour
{
    [Header("Controller References")]
    [SerializeField] private NearFarInteractor leftNearFarInteractor;
    [SerializeField] private NearFarInteractor rightNearFarInteractor;
    [SerializeField] private Transform leftControllerPointer;
    [SerializeField] private Transform rightControllerPointer;

    [SerializeField] private Transform attachPoint;              // Grip/handler to move

    private IXRInteractor activeInteractor;

    private bool isGrabbed = false;


    private Coroutine followRoutine;

    // Call this from HoverEnter event
    /*public void StartFollowing()
    {
        if (followRoutine == null && nearFarInteractor != null && attachPoint != null)
        {
            followRoutine = StartCoroutine(FollowLaserHitPoint());
        }
    }*/

    // Call this from HoverExit event
    public void StopFollowing()
    {
        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }
    }

    /*private IEnumerator FollowLaserHitPoint()
    {
        while (true)
        {
            // Get laser origin and endpoint
            Vector3 origin = nearFarInteractor.transform.position;

            nearFarInteractor.TryGetCurveEndPoint(
                out Vector3 end,
                snapToSelectedAttachIfAvailable: false,
                snapToSnapVolumeIfAvailable: false);

            // Update attachPoint to follow the laser tip
            attachPoint.position = end;

            // Optional: orient attach grip to laser direction
            Vector3 forward = (end - origin).normalized;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            up = Vector3.Cross(forward, right).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, up);

            attachPoint.rotation = rotation;

            yield return null; // Wait one frame
        }
    }*/

    private IEnumerator FollowControllerPointer(Transform controllerPointer)
    {
        while (true)
        {
            if (controllerPointer != null && attachPoint != null)
            {
                // Directly mirror the controllerPointer transform
                attachPoint.position = controllerPointer.position;
                attachPoint.rotation = controllerPointer.rotation;

                Debug.Log($"[AttachFollower] Following controller pointer at {controllerPointer.position}");
            }

            yield return null; // Wait one frame
        }
    }


    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);

            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);

        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);

            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isGrabbed = true;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isGrabbed = false;
    }


    /*private void OnHoverEntered(HoverEnterEventArgs args)
    {
        var interactor = args.interactorObject as IXRInteractor;
        string hand = interactor?.transform?.name ?? "Unknown";

        string mode = "Unknown";

        // Stop any previous follow routine
        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }

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

            if (mode == "Far (Laser)")
            {
                followRoutine = StartCoroutine(FollowLaserHitPoint());
            }
            else if (mode == "Near (Direct Range)" && controllerPointer != null)
            {
                followRoutine = StartCoroutine(FollowControllerPointer(controllerPointer));
            }
        }
        else if (interactor is XRDirectInteractor)
        {
            mode = "Near (Direct Hand)";
            if (controllerPointer != null)
            {
                followRoutine = StartCoroutine(FollowControllerPointer(controllerPointer));
            }
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

        StopFollowing();
    }*/



    /*private void OnHoverEntered(HoverEnterEventArgs args)
    {
        var interactor = args.interactorObject as IXRInteractor;

        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }

        followRoutine = StartCoroutine(FollowDynamic(interactor));

        Debug.Log($"[HoverLogger] {gameObject.name} hovered by {interactor?.transform?.name}");
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        StopFollowing();
    }*/


    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (isGrabbed) return; // Ignore hover from the free hand

        var interactor = args.interactorObject as IXRInteractor;

        activeInteractor = interactor;

        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }

        followRoutine = StartCoroutine(FollowDynamic(activeInteractor));
    }


    private void OnHoverExited(HoverExitEventArgs args)
    {
        var interactor = args.interactorObject as IXRInteractor;

        if (interactor == activeInteractor)
        {
            StopFollowing();
            activeInteractor = null;
        }
    }



    private IEnumerator FollowDynamic(IXRInteractor interactor)
    {
        while (true)
        {
            if (interactor is NearFarInteractor nearFar)
            {
                var type = nearFar.TryGetCurveEndPoint(
                    out Vector3 end,
                    snapToSelectedAttachIfAvailable: false,
                    snapToSnapVolumeIfAvailable: false);

                if (type == EndPointType.ValidCastHit)
                {
                    // Laser
                    attachPoint.position = end;
                    attachPoint.rotation = Quaternion.LookRotation((end - nearFar.transform.position).normalized);
                }
                else
                {
                    // Near/direct fallback
                    attachPoint.position = interactor.transform.position;
                    attachPoint.rotation = interactor.transform.rotation;
                }
            }
            else
            {
                // Direct/sockets
                attachPoint.position = interactor.transform.position;
                attachPoint.rotation = interactor.transform.rotation;
            }

            yield return null;
        }
    }

}
