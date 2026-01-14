using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class DynamicGripPoints : MonoBehaviour
{
    /*
     Ajusta dinámicamente los puntos de agarre primario y secundario
     según desde dónde se toma el objeto (rayo o agarre directo).
    */

    private XRGrabInteractable grab;

    void Awake()
    {
        // Cachea el interactuable y se suscribe al evento de selección
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRRayInteractor rayInteractor)
        {
            // Si se agarra con rayo, usa la información del impacto
            if (rayInteractor.TryGetHitInfo(out Vector3 hitPos, out Vector3 hitNormal,
                                            out int _, out bool isValid) && isValid)
            {
                if (!grab.isSelected) // Primer agarre: ajustar ancla primaria
                {
                    grab.attachTransform.position = hitPos;
                    grab.attachTransform.rotation = Quaternion.LookRotation(-hitNormal, Vector3.up);
                }
                else // Ya estaba seleccionado: usar ancla secundaria
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
            // Agarre directo: usa la posición y rotación de la mano
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

