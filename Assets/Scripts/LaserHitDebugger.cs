using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class LaserHitDebugger : MonoBehaviour
{
    [Header("Near-Far Interactors")]
    [SerializeField] private NearFarInteractor leftInteractor;

    [Header("Follower Target")]
    [SerializeField] private Transform target; // Object to move & rotate with the laser tip

    [Header("Debug UI")]
    [SerializeField] private TMP_Text debugText;

    private void Update()
    {
        if (leftInteractor == null)
            return;

        // 1. Origin
        Vector3 origin = leftInteractor.transform.position;

        // 2. End point
        leftInteractor.TryGetCurveEndPoint(
            out Vector3 end,
            snapToSelectedAttachIfAvailable: false,
            snapToSnapVolumeIfAvailable: false);

        // 3. Forward direction
        Vector3 forward = (end - origin).normalized;

        // 4. Construct rotation basis
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;
        Quaternion rotation = Quaternion.LookRotation(forward, up);

        // === Apply to target object ===
        if (target != null)
        {
            target.position = end;      // Place at laser tip
            target.rotation = rotation; // Orient with laser direction
        }

        // Debug output (optional)
        if (debugText != null)
        {
            debugText.text =
                $"Left Laser:\n" +
                $" Origin: {origin}\n" +
                $" End: {end}\n" +
                $" Direction: {forward}\n" +
                $" Rotation (Euler): {rotation.eulerAngles}\n";
        }
    }
}

