using UnityEngine;

// Trigger de meta: avisa al funnel cuando un objeto entra en la zona final.
public class MetaZoneTrigger : MonoBehaviour
{
    public funnelScript funnel; // Referencia al funnel; asignar en el inspector

    // Cuando un collider entra, notifica al funnel para finalizar con ese objeto.
    private void OnTriggerEnter(Collider other)
    {
        if (funnel != null)
        {
            funnel.Finalizar(other.gameObject);
        }
    }
}
