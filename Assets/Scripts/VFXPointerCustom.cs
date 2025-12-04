using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXPointerCustom : MonoBehaviour
{
    public VisualEffect staticFieldVFX;
    private Collider intruder1;
    public GameObject arcoElectrico;
    public List<GameObject> puntosRef = new List<GameObject>();


    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] OnTriggerEnter llamado con: {other.gameObject.name}");

        if (!other.CompareTag("Conductor"))
        {
            Debug.Log($"[VFXPointerCustom] {other.gameObject.name} no tiene el tag 'Conductor'. Se ignora.");
            return;
        }

        if (intruder1 != null && other == intruder1)
        {
            Debug.Log($"[VFXPointerCustom] {other.gameObject.name} ya está asignado como intruder. Se ignora.");
            return;
        }

        HandleIntruderEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] OnTriggerExit: {other.gameObject.name}");

        if (other == intruder1)
        {
            HandleIntruderExit(other);
        }
    }

    private void HandleIntruderEnter(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] Asignando {other.gameObject.name} como intruder1.");
        intruder1 = other;
        if (staticFieldVFX == null)
        {
            staticFieldVFX.SetBool("Atractor1", true);
            staticFieldVFX.SetVector3("IntruderPosition", intruder1.transform.position);
        }
        
        Debug.Log($"[VFXPointerCustom] VFX actualizado para {other.gameObject.name}.");

        if (arcoElectrico != null)
            arcoElectrico.SetActive(true);

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

    private void HandleIntruderExit(Collider other)
    {
        if (staticFieldVFX == null)
        {
            staticFieldVFX.SetBool("Atractor1", false);
            staticFieldVFX.SetVector3("IntruderPosition", Vector3.zero);
        }
        intruder1 = null;

        if (arcoElectrico != null)
            arcoElectrico.SetActive(false);
    }

    void OnTriggerStay(Collider other)
    {
        if (other == intruder1)
        {
            HandleIntruderStay();
        }
    }

    private void HandleIntruderStay()
    {
        // Asegura que hay suficientes puntos y que no son nulos
        if (puntosRef.Count >= 4 &&
            puntosRef[0] != null && puntosRef[1] != null &&
            puntosRef[2] != null && puntosRef[3] != null)
        {
            Vector3 pos1 = puntosRef[0].transform.position;
            Vector3 pos4 = puntosRef[3].transform.position;

            // El segundo objeto (índice 1) está más cerca del primero (por ejemplo, 25% del camino)
            puntosRef[1].transform.position = Vector3.Lerp(pos1, pos4, 0.25f);

            // El tercero (índice 2) está más cerca del cuarto (por ejemplo, 75% del camino)
            puntosRef[2].transform.position = Vector3.Lerp(pos1, pos4, 0.75f);
        }
    }


    void Update()
    {
        if (intruder1 != null)
            if (staticFieldVFX == null)
            {
                staticFieldVFX.SetVector3("IntruderPosition", intruder1.transform.position);
            }
    }
}
