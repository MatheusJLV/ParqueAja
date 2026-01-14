using System.Collections.Generic;
using UnityEngine;

// Detecta límites internos dentro de un cubo espacial
// Rastrea objetos con un tag específico dentro de un volumen de colisión
// y mantiene un contador de objetos presentes para determinar si el conjunto está completo
public class InternalBoundriesCUBESP : MonoBehaviour
{
    // Referencia al componente del cubo espacial para notificaciones de estado
    public CuboEspacialEnhanced cuboEspacialEnhanced;

    // Indica si todos los objetos requeridos están dentro del límite
    public bool complete = true;
    // Tag de los objetos a rastrear dentro del volumen
    public string trackedTag = "CubitosEspaciales";

    // Contador de objetos actualmente dentro del límite
    public int cantidad = 0;

    // Inicializa el contador en despertar
    private void Awake()
    {
        // Reinicia el contador para asegurar un estado limpio
        cantidad = 0;
    }

    // Sincroniza el estado con los colisores reales presentes al inicio de la escena
    private void Start()
    {
        // Obtiene todos los colisores dentro del volumen del cubo
        Collider[] colliders = Physics.OverlapBox(
            transform.position,
            transform.localScale * 0.5f,
            transform.rotation);

        // Cuenta los objetos con el tag especificado
        foreach (var col in colliders)
        {
            if (col.CompareTag(trackedTag))
                cantidad++;
        }

        // Determina si está completo (9 es el número requerido)
        complete = (cantidad == 9);
    }


    // Detecta cuando un objeto entra al volumen del límite interno
    private void OnTriggerEnter(Collider other)
    {
        // Si el objeto tiene el tag requerido, incrementa el contador
        if (other.CompareTag(trackedTag))
        {
            cantidad++;
            // Actualiza el estado de completitud
            complete = (cantidad == 9);
        }
    }

    // Detecta cuando un objeto sale del volumen del límite interno
    private void OnTriggerExit(Collider other)
    {
        // Si el objeto tiene el tag requerido, decrementa el contador
        if (other.CompareTag(trackedTag))
        {
            // Reduce la cantidad de objetos dentro del límite
            cantidad--;
            // Actualiza el estado de completitud
            complete = (cantidad == 9);
            // Notifica al cubo espacial que cambió el estado (no está completo)
            cuboEspacialEnhanced.SetRed();            
        }
    }
}
