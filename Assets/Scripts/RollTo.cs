using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// este script ya no se va a usar, queda ahi para borrar luego si por si las dudas

// Sistema de movimiento automático para objetos con Rigidbody que los desplaza desde un punto de salida hacia una meta
// usando física (MovePosition). Al llegar, el objeto recupera la gravedad y deja de ser cinemático.
public class RollTo : MonoBehaviour
{
    public GameObject salida;        // Punto de origen desde donde inicia el movimiento
    public GameObject meta;          // Punto de destino al que debe llegar el objeto
    public float speed = 5f;         // Velocidad de desplazamiento en unidades por segundo         // Velocidad de desplazamiento en unidades por segundo

    private Rigidbody rb;            // Referencia al componente Rigidbody del objeto
    //private bool wasKinematic;
    //private bool wasUseGravity;

    public float stopDistance = 0.1f;  // Distancia mínima para considerar que llegó a la meta

    private float delayTimer;          // Temporizador para retrasos (sin uso actual)          // Temporizador para retrasos (sin uso actual)

    // Inicializa la referencia al Rigidbody al despertar el componente
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[RollTo] No se encontr� Rigidbody en el objeto.");
        }
    }

    // Mueve el objeto hacia la meta usando física. Al llegar, restaura la gravedad y deshabilita el script.
    void FixedUpdate()
    {

        if (meta == null) return;

        Vector3 newPosition = Vector3.MoveTowards(
            rb.position,
            meta.transform.position,
            speed * Time.fixedDeltaTime
        );
        rb.MovePosition(newPosition);

        // Check distance after moving
        float distance = Vector3.Distance(rb.position, meta.transform.position);
        if (distance < stopDistance)
        {
            // Final correction to align transform and Rigidbody
            transform.position = meta.transform.position;
            rb.MovePosition(meta.transform.position);

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();

            this.enabled = false;
        }
    }

    /*void OnDisable()
    {
        if (salida != null)
        {
            var socket = salida.GetComponent<XRSocketInteractor>();
            if (socket != null)
            {
                socket.enabled = true;

                if (socket.interactablesSelected.Count > 0)
                {
                    IXRSelectInteractable interactable = socket.interactablesSelected[0];
                    socket.interactionManager.CancelInteractableSelection(interactable);
                    Debug.Log("[RollTo] Interactable forcibly deselected via CancelInteractableSelection.");
                }
            }

        }


        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.useGravity = wasUseGravity;
        }
    }




    void Update()
    {
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        if (meta == null) return;

        Vector3 destino = meta.transform.position;
        float distancia = Vector3.Distance(transform.position, destino);

        if (distancia > 0.01f)
        {
            // Movimiento suave usando Lerp con factor dependiente del tiempo
            float factor = 1f - Mathf.Exp(-speed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, destino, factor);
        }
        else
        {
            transform.position = destino;

            // Habilitar el socket en salida si existe
            if (salida != null)
            {
                var socket = salida.GetComponent<XRSocketInteractor>();
                if (socket != null)
                {
                    socket.enabled = true;
                    Debug.Log("[RollTo] Socket habilitado en salida: " + salida.name);
                }
                else
                {
                    Debug.LogWarning("[RollTo] XRSocketInteractor no encontrado en salida: " + salida.name);
                }
            }
            else
            {
                Debug.LogWarning("[RollTo] salida es null.");
            }

            // Desactivar este componente
            this.enabled = false;
        }
    }*/
}
