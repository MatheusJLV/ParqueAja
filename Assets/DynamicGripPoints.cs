using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class DynamicGripPoints : MonoBehaviour
{
    private XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRRayInteractor rayInteractor)
        {
            // If grabbed with ray, use the hit info
            if (rayInteractor.TryGetHitInfo(out Vector3 hitPos, out Vector3 hitNormal,
                                            out int _, out bool isValid) && isValid)
            {
                if (!grab.isSelected) // first grab
                {
                    grab.attachTransform.position = hitPos;
                    grab.attachTransform.rotation = Quaternion.LookRotation(-hitNormal, Vector3.up);
                }
                else // already selected, so use secondary grip
                {
                    if (grab.secondaryAttachTransform != null)
                    {
                        grab.secondaryAttachTransform.position = hitPos;
                        grab.secondaryAttachTransform.rotation = Quaternion.LookRotation(-hitNormal, Vector3.up);
                    }
                }
            }
        }
        else if (args.interactorObject is XRDirectInteractor directInteractor)
        {
            // Direct grab: just use the interactor’s hand position
            Vector3 grabPos = directInteractor.transform.position;
            Quaternion grabRot = directInteractor.transform.rotation;

            if (!grab.isSelected)
            {
                grab.attachTransform.position = grabPos;
                grab.attachTransform.rotation = grabRot;
            }
            else
            {
                if (grab.secondaryAttachTransform != null)
                {
                    grab.secondaryAttachTransform.position = grabPos;
                    grab.secondaryAttachTransform.rotation = grabRot;
                }
            }
        }
    }
}

