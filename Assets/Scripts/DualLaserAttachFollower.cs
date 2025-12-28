using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/*
 * DualLaserAttachFollower:
 * Gestiona el ajuste dinámico de puntos de agarre para objetos agarrables en VR,
 * permitiendo que los anclajes primarios y secundarios sigan las posiciones de los
 * interactores de ambas manos durante el hover, mejorando la precisión de agarre.
 */

public class DualLaserAttachFollower : MonoBehaviour
{
    [Header("Controller References")]
    [SerializeField] private NearFarInteractor leftNearFarInteractor;  // Interactor cercano-lejano para la mano izquierda
    [SerializeField] private NearFarInteractor rightNearFarInteractor; // Interactor cercano-lejano para la mano derecha

    [Header("Attach Points to Adjust")]
    [SerializeField] private Transform primaryAnchor;   // Anclaje primario (mano izquierda)
    [SerializeField] private Transform secondaryAnchor; // Anclaje secundario (mano derecha)

    private XRGrabInteractable grabInteractable; // Interactable de agarre asociado

    private Coroutine leftRoutine;  // Corrutina para seguimiento de mano izquierda
    private Coroutine rightRoutine; // Corrutina para seguimiento de mano derecha

    private bool leftIsGrabbing = false;  // Indica si la mano izquierda está agarrando
    private bool rightIsGrabbing = false; // Indica si la mano derecha está agarrando

    private void Awake()
    {
        // Obtener el componente XRGrabInteractable y suscribir eventos
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
        // Desuscribir eventos al destruir el objeto
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
        // Marcar como agarrando y bloquear anclaje si es mano izquierda
        if (IsLeftHand(args.interactorObject))
        {
            leftIsGrabbing = true;
            Debug.Log("[DualLaserAttachFollower] LEFT hand grabbed → locking PRIMARY anchor");
        }

        // Marcar como agarrando y bloquear anclaje si es mano derecha
        if (IsRightHand(args.interactorObject))
        {
            rightIsGrabbing = true;
            Debug.Log("[DualLaserAttachFollower] RIGHT hand grabbed → locking SECONDARY anchor");
        }

        // Detener rutina de seguimiento para esta mano
        StopRoutineFor(args.interactorObject);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // Liberar anclaje si es mano izquierda
        if (IsLeftHand(args.interactorObject))
        {
            leftIsGrabbing = false;
            Debug.Log("[DualLaserAttachFollower] LEFT hand released → PRIMARY anchor free");
        }

        // Liberar anclaje si es mano derecha
        if (IsRightHand(args.interactorObject))
        {
            rightIsGrabbing = false;
            Debug.Log("[DualLaserAttachFollower] RIGHT hand released → SECONDARY anchor free");
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Obtener el interactor y verificar tipo
        var interactor = args.interactorObject as IXRInteractor;
        if (interactor == null) return;

        // Iniciar seguimiento para mano izquierda si no está agarrando
        if (IsLeftHand(interactor) && !leftIsGrabbing && leftRoutine == null)
        {
            Debug.Log("[DualLaserAttachFollower] LEFT hand started adjusting PRIMARY anchor");
            leftRoutine = StartCoroutine(FollowDynamic(interactor, primaryAnchor, "LEFT"));
        }

        // Iniciar seguimiento para mano derecha si no está agarrando
        if (IsRightHand(interactor) && !rightIsGrabbing && rightRoutine == null)
        {
            Debug.Log("[DualLaserAttachFollower] RIGHT hand started adjusting SECONDARY anchor");
            rightRoutine = StartCoroutine(FollowDynamic(interactor, secondaryAnchor, "RIGHT"));
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Log para mano izquierda saliendo de hover
        if (IsLeftHand(args.interactorObject))
        {
            Debug.Log("[DualLaserAttachFollower] LEFT hand stopped adjusting PRIMARY anchor");
        }

        // Log para mano derecha saliendo de hover
        if (IsRightHand(args.interactorObject))
        {
            Debug.Log("[DualLaserAttachFollower] RIGHT hand stopped adjusting SECONDARY anchor");
        }

        // Detener rutina de seguimiento
        StopRoutineFor(args.interactorObject);
    }

    private void StopRoutineFor(IXRInteractor interactor)
    {
        // Detener corrutina de mano izquierda si existe
        if (IsLeftHand(interactor) && leftRoutine != null)
        {
            StopCoroutine(leftRoutine);
            leftRoutine = null;
        }
        // Detener corrutina de mano derecha si existe
        if (IsRightHand(interactor) && rightRoutine != null)
        {
            StopCoroutine(rightRoutine);
            rightRoutine = null;
        }
    }

    private IEnumerator FollowDynamic(IXRInteractor interactor, Transform targetAnchor, string handLabel)
    {
        // Bucle infinito para actualizar posición del anclaje
        while (true)
        {
            // Si es NearFarInteractor, usar punto final de curva
            if (interactor is NearFarInteractor nearFar)
            {
                var type = nearFar.TryGetCurveEndPoint(
                    out Vector3 end,
                    snapToSelectedAttachIfAvailable: false,
                    snapToSnapVolumeIfAvailable: false);

                // Si hay un hit válido, posicionar anclaje en el punto final
                if (type == EndPointType.ValidCastHit)
                {
                    targetAnchor.position = end;
                    targetAnchor.rotation = Quaternion.LookRotation((end - nearFar.transform.position).normalized);
                }
                else
                {
                    // Fallback: usar posición del interactor
                    targetAnchor.position = interactor.transform.position;
                    targetAnchor.rotation = interactor.transform.rotation;
                }
            }
            else
            {
                // Para otros interactores, usar posición directa
                targetAnchor.position = interactor.transform.position;
                targetAnchor.rotation = interactor.transform.rotation;
            }

            // Log de debug cada 30 frames para evitar spam
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[DualLaserAttachFollower] {handLabel} hand updating {targetAnchor.name} at {targetAnchor.position}");
            }

            yield return null;
        }
    }

    // Verifica si el interactor es de la mano izquierda por tag
    private bool IsLeftHand(IXRInteractor interactor) =>
        interactor.transform.CompareTag("LeftHand");

    // Verifica si el interactor es de la mano derecha por tag
    private bool IsRightHand(IXRInteractor interactor) =>
        interactor.transform.CompareTag("RightHand");
}
