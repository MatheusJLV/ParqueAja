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

    // ----------------------------
    // Shader Graph offset scrolling
    // ----------------------------
    [Header("Shader Graph - Rope Texture Motion")]
    [SerializeField] private Renderer ropeRenderer;               // renderer using the Shader Graph material
    [SerializeField] private string shaderOffsetProperty = "_Offset"; // Blackboard reference name
    [SerializeField] private Vector2 scrollAxis = new Vector2(0f, 1f); // (0,1)=V, (1,0)=U, can be diagonal
    [SerializeField] private float scrollMultiplier = -8f;        // negative often feels correct (rope moves opposite you)

    [Header("Rig Tracking (recommended)")]
    [Tooltip("XR Origin / XR Rig root (the transform that actually moves when climbing).")]
    [SerializeField] private Transform rigTransform;

    [Header("Rope Axis Reference")]
    [Tooltip("If null, uses this transform. Choose which axis represents rope length.")]
    [SerializeField] private Transform ropeAxisReference;
    [SerializeField] private bool useForwardAxis = false; // false=up, true=forward

    [Tooltip("If true, resets shader offset to (0,0) when you let go.")]
    [SerializeField] private bool resetOffsetOnRelease = false;

    private bool leftLocked;
    private bool rightLocked;

    // Shader offset internals (no allocations per frame)
    private MaterialPropertyBlock mpb;
    private int offsetId;
    private bool hasOffsetProperty;
    private Vector2 ropeTexOffset;
    private Vector3 lastRigPos;
    private bool rigTrackingActive;

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

    private void Start()
    {
        if (ropeAxisReference == null)
            ropeAxisReference = transform;

        mpb = new MaterialPropertyBlock();
        offsetId = Shader.PropertyToID(shaderOffsetProperty);

        // Validate property exists on the material
        if (ropeRenderer != null && ropeRenderer.sharedMaterial != null)
        {
            hasOffsetProperty = ropeRenderer.sharedMaterial.HasProperty(offsetId);

            // Initialize from material (Shader Graph Vector2 usually stored as Vector4)
            if (hasOffsetProperty)
            {
                Vector4 v = ropeRenderer.sharedMaterial.GetVector(offsetId);
                ropeTexOffset = new Vector2(v.x, v.y);
            }
        }

        if (rigTransform != null)
            lastRigPos = rigTransform.position;
    }

    private void Update()
    {
        // Keep your original behavior 1:1
        if (!leftLocked)
            TryPlaceAnchor(leftRayOrigin, leftAnchorInteractable?.transform);

        if (!rightLocked)
            TryPlaceAnchor(rightRayOrigin, rightAnchorInteractable?.transform);

        // Only added behavior: scroll shader offset while climbing
        UpdateShaderOffsetWhileClimbing();
    }

    private void UpdateShaderOffsetWhileClimbing()
    {
        bool anyClimbing = leftLocked || rightLocked;
        if (!anyClimbing)
        {
            rigTrackingActive = false;

            if (resetOffsetOnRelease && ropeRenderer != null && hasOffsetProperty)
            {
                ropeTexOffset = Vector2.zero;
                ropeRenderer.GetPropertyBlock(mpb);
                mpb.SetVector(offsetId, new Vector4(ropeTexOffset.x, ropeTexOffset.y, 0f, 0f));
                ropeRenderer.SetPropertyBlock(mpb);
            }
            return;
        }

        if (!hasOffsetProperty) return;
        if (ropeRenderer == null) return;
        if (rigTransform == null) return;

        Vector3 axis = GetRopeAxis();

        if (!rigTrackingActive)
        {
            lastRigPos = rigTransform.position;
            rigTrackingActive = true;
            return;
        }

        Vector3 rigPos = rigTransform.position;
        float deltaAlongAxis = Vector3.Dot(rigPos - lastRigPos, axis);
        lastRigPos = rigPos;

        // Convert meters moved along rope into UV offset
        ropeTexOffset += scrollAxis * (deltaAlongAxis * scrollMultiplier);

        // Apply per-renderer override (safer than touching shared material)
        ropeRenderer.GetPropertyBlock(mpb);
        mpb.SetVector(offsetId, new Vector4(ropeTexOffset.x, ropeTexOffset.y, 0f, 0f));
        ropeRenderer.SetPropertyBlock(mpb);
    }

    private Vector3 GetRopeAxis()
    {
        Vector3 axis = useForwardAxis
            ? (ropeAxisReference != null ? ropeAxisReference.forward : Vector3.forward)
            : (ropeAxisReference != null ? ropeAxisReference.up : Vector3.up);

        if (axis.sqrMagnitude < 1e-6f) axis = Vector3.up;
        return axis.normalized;
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
