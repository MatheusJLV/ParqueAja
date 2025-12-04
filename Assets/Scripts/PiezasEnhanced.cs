using System.Collections.Generic;
using UnityEngine;

public class PiezasEnhanced : MonoBehaviour
{
    [Header("Parent objects that contain piezas as children")]
    public List<GameObject> piezasParents = new List<GameObject>();

    // Internal storage for original transforms
    private class TransformData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public TransformData(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            localPosition = pos;
            localRotation = rot;
            localScale = scale;
        }
    }

    // Dictionary: child transform -> stored data
    private Dictionary<Transform, TransformData> originalTransforms = new Dictionary<Transform, TransformData>();


    private void Start()
    {
        StoreOriginalTransforms();
    }

    private void StoreOriginalTransforms()
    {
        originalTransforms.Clear();

        foreach (GameObject parent in piezasParents)
        {
            if (parent == null) continue;

            foreach (Transform child in parent.transform)
            {
                if (!originalTransforms.ContainsKey(child))
                {
                    originalTransforms[child] = new TransformData(
                        child.localPosition,
                        child.localRotation,
                        child.localScale
                    );
                }
            }
        }

        Debug.Log($"[PiezasEnhanced] Stored transforms for {originalTransforms.Count} child piezas.");
    }

    public void ResetPiezas()
    {
        foreach (var kvp in originalTransforms)
        {
            Transform child = kvp.Key;
            TransformData data = kvp.Value;

            if (child != null)
            {
                child.localPosition = data.localPosition;
                child.localRotation = data.localRotation;
                child.localScale = data.localScale;
            }
        }

        Debug.Log("[PiezasEnhanced] ResetPiezas completed.");
    }
}
