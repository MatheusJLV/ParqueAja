using UnityEngine;

public class TablasScript : MonoBehaviour
{
    [SerializeField] public int valor;
    [SerializeField] public bool presionado =false;
    [SerializeField] private Material metal;
    [SerializeField] private Material metalAlt;
    [SerializeField] private GameObject agarradera;

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
