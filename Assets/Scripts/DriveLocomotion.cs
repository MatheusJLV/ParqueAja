using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

// Sistema de locomoción para conducir en VR usando un volánte virtual como control de dirección
// Detecta entrada de mano derecha (botones primario/secundario) para acelerar/retroceder
// La rotación del volánte controla la orientación del movimiento
[RequireComponent(typeof(LocomotionSystem))]
public class DriveLocomotion : LocomotionProvider
{
    // Referencia al volante: su rotación local en eje Y determina la dirección del movimiento
    public Transform volante; // Steering wheel object
    // Velocidad de aceleración hacia adelante (unidades/segundo)
    public float acceleration = 1.5f;
    // Velocidad de aceleración hacia atrás/reversa (unidades/segundo)
    public float reverseAcceleration = 1.0f;
    // Límite de velocidad máxima (positiva o negativa)
    public float maxSpeed = 5f;
    // Multiplicador de sensibilidad para el ángulo del volánte
    public float steeringSensitivity = 1f;

    // Velocidad actual del jugador: rango [-maxSpeed, maxSpeed]
    // Negativa indica movimiento hacia atrás (reversa)
    private float currentSpeed = 0f;
    // Controlador de personaje para aplicar movimiento al reproductor VR
    private CharacterController driver;
    private Transform rigTransform;

    // Inicialización: obtiene referencias al CharacterController y al transform del origen XR
    // Estas referencias son necesarias para aplicar movimiento al jugador
    void Start()
    {
        rigTransform = system.xrOrigin.CameraFloorOffsetObject.transform;
        driver = system.xrOrigin.GetComponent<CharacterController>();
    }

    // Actualiza cada frame: lee entrada XR, calcula velocidad, aplica movimiento
    // Solo funciona si no hay otro proveedor de locomoción activo
    void Update()
    {
        // Only move if no other locomotion is active
        if (!CanBeginLocomotion()) return;

        // Lee entrada de mano derecha:
        // - Boto primario: acelera hacia adelante
        // - Botón secundario: acelera en reversa (hacia atrás)
        // Get right hand input
        bool rightPrimary = false;
        bool rightSecondary = false;
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimary);
        rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondary);

        // Calcula cambio de velocidad según botones presionados
        // delta puede ser positivo (acelerar) o negativo (reversa)
        float delta = 0f;

        if (rightPrimary)
            delta += acceleration * Time.deltaTime;
        if (rightSecondary)
            delta -= reverseAcceleration * Time.deltaTime;

        // Limita la velocidad al rango [-maxSpeed, maxSpeed] para evitar velocidades excesivas
        currentSpeed = Mathf.Clamp(currentSpeed + delta, -maxSpeed, maxSpeed);

        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            // Solo aplica movimiento si la velocidad es significativa (evita jitter)
            BeginLocomotion();

            // Lee rotación del volánte (eje Y local) para determinar dirección de movimiento
            // Get steering input from volante rotation
            float steering = volante != null ? volante.localEulerAngles.y : 0f;

            // Normalize to range [-180, 180]
            if (steering > 180f) steering -= 360f;

            Vector3 forward = Quaternion.Euler(0f, steering * steeringSensitivity, 0f) * rigTransform.forward;
            Vector3 motion = forward.normalized * currentSpeed * Time.deltaTime;

            driver.Move(motion);

            EndLocomotion();
        }
    }
}

