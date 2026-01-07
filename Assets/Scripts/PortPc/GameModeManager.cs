using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public GameObject xrRigRoot;
    public GameObject desktopRigRoot;

    void Awake()
    {
        bool useVR = UnityEngine.XR.XRSettings.isDeviceActive; // simple
        SetMode(useVR);
    }

    public void SetMode(bool vr)
    {
        xrRigRoot.SetActive(vr);
        desktopRigRoot.SetActive(!vr);
    }
}
