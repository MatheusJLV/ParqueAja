using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// este script ya no se va a usar, queda ahi para borrar luego si por si las dudas

public class RollTo : MonoBehaviour
{
    public GameObject salida;
    public GameObject meta;
    public float speed = 5f;

    private Rigidbody rb;
    //private bool wasKinematic;
    //private bool wasUseGravity;

    public float stopDistance = 0.1f;

    private float delayTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[RollTo] No se encontró Rigidbody en el objeto.");
        }
    }

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
