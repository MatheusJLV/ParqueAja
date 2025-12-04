using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class DualLaserAttachFollower : MonoBehaviour
{
    [Header("Controller References")]
    [SerializeField] private NearFarInteractor leftNearFarInteractor;
    [SerializeField] private NearFarInteractor rightNearFarInteractor;

    [Header("Attach Points to Adjust")]
    [SerializeField] private Transform primaryAnchor;   // Left-hand anchor
    [SerializeField] private Transform secondaryAnchor; // Right-hand anchor

    private XRGrabInteractable grabInteractable;

    private Coroutine leftRoutine;
    private Coroutine rightRoutine;

    private bool leftIsGrabbing = false;
    private bool rightIsGrabbing = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (IsLeftHand(args.interactorObject))
        {
            leftIsGrabbing = true;
            Debug.Log("[DualLaserAttachFollower] LEFT hand grabbed → locking PRIMARY anchor");
        }

        if (IsRightHand(args.interactorObject))
        {
            rightIsGrabbing = true;
            Debug.Log("[DualLaserAttachFollower] RIGHT hand grabbed → locking SECONDARY anchor");
        }

        StopRoutineFor(args.interactorObject);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (IsLeftHand(args.interactorObject))
        {
            leftIsGrabbing = false;
            Debug.Log("[DualLaserAttachFollower] LEFT hand released → PRIMARY anchor free");
        }

        if (IsRightHand(args.interactorObject))
        {
            rightIsGrabbing = false;
            Debug.Log("[DualLaserAttachFollower] RIGHT hand released → SECONDARY anchor free");
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        var interactor = args.interactorObject as IXRInteractor;
        if (interactor == null) return;

        if (IsLeftHand(interactor) && !leftIsGrabbing && leftRoutine == null)
        {
            Debug.Log("[DualLaserAttachFollower] LEFT hand started adjusting PRIMARY anchor");
            leftRoutine = StartCoroutine(FollowDynamic(interactor, primaryAnchor, "LEFT"));
        }

        if (IsRightHand(interactor) && !rightIsGrabbing && rightRoutine == null)
        {
            Debug.Log("[DualLaserAttachFollower] RIGHT hand started adjusting SECONDARY anchor");
            rightRoutine = StartCoroutine(FollowDynamic(interactor, secondaryAnchor, "RIGHT"));
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (IsLeftHand(args.interactorObject))
        {
            Debug.Log("[DualLaserAttachFollower] LEFT hand stopped adjusting PRIMARY anchor");
        }

        if (IsRightHand(args.interactorObject))
        {
            Debug.Log("[DualLaserAttachFollower] RIGHT hand stopped adjusting SECONDARY anchor");
        }

        StopRoutineFor(args.interactorObject);
    }

    private void StopRoutineFor(IXRInteractor interactor)
    {
        if (IsLeftHand(interactor) && leftRoutine != null)
        {
            StopCoroutine(leftRoutine);
            leftRoutine = null;
        }
        if (IsRightHand(interactor) && rightRoutine != null)
        {
            StopCoroutine(rightRoutine);
            rightRoutine = null;
        }
    }

    private IEnumerator FollowDynamic(IXRInteractor interactor, Transform targetAnchor, string handLabel)
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
                    targetAnchor.position = end;
                    targetAnchor.rotation = Quaternion.LookRotation((end - nearFar.transform.position).normalized);
                }
                else
                {
                    targetAnchor.position = interactor.transform.position;
                    targetAnchor.rotation = interactor.transform.rotation;
                }
            }
            else
            {
                targetAnchor.position = interactor.transform.position;
                targetAnchor.rotation = interactor.transform.rotation;
            }

            // Debug log every few frames (to avoid spamming)
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[DualLaserAttachFollower] {handLabel} hand updating {targetAnchor.name} at {targetAnchor.position}");
            }

            yield return null;
        }
    }

    private bool IsLeftHand(IXRInteractor interactor) =>
        interactor.transform.CompareTag("LeftHand");

    private bool IsRightHand(IXRInteractor interactor) =>
        interactor.transform.CompareTag("RightHand");
}
