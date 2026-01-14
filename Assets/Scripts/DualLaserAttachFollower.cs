using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class DualLaserAttachFollower : MonoBehaviour
{
    /*
     Ajusta dinámicamente los puntos de anclaje para manos izquierda y derecha
     siguiendo el rayo (far) o la mano (near) según el tipo de interactor.
    */

    [Header("Controller References")]
    [SerializeField] private NearFarInteractor leftNearFarInteractor;
    [SerializeField] private NearFarInteractor rightNearFarInteractor;

    [Header("Attach Points to Adjust")]
    [SerializeField] private Transform primaryAnchor;   // Ancla para mano izquierda
    [SerializeField] private Transform secondaryAnchor; // Ancla para mano derecha

    private XRGrabInteractable grabInteractable;

    // Corrutinas activas para seguir el punto de cada mano
    private Coroutine leftRoutine;
    private Coroutine rightRoutine;

    // Flags de bloqueo cuando la mano está agarrando (no se ajusta por hover)
    private bool leftIsGrabbing = false;
    private bool rightIsGrabbing = false;

    private void Awake()
    {
        // Cachea el XRGrabInteractable y se suscribe a eventos de selección y hover
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
        // Limpia las suscripciones para evitar referencias colgantes
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
        // Marca el estado de agarre de cada mano y detiene la rutina dinámica
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
        // Libera la marca de agarre para permitir que el hover vuelva a ajustar
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

        // Solo inicia seguimiento si la mano no está agarrando y no hay corrutina corriendo
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
        // Al salir del hover, solo registra y detiene la rutina correspondiente
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
        // Detiene la corrutina de seguimiento para la mano asociada
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
        // Sigue el punto final del rayo o la posición de la mano para actualizar el ancla
        while (true)
        {
            if (interactor is NearFarInteractor nearFar)
            {
                // Usa el punto final válido del rayo; si no hay hit, cae a la posición de la mano
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

            // Log de depuración cada cierto número de frames para evitar spam
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[DualLaserAttachFollower] {handLabel} hand updating {targetAnchor.name} at {targetAnchor.position}");
            }

            yield return null;
        }
    }

    private bool IsLeftHand(IXRInteractor interactor) =>
        // Usa tag para identificar mano izquierda
        interactor.transform.CompareTag("LeftHand");

    private bool IsRightHand(IXRInteractor interactor) =>
        // Usa tag para identificar mano derecha
        interactor.transform.CompareTag("RightHand");
}
