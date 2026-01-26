using UnityEngine;

public class MovingPlatformFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController cc;

    [Header("Detection")]
    [SerializeField] private LayerMask platformLayers = ~0;

    [Tooltip("How far below the CharacterController bottom we probe for a platform.")]
    [SerializeField] private float probeDistance = 0.25f;

    [Tooltip("Extra forgiveness for stairs/small platforms.")]
    [SerializeField] private float probeExtraDistance = 0.15f;

    [Tooltip("How long we keep carrying after losing contact for 1-2 frames.")]
    [SerializeField] private float stickGraceTime = 0.20f;

    [Header("Optional")]
    [SerializeField] private bool carryRotation = false;

    private Transform platform;
    private Vector3 lastPlatformPos;
    private Quaternion lastPlatformRot;

    private float lastPlatformSeenTime;

    private void Awake()
    {
        if (cc == null)
            cc = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (cc == null) return;

        Transform detected = DetectPlatform();

        // If we see a platform, acquire/refresh it
        if (detected != null)
        {
            lastPlatformSeenTime = Time.time;

            if (platform != detected)
            {
                platform = detected;
                lastPlatformPos = platform.position;
                lastPlatformRot = platform.rotation;
            }
        }
        else
        {
            // If we stop seeing the platform, keep it for a short grace time
            if (Time.time - lastPlatformSeenTime > stickGraceTime)
                platform = null;
        }

        if (platform == null) return;

        // Apply platform delta
        Vector3 deltaPos = platform.position - lastPlatformPos;
        Vector3 deltaRotMove = Vector3.zero;

        if (carryRotation)
        {
            Quaternion rotDelta = platform.rotation * Quaternion.Inverse(lastPlatformRot);
            Vector3 relative = transform.position - platform.position;
            relative = rotDelta * relative;
            Vector3 rotatedPos = platform.position + relative;
            deltaRotMove = rotatedPos - transform.position;
        }

        Vector3 move = deltaPos + deltaRotMove;

        if (move.sqrMagnitude > 0f)
            cc.Move(move);

        lastPlatformPos = platform.position;
        lastPlatformRot = platform.rotation;
    }

    private Transform DetectPlatform()
    {
        // --- Compute the CharacterController bottom sphere center ---
        // CC center is relative to transform, so world center is:
        Vector3 worldCenter = transform.position + cc.center;

        // Bottom of capsule (slightly above bottom to avoid starting inside geometry)
        float bottomOffset = (cc.height * 0.5f) - cc.radius;
        Vector3 bottom = worldCenter + Vector3.down * bottomOffset;

        bottom += Vector3.up * 0.03f; // 3 cm up


        float radius = Mathf.Max(0.04f, cc.radius * 0.6f);
        float castDist = probeDistance + probeExtraDistance;

        // SphereCast down from the CC bottom to find what we're standing on
        if (Physics.SphereCast(bottom, radius, Vector3.down, out RaycastHit hit, castDist, platformLayers, QueryTriggerInteraction.Ignore))
        {
            var rb = hit.collider.attachedRigidbody;
            if (rb != null && rb.isKinematic)
                return rb.transform;

            // Prefer Rigidbody transform if present (better for moving RB platforms)
            /*if (hit.collider.attachedRigidbody != null)
                return hit.collider.attachedRigidbody.transform;

            return hit.collider.transform;*/
        }

        return null;
    }
}

