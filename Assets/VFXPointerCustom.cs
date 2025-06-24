using UnityEngine;
using UnityEngine.VFX;

public class VFXPointerCustom : MonoBehaviour
{
    public VisualEffect staticFieldVFX;
    public Transform intruderTip;
    public Transform intruderTip2;

    void Update()
    {
        staticFieldVFX.SetVector3("IntruderPosition", intruderTip.position);
        staticFieldVFX.SetVector3("IntruderPosition2", intruderTip2.position);
    }

}
