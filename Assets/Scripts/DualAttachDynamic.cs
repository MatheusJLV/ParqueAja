using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class DualAttachDynamic : MonoBehaviour
{
    /*
     Maneja eventos de hover para identificar con qué mano o interactor
     se acerca el objeto y registra el modo (cercano, lejano, socket).
    */

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        // Cachea la referencia y se suscribe a eventos de hover
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDestroy()
    {
        // Limpia suscripciones para evitar referencias pendientes
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Identifica el interactor que hizo hover y su modo de interacción
        var interactor = args.interactorObject as IXRInteractor;
        // Usa el nombre del transform como identificador de mano; fallback a "Unknown" si falta
        string hand = interactor?.transform?.name ?? "Unknown";

        string mode = "Unknown";

        if (interactor is NearFarInteractor nearFar)
        {
            // Consulta el tipo de punto final para saber si fue rayo lejano o toque cercano
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

        // Registro de depuración con el origen y modo del hover
        Debug.Log($"[HoverLogger] {gameObject.name} hovered by {hand} using {mode}");
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Registro de depuración al salir del hover
        var interactor = args.interactorObject as IXRInteractor;
        string hand = interactor?.transform?.name ?? "Unknown";
        Debug.Log($"[HoverLogger] {gameObject.name} hover exited by {hand}");
    }
}
