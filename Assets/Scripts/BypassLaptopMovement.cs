using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BypassLaptopMovement : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 5f;
    public float alturaSalto = 1.5f;
    public float gravedad = -9.81f;

    [Header("Referencias")]
    public Transform camaraJugador;

    private CharacterController controller;
    private Vector3 velocidadVertical; // Renombrado para mayor claridad
    private bool enElSuelo;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. REVISAR SUELO AL INICIO DEL FOTOGRAMA
        enElSuelo = controller.isGrounded;
        if (enElSuelo && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
        }

        // 2. CALCULAR MOVIMIENTO HORIZONTAL (WASD)
        float movX = Input.GetAxis("Horizontal");
        float movZ = Input.GetAxis("Vertical");

        Vector3 camForward = camaraJugador.forward;
        Vector3 camRight = camaraJugador.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Vector X y Z
        Vector3 movimientoHorizontal = (camRight * movX) + (camForward * movZ);
        movimientoHorizontal *= velocidad;

        // 3. CALCULAR SALTO Y GRAVEDAD (Eje Y)
        if (Input.GetButtonDown("Jump") && enElSuelo)
        {
            velocidadVertical.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
        }
        velocidadVertical.y += gravedad * Time.deltaTime;

        // 4. UNIFICAR Y MOVER (El secreto para que isGrounded no falle)
        Vector3 movimientoFinal = movimientoHorizontal + velocidadVertical;

        // UNA SOLA LLAMADA POR FOTOGRAMA
        controller.Move(movimientoFinal * Time.deltaTime);
    }
}