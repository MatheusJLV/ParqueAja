using UnityEngine;

// Sistema de tablas interactivas que gestiona la selección y el estado visual de objetos mediante el cambio de materiales.
// Permite alternar entre dos estados (presionado/no presionado) cambiando el material de la agarradera.
public class TablasScript : MonoBehaviour
{
    [SerializeField] public int valor;                     // Valor asociado a esta tabla
    [SerializeField] public bool presionado = false;       // Estado de si la tabla está presionada
    [SerializeField] private Material metal;               // Material por defecto de la agarradera
    [SerializeField] private Material metalAlt;            // Material alternativo cuando está presionada
    [SerializeField] private GameObject agarradera;        // GameObject de la agarradera que cambia de material

    // Alterna el estado de selección de la tabla, cambiando el material de la agarradera entre metal y metalAlt
    public void Seleccion()
    {
        if (agarradera == null) return;

        var renderer = agarradera.GetComponent<Renderer>();
        if (renderer == null) return;

        if (!presionado)
        {
            presionado = true;
            renderer.material = metalAlt;
        }
        else
        {
            presionado = false;
            renderer.material = metal;
        }
    }
}
