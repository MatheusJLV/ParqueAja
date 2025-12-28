using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/*
 * DualAttachDynamic:
 * Registra eventos de hover en objetos agarrables XR, identificando la mano
 * y el modo de interacción (near, far, socket) para logging de debug.
 */

public class DualAttachDynamic : MonoBehaviour
{
    private XRGrabInteractable grabInteractable; // Interactable que se puede agarrar

    private void Awake()
    {
        // Configurar listeners para eventos de hover al despertar
        grabInteractable = GetComponent<XRGrabInteractable>(); // Obtener el interactable
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEntered); // Agregar listener para hover entered
            grabInteractable.hoverExited.AddListener(OnHoverExited); // Agregar listener para hover exited
        }
    }

    private void OnDestroy()
    {
        // Limpiar listeners al destruir para evitar memory leaks
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered); // Remover listener
            grabInteractable.hoverExited.RemoveListener(OnHoverExited); // Remover listener
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Registrar cuando un interactor hace hover sobre el objeto
        var interactor = args.interactorObject as IXRInteractor; // Obtener interactor
        string hand = interactor?.transform?.name ?? "Unknown"; // Nombre de la mano

        string mode = "Unknown"; // Modo de interacción

        if (interactor is NearFarInteractor nearFar) // Si es NearFarInteractor
        {
            var endPointType = nearFar.TryGetCurveEndPoint( // Obtener tipo de punto final
                out Vector3 _,
                snapToSelectedAttachIfAvailable: false,
                snapToSnapVolumeIfAvailable: false
            );

            mode = endPointType switch // Determinar modo basado en tipo
            {
                EndPointType.ValidCastHit => "Far (Laser)",
                EndPointType.AttachPoint => "Near (Touch)",
                EndPointType.None => "Near (Direct Range)",
                _ => "Unknown"
            };
        }
        else if (interactor is XRDirectInteractor) // Si es DirectInteractor
        {
            mode = "Near (Direct Hand)";
        }
        else if (interactor is XRSocketInteractor) // Si es SocketInteractor
        {
            mode = "Socket";
        }

        Debug.Log($"[HoverLogger] {gameObject.name} hovered by {hand} using {mode}"); // Log de debug
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Registrar cuando un interactor deja de hacer hover sobre el objeto
        var interactor = args.interactorObject as IXRInteractor; // Obtener interactor
        string hand = interactor?.transform?.name ?? "Unknown"; // Nombre de la mano
        Debug.Log($"[HoverLogger] {gameObject.name} hover exited by {hand}"); // Log de debug
    }
}
