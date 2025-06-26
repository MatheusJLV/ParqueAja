using UnityEngine;
using UnityEngine.VFX;

public class VFXPointerCustom : MonoBehaviour
{
    public VisualEffect staticFieldVFX;
    private Collider intruder1;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] OnTriggerEnter llamado con: {other.gameObject.name}");

        // Etapa 1: Validaci�n de tag
        if (!other.CompareTag("Conductor"))
        {
            Debug.Log($"[VFXPointerCustom] {other.gameObject.name} no tiene el tag 'Conductor'. Se ignora.");
            return;
        }
        Debug.Log($"[VFXPointerCustom] {other.gameObject.name} tiene el tag 'Conductor'.");

        // Etapa 2: Validaci�n de duplicado
        if (intruder1 != null && other == intruder1)
        {
            Debug.Log($"[VFXPointerCustom] {other.gameObject.name} ya est� asignado como intruder. Se ignora.");
            return;
        }
        Debug.Log($"[VFXPointerCustom] {other.gameObject.name} no est� asignado como intruder. Continuando.");

        // Etapa 3: Asignaci�n y activaci�n de VFX
        if (intruder1 == null)
        {
            Debug.Log($"[VFXPointerCustom] Asignando {other.gameObject.name} como intruder1.");
            intruder1 = other;
            staticFieldVFX.SetBool("Atractor1", true);
            staticFieldVFX.SetVector3("IntruderPosition", intruder1.transform.position);
            Debug.Log($"[VFXPointerCustom] VFX actualizado para {other.gameObject.name}.");

            // Etapa 4: Intento de cargar el carrier
            var carrier = intruder1.GetComponent<VFXCarrier>();
            if (carrier != null)
            {
                carrier.Charge();
                Debug.Log($"[VFXPointerCustom] Charge() llamado en {intruder1.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[VFXPointerCustom] {intruder1.gameObject.name} no tiene componente VFXCarrier.");
            }
        }
        else
        {
            Debug.Log($"[VFXPointerCustom] intruder1 ya est� asignado, no se realiza ninguna acci�n.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] OnTriggerExit: {other.gameObject.name}");

        if (other == intruder1)
        {
            staticFieldVFX.SetBool("Atractor1", false);
            staticFieldVFX.SetVector3("IntruderPosition", Vector3.zero);
            intruder1 = null;
        }
    }

    void Update()
    {
        if (intruder1 != null)
            staticFieldVFX.SetVector3("IntruderPosition", intruder1.transform.position);
    }
}
