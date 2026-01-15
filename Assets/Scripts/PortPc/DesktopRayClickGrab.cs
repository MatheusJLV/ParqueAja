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
        if (currentSelected != null) return;

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            var interactable = hit.transform.GetComponentInParent<IXRSelectInteractable>();
            interactionManager.SelectEnter(rayInteractor as IXRSelectInteractor, interactable);


            interactionManager.SelectExit(rayInteractor as IXRSelectInteractor, currentSelected);
            currentSelected = interactable;
        }
    }

    void TryUnselect()
    {
        if (currentSelected == null) return;

        interactionManager.SelectExit(rayInteractor, currentSelected);
        currentSelected = null;
    }
}
