using UnityEngine;

public class PlayerModeManager : MonoBehaviour
{
    [Header("1. Componentes VR a deshabilitar en PC")]
    public MonoBehaviour trackedPoseDriver;
    public GameObject leftController;
    public GameObject rightController;

    [Header("2. Componentes PC a habilitar en PC")]
    public MonoBehaviour laptopMovement;
    public MonoBehaviour laptopLook;
    public MonoBehaviour laptopGrab;

    [Header("3. Interfaz de Usuario (UI)")]
    public GameObject reticulaUI; // <--- ¡NUEVA VARIABLE! Arrastra aquí tu Canvas de la retícula

    private HardwareDetector detector;

    void Start()
    {
        detector = GetComponent<HardwareDetector>();
        AplicarBypass();
    }

    void AplicarBypass()
    {
        if (detector.isVRActive)
        {
            // --- MODO VR ---
            if (trackedPoseDriver != null) trackedPoseDriver.enabled = true;
            if (leftController != null) leftController.SetActive(true);
            if (rightController != null) rightController.SetActive(true);

            if (laptopMovement != null) laptopMovement.enabled = false;
            if (laptopLook != null) laptopLook.enabled = false;
            if (laptopGrab != null) laptopGrab.enabled = false;

            // Apagamos la cruz en VR
            if (reticulaUI != null) reticulaUI.SetActive(false);
        }
        else
        {
            // --- MODO LAPTOP (BYPASS) ---
            if (trackedPoseDriver != null) trackedPoseDriver.enabled = false;

            if (leftController != null) leftController.SetActive(false);
            if (rightController != null) rightController.SetActive(false);

            if (laptopMovement != null) laptopMovement.enabled = true;
            if (laptopLook != null) laptopLook.enabled = true;
            if (laptopGrab != null) laptopGrab.enabled = true;

            // Encendemos la cruz en Laptop
            if (reticulaUI != null) reticulaUI.SetActive(true);
        }
    }
}