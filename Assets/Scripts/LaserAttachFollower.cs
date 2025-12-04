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

    public void StopFollowing()
    {
        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
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
        StopFollowing();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isGrabbed = false;
    }

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