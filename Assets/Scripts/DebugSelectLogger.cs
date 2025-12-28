using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class DebugSelectLogger : MonoBehaviour
{
    /*
     Registra eventos de hover para objetos interactuables en XR,
     mostrando logs de depuración con información sobre el interactor
     y el modo de interacción utilizado.
    */

    // Componente interactuable que se monitorea
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        // Obtiene el componente XRGrabInteractable del mismo objeto
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // Agrega listeners para eventos de hover
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            // Remueve los listeners para evitar errores al destruir el objeto
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    // Maneja el evento cuando un interactor entra en hover
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Obtiene el interactor y determina el nombre de la mano
        var interactor = args.interactorObject as IXRInteractor;
        string hand = interactor?.transform?.name ?? "Unknown";

        string mode = "Unknown";

        // Determina el modo de interacción basado en el tipo de interactor
        if (interactor is NearFarInteractor nearFar)
        {
            // Intenta obtener el punto final de la curva del interactor near-far
            var endPointType = nearFar.TryGetCurveEndPoint(
                out Vector3 _,
                snapToSelectedAttachIfAvailable: false,
                snapToSnapVolumeIfAvailable: false
            );

            // Asigna el modo basado en el tipo de punto final obtenido
            mode = endPointType switch
            {
                EndPointType.ValidCastHit => "Far (Laser)",  // Interacción a distancia con láser
                EndPointType.AttachPoint => "Near (Touch)",  // Interacción cercana por toque
                EndPointType.None => "Near (Direct Range)",  // Interacción cercana directa
                _ => "Unknown"  // Modo desconocido
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

        // Registra el evento en el log de depuración
        Debug.Log($"[HoverLogger] {gameObject.name} hovered by {hand} using {mode}");
    }

    // Maneja el evento cuando un interactor sale del hover
    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Obtiene el interactor y determina el nombre de la mano
        var interactor = args.interactorObject as IXRInteractor;
        string hand = interactor?.transform?.name ?? "Unknown";

        // Registra el evento en el log de depuración
        Debug.Log($"[HoverLogger] {gameObject.name} hover exited by {hand}");
    }
}

