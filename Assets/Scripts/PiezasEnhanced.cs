using System.Collections.Generic;
using UnityEngine;

// Gestor de piezas: almacena posiciones/rotaciones originales y permite resetear a estado inicial.
public class PiezasEnhanced : MonoBehaviour
{
    [Header("Parent objects that contain piezas as children")]
    public List<GameObject> piezasParents = new List<GameObject>(); // Padres que contienen piezas como hijos

    // Almacenamiento interno de transformaciones originales
    // Estructura de datos para guardar transform local de una pieza
    private class TransformData
    {
        public Vector3 localPosition;   // Posición local original
        public Quaternion localRotation; // Rotación local original
        public Vector3 localScale;      // Escala local original

        public TransformData(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            localPosition = pos;
            localRotation = rot;
            localScale = scale;
        }
    }

    // Dictionary: child transform -> stored data
    private Dictionary<Transform, TransformData> originalTransforms = new Dictionary<Transform, TransformData>();


    // Almacena los transforms originales de todas las piezas al iniciar.
    private void Start()
    {
        StoreOriginalTransforms();
    }

    // Recorre padres e hijos, guardando posición/rotación/escala original de cada pieza.
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

    // Restaura todas las piezas a su posición/rotación/escala original.
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
