using UnityEngine;

// este script ya no se va a usar, queda ahi para borrar luego si por si las dudas

public class MoveTo : MonoBehaviour
{
    /*public Transform target;
    public float speed = 2f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Asegurate de que sea kinemático
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 newPosition = Vector3.MoveTowards(
            rb.position,
            target.position,
            speed * Time.fixedDeltaTime
        );
        rb.MovePosition(newPosition);
    }*/

    public Transform target;            // Objetivo hacia donde moverse
    public float force = 10f;           // Magnitud de la fuerza aplicada; ajustar según masa/drag
    public float stopDistance = 0.1f;   // Distancia mínima para detener el movimiento

    private Rigidbody rb;

    // Obtiene el Rigidbody y lo configura como dinámico para poder aplicar fuerzas.
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false; // Debe ser dinámico para usar AddForce
    }

    // Cada frame físico: calcula dirección hacia el objetivo y aplica fuerza si no está cerca.
    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 direction = (target.position - rb.position);
        float distance = direction.magnitude;
        // Si llegó al destino, detén la velocidad
        if (distance < stopDistance)
        {
            rb.linearVelocity = Vector3.zero; // Detener el objeto
            return;
        }

        // Aplica fuerza en la dirección normalizada del objetivo
        direction.Normalize();
        rb.AddForce(direction * force, ForceMode.Force);
    }
}
