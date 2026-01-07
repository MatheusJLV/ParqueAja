using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DesktopCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot; // Empty child (CameraPivot)
    [SerializeField] private Camera playerCamera;   // Main Camera

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private CharacterController controller;
    private float verticalVelocity;
    private float pitch; // up/down

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraPivot == null)
            Debug.LogError("[DesktopCharacterController] Falta asignar CameraPivot en el inspector.");

        if (playerCamera == null)
            playerCamera = Camera.main;

        LockCursor(true);
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();

        // Por si necesitas liberar el mouse en pruebas
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(false);
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float z = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        Vector3 horizontal = move * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // pega al suelo (anti “floating”)

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontal + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Yaw: gira el cuerpo (izq/der)
        transform.Rotate(Vector3.up * mouseX);

        // Pitch: gira el pivot (arriba/abajo)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
