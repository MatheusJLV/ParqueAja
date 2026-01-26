using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

using UnityEngine.XR.Interaction.Toolkit.Interactables;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DesktopRayClickGrab : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private XRRayInteractor rayInteractor;
    [SerializeField] private XRInteractionManager interactionManager;

    private IXRSelectInteractable currentSelected;

    void Reset()
    {
        rayInteractor = GetComponent<XRRayInteractor>();
        if (!interactionManager) interactionManager = FindFirstObjectByType<XRInteractionManager>();
    }

    void Awake()
    {
        if (!rayInteractor) rayInteractor = GetComponent<XRRayInteractor>();
        if (!interactionManager) interactionManager = FindFirstObjectByType<XRInteractionManager>();
    }

    void Update()
    {
        if (!rayInteractor || !interactionManager) return;

        bool down = false, up = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            down = Mouse.current.leftButton.wasPressedThisFrame;
            up = Mouse.current.leftButton.wasReleasedThisFrame;
        }
#else
        down = Input.GetMouseButtonDown(0);
        up   = Input.GetMouseButtonUp(0);
#endif

        if (down) TrySelect();
        if (up) TryUnselect();
    }

    void TrySelect()
    {
        if (interactionManager == null)
        {
            Debug.LogError("interactionManager es NULL. Asigna el XR Interaction Manager en el inspector.");
            return;
        }

        var selectInteractor = rayInteractor as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor;
        if (selectInteractor == null)
        {
            Debug.LogError("rayInteractor NO implementa IXRSelectInteractor. Revisa que sea XRRayInteractor y que el componente esté activo.");
            return;
        }

        if (!rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            return;

        var interactable = hit.transform.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable>();
        if (interactable == null)
        {
            Debug.LogWarning("Le pegó el ray, pero ese objeto NO es IXRSelectInteractable (no tiene XRGrabInteractable o similar en el padre).");
            return;
        }

        interactionManager.SelectEnter(selectInteractor, interactable);
    }

    void TryUnselect()
    {
        if (currentSelected == null) return;

        interactionManager.SelectExit(rayInteractor, currentSelected);
        currentSelected = null;
    }
}
