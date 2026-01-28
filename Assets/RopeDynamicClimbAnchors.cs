using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class RopeDynamicClimbAnchors : MonoBehaviour
{
    [Header("Interactors (Rays)")]
    [SerializeField] private Transform leftRayOrigin;   // usually Left controller / ray origin
    [SerializeField] private Transform rightRayOrigin;  // usually Right controller / ray origin

    [Header("Anchors (Climb points)")]
    [SerializeField] private XRBaseInteractable leftAnchorInteractable;
    [SerializeField] private XRBaseInteractable rightAnchorInteractable;

    [Header("Rope Collider(s)")]
    [SerializeField] private Collider[] ropeColliders;

    [Header("Ray Settings")]
    [SerializeField] private float maxRayDistance = 10f;
    [SerializeField] private LayerMask raycastMask = ~0; // should include the rope layer
    [Tooltip("Layers to ignore (anchors should be on one of these layers).")]
    [SerializeField] private LayerMask ignoreLayers = 0;

    [Header("Behavior")]
    [SerializeField] private bool rotateToRay = true;
    [SerializeField] private float surfaceOffset = 0.0f;

    private bool leftLocked;
    private bool rightLocked;

    private void Awake()
    {
        if (ropeColliders == null || ropeColliders.Length == 0)
            ropeColliders = GetComponentsInChildren<Collider>(includeInactive: false);

        if (leftAnchorInteractable != null)
        {
            leftAnchorInteractable.selectEntered.AddListener(_ => leftLocked = true);
            leftAnchorInteractable.selectExited.AddListener(_ => leftLocked = false);
        }

        if (rightAnchorInteractable != null)
        {
            rightAnchorInteractable.selectEntered.AddListener(_ => rightLocked = true);
            rightAnchorInteractable.selectExited.AddListener(_ => rightLocked = false);
        }
    }

    private void Update()
    {
        if (!leftLocked)
            TryPlaceAnchor(leftRayOrigin, leftAnchorInteractable?.transform);

        if (!rightLocked)
            TryPlaceAnchor(rightRayOrigin, rightAnchorInteractable?.transform);
    }

    private void TryPlaceAnchor(Transform rayOrigin, Transform anchor)
    {
        if (rayOrigin == null || anchor == null) return;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        // Build a mask that excludes ignoreLayers
        int mask = raycastMask & ~ignoreLayers.value;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRayDistance, mask, QueryTriggerInteraction.Ignore))
        {
            if (!IsRopeCollider(hit.collider))
                return;

            anchor.position = hit.point + hit.normal * surfaceOffset;

            if (rotateToRay)
                anchor.rotation = Quaternion.LookRotation(dir);
        }
    }

    private bool IsRopeCollider(Collider c)
    {
        if (c == null || ropeColliders == null) return false;

        for (int i = 0; i < ropeColliders.Length; i++)
            if (ropeColliders[i] == c)
                return true;

        return false;
    }
}

