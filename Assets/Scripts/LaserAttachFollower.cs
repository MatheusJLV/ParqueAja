using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;


// Hace que el punto de agarre siga dinámicamente la posición del interactor activo
// Soporta interacción por rayo láser (NearFarInteractor) y contacto directo
// El seguimiento se detiene cuando el objeto es agarrado
public class LaserAttachFollower : MonoBehaviour
{
    [Header("Controller References")]
    // Referencias a los interactores near/far de ambos controladores
    [SerializeField] private NearFarInteractor leftNearFarInteractor;
    [SerializeField] private NearFarInteractor rightNearFarInteractor;
    // Puntos que apuntan hacia dónde se dirigen los rayos (posición virtual)
    [SerializeField] private Transform leftControllerPointer;
    [SerializeField] private Transform rightControllerPointer;

    // Punto de agarre que se mueve para seguir el interactor
    [SerializeField] private Transform attachPoint;

    // Interactor actual que está siendo rastreado
    private IXRInteractor activeInteractor;

    // Indica si el objeto está siendo agarrado actualmente
    private bool isGrabbed = false;

    // Corrutina activa que sigue el movimiento del interactor
    private Coroutine followRoutine;

    // Detiene la corrutina de seguimiento activa
    public void StopFollowing()
    {
        if (followRoutine != null)
        {
            // Cancela la corrutina de seguimiento
            StopCoroutine(followRoutine);
            followRoutine = null;
        }
    }

    // Componente que maneja las interacciones de agarre
    private XRGrabInteractable grabInteractable;

    // Inicialización del componente e instalación de listeners de eventos
    private void Awake()
    {
        // Obtiene el componente de interacción grabable
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // Suscribe a eventos de entrada/salida de hover
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);

            // Suscribe a eventos de entrada/salida de selección (agarre)
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);

        }
    }

    // Limpia los listeners de eventos al destruir el objeto
    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            // Desuscribe de los eventos de hover
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);

            // Desuscribe de los eventos de selección
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    // Se dispara cuando el usuario comienza a agarrar el objeto
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Marca el objeto como agarrado
        isGrabbed = true;
        // Detiene el seguimiento mientras se agarra
        StopFollowing();
    }

    // Se dispara cuando el usuario suelta el objeto
    private void OnSelectExited(SelectExitEventArgs args)
    {
        // Marca el objeto como no agarrado
        isGrabbed = false;
    }

    // Se dispara cuando un interactor entra en el área de hover del objeto
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Si está siendo agarrado, ignora el hover de la otra mano
        if (isGrabbed) return;

        // Obtiene el interactor que activó el hover
        var interactor = args.interactorObject as IXRInteractor;

        // Almacena como interactor activo
        activeInteractor = interactor;

        // Detiene la corrutina anterior si existe
        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }

        // Inicia nueva corrutina de seguimiento
        followRoutine = StartCoroutine(FollowDynamic(activeInteractor));
    }


    // Se dispara cuando un interactor sale del área de hover del objeto
    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Obtiene el interactor que salió
        var interactor = args.interactorObject as IXRInteractor;

        // Si es el interactor activo, detiene el seguimiento
        if (interactor == activeInteractor)
        {
            StopFollowing();
            activeInteractor = null;
        }
    }



    // Corrutina que sigue dinámicamente el movimiento del interactor
    private IEnumerator FollowDynamic(IXRInteractor interactor)
    {
        while (true)
        {
            // Si es un interactor near/far, intenta obtener el punto final del rayo
            if (interactor is NearFarInteractor nearFar)
            {
                // Obtiene el punto donde el rayo impacta
                var type = nearFar.TryGetCurveEndPoint(
                    out Vector3 end,
                    snapToSelectedAttachIfAvailable: false,
                    snapToSnapVolumeIfAvailable: false);

                // Si el rayo impactó algo, sigue ese punto
                if (type == EndPointType.ValidCastHit)
                {
                    // Usa el punto de impacto del rayo como destino
                    attachPoint.position = end;
                    // Rota para mirar hacia el controlador
                    attachPoint.rotation = Quaternion.LookRotation((end - nearFar.transform.position).normalized);
                }
                else
                {
                    // Si no hay impacto, usa posición/rotación del interactor como fallback
                    attachPoint.position = interactor.transform.position;
                    attachPoint.rotation = interactor.transform.rotation;
                }
            }
            else
            {
                // Para interactores directos (contacto/sockets), sigue su transformada
                attachPoint.position = interactor.transform.position;
                attachPoint.rotation = interactor.transform.rotation;
            }

            yield return null;
        }
    }

}