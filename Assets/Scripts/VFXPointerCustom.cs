using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

// Sistema de puntero VFX personalizado que gestiona campos eléctricos estáticos y arcos eléctricos
// Detecta conductores dentro de su trigger y activa efectos visuales y carga VFX
public class VFXPointerCustom : MonoBehaviour
{
    public VisualEffect staticFieldVFX;    // Efecto visual del campo eléctrico estático
    private Collider intruder1;            // Primer conductor que entra en el trigger
    public GameObject arcoElectrico;       // GameObject del arco eléctrico a activar
    public List<GameObject> puntosRef = new List<GameObject>();  // Puntos de referencia para interpolar el arco eléctrico  // Puntos de referencia para interpolar el arco eléctrico


    // Detecta cuando un conductor entra al trigger y activa el sistema VFX
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
            Debug.Log($"[VFXPointerCustom] {other.gameObject.name} ya est� asignado como intruder. Se ignora.");
            return;
        }

        HandleIntruderEnter(other);
    }

    // Detecta cuando un conductor sale del trigger y desactiva el sistema VFX
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] OnTriggerExit: {other.gameObject.name}");

        if (other == intruder1)
        {
            HandleIntruderExit(other);
        }
    }

    // Gestiona la entrada de un conductor: activa VFX, arco eléctrico y carga el VFXCarrier
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

        // Activar el arco eléctrico visual
        if (arcoElectrico != null)
            arcoElectrico.SetActive(true);

        // Intentar cargar el componente VFXCarrier del conductor
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

    // Gestiona la salida de un conductor: desactiva VFX y arco eléctrico
    private void HandleIntruderExit(Collider other)
    {
        if (staticFieldVFX == null)
        {
            staticFieldVFX.SetBool("Atractor1", false);
            staticFieldVFX.SetVector3("IntruderPosition", Vector3.zero);
        }
        intruder1 = null;

        // Desactivar el arco eléctrico visual
        if (arcoElectrico != null)
            arcoElectrico.SetActive(false);
    }

    // Actualiza continuamente la geometría del arco mientras el conductor permanece en el trigger
    void OnTriggerStay(Collider other)
    {
        if (other == intruder1)
        {
            HandleIntruderStay();
        }
    }

    // Interpola los puntos intermedios del arco eléctrico entre los extremos
    private void HandleIntruderStay()
    {
        // Asegura que hay suficientes puntos y que no son nulos
        if (puntosRef.Count >= 4 &&
            puntosRef[0] != null && puntosRef[1] != null &&
            puntosRef[2] != null && puntosRef[3] != null)
        {
            Vector3 pos1 = puntosRef[0].transform.position;
            Vector3 pos4 = puntosRef[3].transform.position;

            // El segundo objeto (índice 1) está más cerca del primero (25% del recorrido)
            puntosRef[1].transform.position = Vector3.Lerp(pos1, pos4, 0.25f);

            // El tercero (índice 2) está más cerca del cuarto (75% del recorrido)
            puntosRef[2].transform.position = Vector3.Lerp(pos1, pos4, 0.75f);
        }
    }


    // Actualiza la posición del intruso en el VFX cada cuadro
    void Update()
    {
        if (intruder1 != null)
            if (staticFieldVFX == null)
            {
                staticFieldVFX.SetVector3("IntruderPosition", intruder1.transform.position);
            }
    }
}
