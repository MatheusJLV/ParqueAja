using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// Activa automáticamente el XR Rig o el DesktopCharacter según el dispositivo.
/// Colócalo en un GameObject vacío en tu escena (por ejemplo "GameInitializer").
/// </summary>
public class PlayerBootstrap : MonoBehaviour
{
    [Header("Referencias de jugador")]
    [Tooltip("Arrastra aquí el XR Origin (XR Rig)")]
    public GameObject xrRig;

    [Tooltip("Arrastra aquí el DesktopCharacter o Player PC")]
    public GameObject desktopCharacter;

    void Awake()
    {
        // Verificar que las referencias estén asignadas
        if (xrRig == null || desktopCharacter == null)
        {
            Debug.LogError("⚠️ PlayerBootstrap: Faltan referencias a XR Rig o DesktopCharacter.");
            return;
        }

        // Detectar si un sistema XR está corriendo
        bool isXR = IsXRActive();

        if (isXR)
        {
            // Activar el modo VR
            xrRig.SetActive(true);
            desktopCharacter.SetActive(false);
            Debug.Log("🎮 Modo XR detectado → Activando XR Rig");
        }
        else
        {
            // Activar el modo Desktop
            xrRig.SetActive(false);
            desktopCharacter.SetActive(true);
            Debug.Log("💻 No se detectó XR → Activando DesktopCharacter");
        }
    }

    /// <summary>
    /// Devuelve true si hay un sistema XR inicializado y un loader activo.
    /// </summary>
    private bool IsXRActive()
    {
        var xrSettings = XRGeneralSettings.Instance;
        if (xrSettings == null) return false;

        var xrManager = xrSettings.Manager;
        if (xrManager == null) return false;

        return xrManager.isInitializationComplete && xrManager.activeLoader != null;
    }
}
