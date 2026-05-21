using UnityEngine;

public class BypassLaptopLook : MonoBehaviour
{
    public float sensibilidadMouse = 2f;
    private Transform cuerpoJugador;
    private float rotacionX = 0f;

    void Start()
    {
        // El cuerpo que va a rotar es el XR Origin (padre de la cámara)
        cuerpoJugador = transform.parent;
        Cursor.lockState = CursorLockMode.Locked; // Oculta el mouse
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadMouse;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadMouse;

        // Mirar arriba y abajo (rota la cámara)
        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);

        // Girar izquierda y derecha (rota el cuerpo entero)
        if (cuerpoJugador != null)
        {
            cuerpoJugador.Rotate(Vector3.up * mouseX);
        }
    }
}