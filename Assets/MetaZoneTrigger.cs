using UnityEngine;

public class MetaZoneTrigger : MonoBehaviour
{
    public funnelScript funnel; // Asigna esta referencia en el inspector

    private void OnTriggerEnter(Collider other)
    {
        if (funnel != null)
        {
            funnel.Finalizar(other.gameObject);
        }
    }
}
