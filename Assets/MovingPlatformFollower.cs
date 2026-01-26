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

    [Header("Sticky / Stability")]
    [Tooltip("Small constant downward move while riding to keep ground contact stable.")]
    [SerializeField] private bool snapDownWhileRiding = true;

    [Tooltip("Snap-down distance per frame (meters). Keep small (0.01 - 0.05).")]
    [SerializeField] private float snapDownDistance = 0.02f;

    [Header("Optional")]
    [SerializeField] private bool carryRotation = false;

    private Transform platform;
    private Rigidbody platformRb;

    private Vector3 lastPlatformPos;
    private Quaternion lastPlatformRot;

    private float lastPlatformSeenTime = -999f;

    private void Awake()
    {
        if (cc == null)
            cc = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (cc == null) return;

        Transform detected = DetectPlatform(out Rigidbody detectedRb);

        // Acquire or refresh platform
        if (detected != null)
        {
            lastPlatformSeenTime = Time.time;

            if (platform != detected)
            {
                platform = detected;
                platformRb = detectedRb;

                // IMPORTANT:
                // Initialize tracking so next frame delta is correct.
                lastPlatformPos = platform.position;
                lastPlatformRot = platform.rotation;
            }
        }
        else
        {
            // Keep riding briefly even if detection flickers (critical for vertical motion start)
            if (Time.time - lastPlatformSeenTime > stickGraceTime)
            {
                platform = null;
                platformRb = null;
            }
        }

        if (platform == null) return;

        // Apply platform delta position
        Vector3 deltaPos = platform.position - lastPlatformPos;

        // Optional rotation carry
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

        // Move the CharacterController with the platform
        if (move.sqrMagnitude > 0f)
        {
            cc.Move(move);

        }


        // Sticky snap-down (helps keep stable contact especially when platform moves down)
        if (snapDownWhileRiding && snapDownDistance > 0f)
            cc.Move(Vector3.down * snapDownDistance);

        lastPlatformPos = platform.position;
        lastPlatformRot = platform.rotation;
    }

    private Transform DetectPlatform(out Rigidbody foundRb)
    {
        foundRb = null;

        // World-space CC center
        Vector3 worldCenter = transform.position + cc.center;

        // Bottom of capsule (slightly above bottom to avoid starting inside geometry too much)
        float bottomOffset = (cc.height * 0.5f) - cc.radius;
        Vector3 bottom = worldCenter + Vector3.down * bottomOffset;

        // Small upward nudge helps avoid starting the cast inside the floor
        bottom += Vector3.up * 0.03f;

        float radius = Mathf.Max(0.04f, cc.radius * 0.6f);
        float castDist = probeDistance + probeExtraDistance;

        // 1) Primary: SphereCast downward
        if (Physics.SphereCast(bottom, radius, Vector3.down, out RaycastHit hit, castDist, platformLayers, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.collider.attachedRigidbody;

            // Prefer RB transform if present
            if (rb != null)
            {
                foundRb = rb;
                return rb.transform;
            }

            return hit.collider.transform;
        }

        // 2) Fallback: OverlapSphere (VERY important for vertical moving platforms)
        // This catches cases where the platform moved UP into the controller and SphereCast fails.
        Vector3 overlapPos = bottom + Vector3.down * 0.02f;
        Collider[] overlaps = Physics.OverlapSphere(overlapPos, radius, platformLayers, QueryTriggerInteraction.Ignore);

        float bestDot = -1f;
        Transform best = null;
        Rigidbody bestRb = null;

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider c = overlaps[i];
            if (c == null) continue;

            // Prefer something "below" us
            Vector3 dir = (c.bounds.center - worldCenter).normalized;
            float dot = Vector3.Dot(dir, Vector3.down);

            if (dot > bestDot)
            {
                bestDot = dot;

                Rigidbody rb = c.attachedRigidbody;
                bestRb = rb;
                best = (rb != null) ? rb.transform : c.transform;
            }
        }

        if (best != null)
        {
            foundRb = bestRb;
            return best;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (cc == null) return;

        Gizmos.color = Color.yellow;

        Vector3 worldCenter = transform.position + cc.center;
        float bottomOffset = (cc.height * 0.5f) - cc.radius;
        Vector3 bottom = worldCenter + Vector3.down * bottomOffset;
        bottom += Vector3.up * 0.03f;

        float radius = Mathf.Max(0.04f, cc.radius * 0.6f);
        Gizmos.DrawWireSphere(bottom + Vector3.down * 0.02f, radius);
    }
#endif
}
