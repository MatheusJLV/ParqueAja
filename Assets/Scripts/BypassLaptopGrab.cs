using UnityEngine;

public class BypassLaptopGrab : MonoBehaviour
{
    [Header("Configuración de Agarre")]
    public float distanciaMaxRaycast = 4f;
    public float velocidadZoom = 2f;
    public float velocidadRotacion = 300f;
    public float rigidezAgarre = 25f;      // ¡NUEVO! Qué tan "fuerte" sostiene tu mano el objeto
    public Transform puntoDeAgarre;

    [Header("Límites del Brazo Virtual")]
    public float distanciaMinimaZoom = 0.6f;
    public float distanciaMaximaZoom = 2.0f;

    private Rigidbody objetoSostenido;

    void Update()
    {
        // 1. Clic Izquierdo (Agarrar)
        if (Input.GetMouseButtonDown(0))
        {
            IntentarAgarrar();
        }

        // 2. Soltar
        if (Input.GetMouseButtonUp(0))
        {
            SoltarObjeto();
        }

        // 3. Manejar el Scroll y Rotación (Aplicado al Punto Invisible)
        if (objetoSostenido != null)
        {
            ManejarInteraccionConScroll();
        }
    }

    void FixedUpdate()
    {
        // ¡LA MAGIA DE LAS FÍSICAS! (Se ejecuta sincronizado con el motor de colisiones)
        if (objetoSostenido != null)
        {
            // A) Perseguir la Posición sin atravesar paredes
            Vector3 direccion = puntoDeAgarre.position - objetoSostenido.position;
            objetoSostenido.linearVelocity = direccion * rigidezAgarre;

            // B) Perseguir la Rotación (Enderezarse) sin atravesar paredes
            Quaternion diferenciaRotacion = puntoDeAgarre.rotation * Quaternion.Inverse(objetoSostenido.rotation);
            diferenciaRotacion.ToAngleAxis(out float angulo, out Vector3 eje);

            // Corrección matemática para que gire por el camino más corto
            if (angulo > 180f) angulo -= 360f;

            if (angulo != 0)
            {
                // Aplicamos torsión física (Torque) para que copie el giro del Punto de Agarre
                objetoSostenido.angularVelocity = (eje * angulo * Mathf.Deg2Rad) * rigidezAgarre;
            }
        }
    }

    void ManejarInteraccionConScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            // Ahora giramos el PUNTO DE AGARRE, no el objeto directamente. El objeto lo seguirá.
            if (Input.GetKey(KeyCode.Q))
            {
                puntoDeAgarre.RotateAround(puntoDeAgarre.position, transform.up, -scroll * velocidadRotacion);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                puntoDeAgarre.RotateAround(puntoDeAgarre.position, transform.right, scroll * velocidadRotacion);
            }
            else
            {
                float nuevaDistancia = puntoDeAgarre.localPosition.z + (scroll * velocidadZoom);
                nuevaDistancia = Mathf.Clamp(nuevaDistancia, distanciaMinimaZoom, distanciaMaximaZoom);
                puntoDeAgarre.localPosition = new Vector3(0, 0, nuevaDistancia);
            }
        }
    }

    void IntentarAgarrar()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, distanciaMaxRaycast))
        {
            Rigidbody rb = hit.transform.GetComponent<Rigidbody>();

            if (rb != null && !rb.isKinematic)
            {
                objetoSostenido = rb;

                // 1. Desactivamos la gravedad, pero MANTENEMOS isKinematic = false para que haya choques
                objetoSostenido.useGravity = false;

                // 2. Colocamos el Punto de Agarre a la distancia en la que interceptamos el objeto
                float distInicial = Vector3.Distance(transform.position, objetoSostenido.position);
                distInicial = Mathf.Clamp(distInicial, distanciaMinimaZoom, distanciaMaximaZoom);

                puntoDeAgarre.localPosition = new Vector3(0, 0, distInicial);

                // 3. Le decimos al Punto de Agarre que mire "recto" hacia nosotros
                puntoDeAgarre.localRotation = Quaternion.identity;
            }
        }
    }

    void SoltarObjeto()
    {
        if (objetoSostenido != null)
        {
            // Restablecemos físicas de caída ("La cura lunar")
            objetoSostenido.useGravity = true;
            objetoSostenido.linearDamping = 0f;
            objetoSostenido.angularDamping = 0.05f;

            objetoSostenido = null;
        }
    }
}