using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

/*
 * DriveLocomotion:
 * Proporciona locomoción tipo conducción en VR, usando un volante para dirección
 * y botones del controlador para acelerar y reversa.
 */

[RequireComponent(typeof(LocomotionSystem))]
public class DriveLocomotion : LocomotionProvider
{
    public Transform volante; // Objeto del volante para dirección
    public float acceleration = 1.5f; // Aceleración hacia adelante
    public float reverseAcceleration = 1.0f; // Aceleración hacia atrás
    public float maxSpeed = 5f; // Velocidad máxima
    public float steeringSensitivity = 1f; // Sensibilidad de dirección

    private float currentSpeed = 0f; // Velocidad actual
    private CharacterController driver; // Controlador del personaje
    private Transform rigTransform; // Transform del rig XR

    void Start()
    {
        // Inicializar referencias al iniciar
        rigTransform = system.xrOrigin.CameraFloorOffsetObject.transform; // Obtener transform del rig
        driver = system.xrOrigin.GetComponent<CharacterController>(); // Obtener CharacterController
    }

    void Update()
    {
        // Procesar entrada y movimiento en cada frame
        // Only move if no other locomotion is active
        if (!CanBeginLocomotion()) return; // Verificar si se puede iniciar locomoción

        // Get right hand input
        bool rightPrimary = false;
        bool rightSecondary = false;
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand); // Obtener dispositivo de mano derecha
        rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimary); // Botón primario
        rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondary); // Botón secundario

        float delta = 0f; // Cambio en velocidad

        if (rightPrimary)
            delta += acceleration * Time.deltaTime; // Acelerar
        if (rightSecondary)
            delta -= reverseAcceleration * Time.deltaTime; // Reversar

        currentSpeed = Mathf.Clamp(currentSpeed + delta, -maxSpeed, maxSpeed); // Actualizar velocidad con clamp

        if (Mathf.Abs(currentSpeed) > 0.01f) // Si hay velocidad significativa
        {
            BeginLocomotion(); // Iniciar locomoción

            // Get steering input from volante rotation
            float steering = volante != null ? volante.localEulerAngles.y : 0f; // Obtener ángulo del volante

            // Normalize to range [-180, 180]
            if (steering > 180f) steering -= 360f; // Normalizar a [-180, 180]

            Vector3 forward = Quaternion.Euler(0f, steering * steeringSensitivity, 0f) * rigTransform.forward; // Dirección forward rotada
            Vector3 motion = forward.normalized * currentSpeed * Time.deltaTime; // Vector de movimiento

            driver.Move(motion); // Mover al controlador

            EndLocomotion(); // Finalizar locomoción
        }
    }
}

