using System.Collections.Generic;
using UnityEngine;

// Sistema de reinicio de transformaciones que guarda y restaura las posiciones, rotaciones y escalas iniciales
// de múltiples objetos. Soporta espacio local y mundial, con opción de resetear la física (velocidades).
public class SimpleReset : MonoBehaviour
{
    [Header("Objetos a registrar")]
    [SerializeField] private List<GameObject> objetos = new List<GameObject>();  // Lista de GameObjects cuyas transformaciones se guardarán y restaurarán

    [Header("Opciones")]
    [Tooltip("Usar posiciones/rotaciones/escala en espacio LOCAL. Si está apagado, se guardan/restauran en MUNDO.")]
    [SerializeField] private bool usarEspacioLocal = false;        // Determina si se usan coordenadas locales o mundiales

    [Tooltip("Si existe Rigidbody, poner velocidad a 0 al reiniciar.")]
    [SerializeField] private bool resetearFisica = true;           // Si es verdadero, resetea las velocidades de los Rigidbodies

    // Buffers
    private Vector3[] posiciones;      // Array que almacena las posiciones iniciales de cada objeto
    private Quaternion[] rotaciones;   // Array que almacena las rotaciones iniciales de cada objeto
    private Vector3[] escalas;         // Array que almacena las escalas iniciales de cada objeto

    // ----------------- Ciclo de vida -----------------

    // Guarda los estados iniciales de todos los objetos al inicio
    private void Start()
    {
        RegistrarEstadosIniciales();
    }

    // Guarda las transformaciones actuales de los objetos listados.
    // Llama esto si cambias la lista dinámicamente en runtime.
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

    // Restaura posición, rotación y escala originales de todos los objetos.
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

    // Ajusta localScale para obtener una escala en mundo específica (worldScale),
    // teniendo en cuenta la escala del padre.
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
