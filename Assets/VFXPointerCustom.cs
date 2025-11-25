using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/*
VFXPointerCustom: maneja la detección de un "conductor" dentro de un trigger,
actualiza un VisualEffect estático con la posición del intruso, activa un
arco eléctrico y notifica al VFXCarrier del objeto para que se cargue.
*/
public class VFXPointerCustom : MonoBehaviour
{
    //VisualEffect que muestra el campo estático
    public VisualEffect staticFieldVFX;
    //Collider actualmente detectado como intruso
    private Collider intruder1;
    //GameObject con la animación/efecto visual del arco eléctrico
    public GameObject arcoElectrico;
    //Puntos de referencia usados para posicionar curvas/efectos entre 0..3
    public List<GameObject> puntosRef = new List<GameObject>();

    //OnTriggerEnter: gestionar entrada de colliders al trigger
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] OnTriggerEnter llamado con: {other.gameObject.name}");
        
        //Ignorar si no es conductor
        if (!other.CompareTag("Conductor"))
        {
            Debug.Log($"[VFXPointerCustom] {other.gameObject.name} no tiene el tag 'Conductor'. Se ignora.");
            return;
        }
        //Ignorar si ya es el intruso registrado
        if (intruder1 != null && other == intruder1)
        {
            Debug.Log($"[VFXPointerCustom] {other.gameObject.name} ya est� asignado como intruder. Se ignora.");
            return;
        }

        HandleIntruderEnter(other);
    }
    //OnTriggerExit: gestionar salida de colliders del trigger
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] OnTriggerExit: {other.gameObject.name}");

        if (other == intruder1)
        {
            HandleIntruderExit(other);
        }
    }
    /*
    HandleIntruderEnter: asigna el intruso, actualiza el VFX estático,
    activa el arco y solicita carga al VFXCarrier del objeto.
    */
    private void HandleIntruderEnter(Collider other)
    {
        Debug.Log($"[VFXPointerCustom] Asignando {other.gameObject.name} como intruder1.");
        intruder1 = other;

        // Actualizar VFX si está asignado
        if (staticFieldVFX == null)
        {
            staticFieldVFX.SetBool("Atractor1", true);
            staticFieldVFX.SetVector3("IntruderPosition", intruder1.transform.position);
        }
        
        Debug.Log($"[VFXPointerCustom] VFX actualizado para {other.gameObject.name}.");
        
        // Activar arco eléctrico visual (si existe)
        if (arcoElectrico != null)
            arcoElectrico.SetActive(true);

        // Notificar al VFXCarrier del objeto intruso para que se cargue
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

    //HandleIntruderExit: limpia la referencia del intruso y apaga efectos
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

    //OnTriggerStay: mantener actualizaciones mientras el intruso permanece
    void OnTriggerStay(Collider other)
    {
        if (other == intruder1)
        {
            HandleIntruderStay();
        }
    }

    //HandleIntruderStay: reposiciona puntos intermedios entre puntosRef[0] y [3]
    private void HandleIntruderStay()
    {
        // Asegura que hay suficientes puntos y que no son nulos
        if (puntosRef.Count >= 4 &&
            puntosRef[0] != null && puntosRef[1] != null &&
            puntosRef[2] != null && puntosRef[3] != null)
        {
            Vector3 pos1 = puntosRef[0].transform.position;
            Vector3 pos4 = puntosRef[3].transform.position;

            // El segundo objeto (�ndice 1) est� m�s cerca del primero (por ejemplo, 25% del camino)
            puntosRef[1].transform.position = Vector3.Lerp(pos1, pos4, 0.25f);

            // El tercero (�ndice 2) est� m�s cerca del cuarto (por ejemplo, 75% del camino)
            puntosRef[2].transform.position = Vector3.Lerp(pos1, pos4, 0.75f);
        }
    }

    //Update: actualizar posición del intruso en el VFX cada frame si aplica

    void Update()
    {
        if (intruder1 != null)
            if (staticFieldVFX == null)
            {
                staticFieldVFX.SetVector3("IntruderPosition", intruder1.transform.position);
            }
    }
}
