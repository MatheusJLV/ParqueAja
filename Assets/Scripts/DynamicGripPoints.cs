using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/*
 * DynamicGripPoints:
 * Ajusta dinámicamente los puntos de agarre (attach transforms) de un objeto
 * basado en cómo se agarra: con rayo (usando hit info) o directamente (mano).
 */

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class DynamicGripPoints : MonoBehaviour
{
    private XRGrabInteractable grab; // Interactable que se puede agarrar

    void Awake()
    {
        // Configurar listener para evento de selección al despertar
        grab = GetComponent<XRGrabInteractable>(); // Obtener el interactable
        grab.selectEntered.AddListener(OnSelectEntered); // Agregar listener
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Ajustar punto de agarre basado en el tipo de interactor
        if (args.interactorObject is XRRayInteractor rayInteractor) // Si es rayo
        {
            // If grabbed with ray, use the hit info
            if (rayInteractor.TryGetHitInfo(out Vector3 hitPos, out Vector3 hitNormal,
                                            out int _, out bool isValid) && isValid) // Obtener info de hit
            {
                if (!grab.isSelected) // first grab - Primer agarre
                {
                    grab.attachTransform.position = hitPos; // Posición en hit
                    grab.attachTransform.rotation = Quaternion.LookRotation(-hitNormal, Vector3.up); // Rotación basada en normal
                }
                else // already selected, so use secondary grip - Ya seleccionado, usar agarre secundario
                {
                    if (grab.secondaryAttachTransform != null)
                    {
                        grab.secondaryAttachTransform.position = hitPos; // Posición secundaria
                        grab.secondaryAttachTransform.rotation = Quaternion.LookRotation(-hitNormal, Vector3.up); // Rotación secundaria
                    }
                }
            }
        }
        else if (args.interactorObject is XRDirectInteractor directInteractor) // Si es directo
        {
            // Direct grab: just use the interactor�s hand position
            Vector3 grabPos = directInteractor.transform.position; // Posición de la mano
            Quaternion grabRot = directInteractor.transform.rotation; // Rotación de la mano

            if (!grab.isSelected) // Primer agarre
            {
                grab.attachTransform.position = grabPos; // Usar posición de mano
                grab.attachTransform.rotation = grabRot; // Usar rotación de mano
            }
            else // Agarres secundarios
            {
                if (grab.secondaryAttachTransform != null)
                {
                    grab.secondaryAttachTransform.position = grabPos; // Posición secundaria
                    grab.secondaryAttachTransform.rotation = grabRot; // Rotación secundaria
                }
            }
        }
    }
}

