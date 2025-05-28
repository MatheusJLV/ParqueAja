using UnityEngine;

public class CajaSimetria : MonoBehaviour
{
    [Header("Position axis inversion (1 = normal, -1 = mirrored)")]
    public int xAxis = 1;
    public int yAxis = 1;
    public int zAxis = 1;

    [Header("Rotation mirror")]
    public bool mirrorRotation = true;
    public Vector3 rotationMirrorNormal = Vector3.right; // Default mirror across X plane

    [Header("References")]
    public Transform target;
    public Transform referencia;
    public bool useGlobalSpace = true;

    void Update()
    {
        if (target == null) return;

        // === POSITION ===
        if (useGlobalSpace)
        {
            Vector3 center = referencia != null ? referencia.position : Vector3.zero;
            Vector3 offset = transform.position - center;

            Vector3 mirroredOffset = new Vector3(
                offset.x * xAxis,
                offset.y * yAxis,
                offset.z * zAxis
            );

            target.position = center + mirroredOffset;
        }
        else
        {
            Vector3 localPos = transform.localPosition;
            target.localPosition = new Vector3(
                localPos.x * xAxis,
                localPos.y * yAxis,
                localPos.z * zAxis
            );
        }

        // === ROTATION ===
        if (mirrorRotation)
        {
            Quaternion sourceRotation = useGlobalSpace ? transform.rotation : transform.localRotation;

            // Reflect rotation across a plane defined by a normal
            Quaternion mirroredRotation = MirrorRotationAcrossPlane(sourceRotation, rotationMirrorNormal.normalized);

            if (useGlobalSpace)
                target.rotation = mirroredRotation;
            else
                target.localRotation = mirroredRotation;
        }
    }

    // Mirrors a rotation across a plane (defined by its normal vector)
    Quaternion MirrorRotationAcrossPlane(Quaternion original, Vector3 planeNormal)
    {
        // Convert rotation to forward & up vectors
        Vector3 fwd = original * Vector3.forward;
        Vector3 up = original * Vector3.up;

        // Reflect both vectors across the mirror plane
        fwd = Vector3.Reflect(fwd, planeNormal);
        up = Vector3.Reflect(up, planeNormal);

        // Build the mirrored rotation from the reflected vectors
        return Quaternion.LookRotation(fwd, up);
    }
}