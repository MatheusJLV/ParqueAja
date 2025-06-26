using UnityEngine;
using UnityEngine.VFX;

public class VFXCarrier : MonoBehaviour
{
    public VisualEffect carrierVFX;
    private Collider intruder1;
    public bool isCharged = false;

    private void Start()
    {
        if (carrierVFX != null)
            carrierVFX.Stop();
    }

    public void Charge()
    {
        isCharged = true;
        if (carrierVFX != null)
            carrierVFX.Play();
    }

    public void Discharge(Collider other)
    {
        if (!other.CompareTag("Conductor"))
            return;

        if (intruder1 != null && other == intruder1)
        {
            Debug.Log($"[VFXCarrier] {other.gameObject.name} ya está asignado como intruder.");
            return;
        }

        if (intruder1 == null)
        {
            intruder1 = other;
            if (carrierVFX != null)
            {
                carrierVFX.SetBool("Atractor1", true);
                carrierVFX.SetVector3("IntruderPosition", intruder1.transform.position);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[VFXCarrier] OnTriggerEnter: {other.gameObject.name}");

        if (!isCharged || !other.CompareTag("Conductor"))
            return;

        Discharge(other);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[VFXCarrier] OnTriggerExit: {other.gameObject.name}");

        isCharged = false;
        if (carrierVFX != null)
            carrierVFX.Stop();

        if (other == intruder1)
        {
            if (carrierVFX != null)
            {
                carrierVFX.SetBool("Atractor1", false);
                carrierVFX.SetVector3("IntruderPosition", Vector3.zero);
            }
            intruder1 = null;
        }
    }

    void Update()
    {
        if (intruder1 != null && carrierVFX != null)
            carrierVFX.SetVector3("IntruderPosition", intruder1.transform.position);
    }
}
