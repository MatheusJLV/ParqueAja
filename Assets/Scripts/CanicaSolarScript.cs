using UnityEngine;

/* Este script controla el comportamiento de una canica que, al
 colisionar con una "PlacaSolar", es impulsada hacia un "foco".
 Se utiliza física real de Unity mediante Rigidbody.*/
public class CanicaSolarScript : MonoBehaviour
{
    public GameObject foco; // Asigna el foco desde el editor
    public float fuerzaMultiplicador = 10f; // Ajusta la fuerza desde el editor

    private Rigidbody rb;// Referencia interna al componente Rigidbody

    void Start()
    {
        // Obtiene el componente Rigidbody del objeto
        rb = GetComponent<Rigidbody>();

        // Si no se encuentra, muestra una advertencia
        if (rb == null)
        {
            Debug.LogWarning("CanicaSolarScript requiere un Rigidbody en el mismo GameObject.");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        /* Verifica que la colisión sea con una placa solar
         y que el foco y el Rigidbody estén correctamente asignados.*/
        if (collision.gameObject.CompareTag("PlacaSolar") && foco != null && rb != null)
        {
            // Limpiar fuerzas actuales
            // Reinicia el movimiento actual para evitar acumulación de fuerzas
            rb.linearVelocity = Vector3.zero; // (corrección: 'linearVelocity' no existe en Unity)
            rb.angularVelocity = Vector3.zero;  // Detiene rotación

            // Calcula la dirección normalizada hacia el foco
            Vector3 direccion = (foco.transform.position - transform.position).normalized;
            // Aplica la fuerza en esa dirección
            rb.AddForce(direccion * fuerzaMultiplicador, ForceMode.Impulse);
        }
    }
}
