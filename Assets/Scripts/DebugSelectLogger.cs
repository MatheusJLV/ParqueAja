using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class DebugSelectLogger : MonoBehaviour
{
    // Escucha eventos de hover en XRInteractables y reporta quién interactúa y con qué modo
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        // Cachea la referencia al interactuable y se suscribe a eventos de hover
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // Solo se suscribe si el componente existe para evitar null references en prefabs incompletos
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDestroy()
    {
        // Evita referencias colgantes al destruir el objeto
        if (grabInteractable != null)
        {
            // Limpia los listeners por si el objeto se destruye durante juego o recarga de escena
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Intenta resolver el interactor que está haciendo hover y el modo (cercano, lejano, socket)
        var interactor = args.interactorObject as IXRInteractor;
        string hand = interactor?.transform?.name ?? "Unknown";

        string mode = "Unknown";

        if (interactor is NearFarInteractor nearFar)
        {
            // Consulta el tipo de punto de fin de rayo para diferenciar interacción láser vs. toque
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

        // Traza en consola quién y cómo está apuntando/rozando este objeto
        Debug.Log($"[HoverLogger] {gameObject.name} hovered by {hand} using {mode}");
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Loguea la salida de hover indicando qué mano o interactor lo generó
        var interactor = args.interactorObject as IXRInteractor;
        string hand = interactor?.transform?.name ?? "Unknown";
        Debug.Log($"[HoverLogger] {gameObject.name} hover exited by {hand}");
    }
}

