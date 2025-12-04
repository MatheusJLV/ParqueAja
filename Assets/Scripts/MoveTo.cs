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
        rb.isKinematic = true; // Aseg�rate de que sea kinem�tico
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

    public Transform target;
    public float force = 10f; // Ajusta este valor seg�n la masa y el drag del Rigidbody
    public float stopDistance = 0.1f; // Distancia m�nima para dejar de aplicar fuerza

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false; // Debe ser din�mico para usar AddForce
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 direction = (target.position - rb.position);
        float distance = direction.magnitude;
        if (distance < stopDistance)
        {
            rb.linearVelocity = Vector3.zero; // Detener el objeto
            return;
        }

        direction.Normalize();
        rb.AddForce(direction * force, ForceMode.Force);
    }
}
