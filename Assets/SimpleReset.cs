using System.Collections.Generic;
using UnityEngine;

public class SimpleReset : MonoBehaviour
{
    [Header("Objetos a registrar")]
    [SerializeField] private List<GameObject> objetos = new List<GameObject>();

    [Header("Opciones")]
    [Tooltip("Usar posiciones/rotaciones/escala en espacio LOCAL. Si est� apagado, se guardan/restauran en MUNDO.")]
    [SerializeField] private bool usarEspacioLocal = false;

    [Tooltip("Si existe Rigidbody, poner velocidad a 0 al reiniciar.")]
    [SerializeField] private bool resetearFisica = true;

    // Buffers
    private Vector3[] posiciones;
    private Quaternion[] rotaciones;
    private Vector3[] escalas; // guardaremos ESCALA tambi�n

    // ----------------- Ciclo de vida -----------------

    private void Start()
    {
        RegistrarEstadosIniciales();
    }

    /// <summary>
    /// Guarda las transformaciones actuales de los objetos listados.
    /// Llama esto si cambias la lista din�micamente en runtime.
    /// </summary>
    [ContextMenu("Registrar estados iniciales")]
    public void RegistrarEstadosIniciales()
    {
        int n = objetos.Count;
        posiciones = new Vector3[n];
        rotaciones = new Quaternion[n];
        escalas = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            var go = objetos[i];
            if (go == null) continue;

            var t = go.transform;

            if (usarEspacioLocal)
            {
                posiciones[i] = t.localPosition;
                rotaciones[i] = t.localRotation;
                escalas[i] = t.localScale;
            }
            else
            {
                posiciones[i] = t.position;
                rotaciones[i] = t.rotation;
                escalas[i] = t.lossyScale; // escala en mundo
            }
        }
    }

    /// <summary>
    /// Restaura posici�n, rotaci�n y escala originales.
    /// </summary>
    [ContextMenu("Reiniciar (Editor)")]
    public void Reiniciar()
    {
        if (posiciones == null || rotaciones == null || escalas == null ||
            posiciones.Length != objetos.Count)
        {
            // Por si la lista cambi� desde que guardamos
            RegistrarEstadosIniciales();
        }

        for (int i = 0; i < objetos.Count; i++)
        {
            var go = objetos[i];
            if (go == null) continue;

            var t = go.transform;

            if (usarEspacioLocal)
            {
                t.localPosition = posiciones[i];
                t.localRotation = rotaciones[i];
                t.localScale = escalas[i];
            }
            else
            {
                // Posici�n/rotaci�n en mundo
                t.SetPositionAndRotation(posiciones[i], rotaciones[i]);

                // Escala en mundo: calcular localScale que produzca esa lossyScale
                SetWorldScale(t, escalas[i]);
            }

            if (resetearFisica)
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }
            }
        }
    }

    // ----------------- Utilidades -----------------

    /// <summary>
    /// Ajusta localScale para obtener una escala en mundo espec�fica (worldScale),
    /// teniendo en cuenta la escala del padre.
    /// </summary>
    private static void SetWorldScale(Transform t, Vector3 worldScale)
    {
        var parent = t.parent;
        if (parent == null)
        {
            t.localScale = worldScale;
            return;
        }

        var p = parent.lossyScale;
        // Evita divisiones por cero
        float sx = p.x == 0f ? 1f : worldScale.x / p.x;
        float sy = p.y == 0f ? 1f : worldScale.y / p.y;
        float sz = p.z == 0f ? 1f : worldScale.z / p.z;
        t.localScale = new Vector3(sx, sy, sz);
    }
}
