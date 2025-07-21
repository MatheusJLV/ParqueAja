using UnityEngine;

public class CanicaSolarScript : MonoBehaviour
{
    public GameObject foco; // Asigna el foco desde el editor
    public float fuerzaMultiplicador = 10f; // Ajusta la fuerza desde el editor

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("CanicaSolarScript requiere un Rigidbody en el mismo GameObject.");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("PlacaSolar") && foco != null && rb != null)
        {
            // Limpiar fuerzas actuales
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Calcula la dirección normalizada hacia el foco
            Vector3 direccion = (foco.transform.position - transform.position).normalized;
            // Aplica la fuerza en esa dirección
            rb.AddForce(direccion * fuerzaMultiplicador, ForceMode.Impulse);
        }
    }
}
