using System.Collections;
using UnityEngine;

/*
Tortuguimetro: controla alarmas visuales y de audio cuando una "tortuga" (MeshCollider)
entra en contacto con este trigger.

Funcionalidades:
- Reproduce sonidos de alarma al entrar y permanecer en el trigger.
- Parpadea luz y emisión del material del foco mientras la tortuga está dentro.
- Puede instanciar efectos visuales en puntos de impacto (colisiones).
*/
public class Tortuguimetro : MonoBehaviour
{
    [Header("Audio / Visual")]
    [SerializeField] private AudioSource alarma1;   // Audio que se reproduce al entrar (OnTriggerEnter)
    [SerializeField] private AudioSource alarma2;   // Audio que se reproduce mientras se está dentro (OnTriggerStay)
    [SerializeField] private MeshCollider tortugaSil;   // Collider de la tortuga que dispara las alarmas
    [SerializeField] private Light luzAlarma;   // Luz que parpadea durante la alarma
    [SerializeField] private GameObject foco;   // GameObject con material emisivo que parpadea

    [Header("Impact FX")]
    [SerializeField] private GameObject impactoPF; //  Assign the impact prefab in Inspector

    //Flags y control de coroutines
    //Indica si la alarma visual está activa
    private bool alarmaActiva = false;
    private Coroutine alarmaVisualCoroutine = null; // Referencia para detener la coroutine

    /* Inicializar componentes y estados
    - Configurar tortugaSil como trigger
    - Desactivar luz al iniciar
    - Inicializar emisión del foco en negro
    */
    void Start()
    {
        if (tortugaSil != null)
        {
            tortugaSil.isTrigger = true;
        }
        if (luzAlarma != null)
        {
            luzAlarma.enabled = false;
        }
        if (foco != null)
        {
            var renderer = foco.GetComponent<Renderer>();
            if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
    //OnTriggerEnter: se ejecuta cuando la tortuga entra en este trigger
    //Reproduce la alarma1 (sonido de entrada)
    
    void OnTriggerEnter(Collider other)
    {
        if (other == tortugaSil)
        {
            ActivarAlarma1();

            // If your "impact" is detected via triggers and you still want a spawn:
            // Vector3 p = other.ClosestPoint(transform.position);
            // SpawnImpactFX(p, Vector3.up);
        }
    }

    //OnTriggerStay: se ejecuta cada frame mientras la tortuga está dentro
    //Reproduce la alarma2 (sonido continuo) e inicia el parpadeo visual
    void OnTriggerStay(Collider other)
    {
        if (other == tortugaSil)
        {
            ActivarAlarma2();
            //Activar alarma visual solo una vez (cuando entra por primera vez)
            if (!alarmaActiva)
            {
                alarmaActiva = true;
                if (alarmaVisualCoroutine == null)
                {
                    alarmaVisualCoroutine = StartCoroutine(ActivarAlarmaVisual());
                }
            }
        }
    }
    //OnTriggerExit: se ejecuta cuando la tortuga sale del trigger
    //Detiene ambas alarmas, apaga la luz y restaura el material a negro
    void OnTriggerExit(Collider other)
    {
        if (other == tortugaSil)
        {
            //Detener reproducción de audio
            if (alarma1 != null && alarma1.isPlaying) alarma1.Stop();
            if (alarma2 != null && alarma2.isPlaying) alarma2.Stop();
            //Desactivar flags y coroutine
            alarmaActiva = false;
            if (alarmaVisualCoroutine != null)
            {
                StopCoroutine(alarmaVisualCoroutine);
                alarmaVisualCoroutine = null;
            //Apagar luz de alarma
            }
            if (luzAlarma != null) luzAlarma.enabled = false;

            //Restaurar emisión del foco a negro
            if (foco != null)
            {
                var renderer = foco.GetComponent<Renderer>();
                if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
                {
                    renderer.material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
    
    //Reproduce la alarma1 si no está ya sonando
    //Uso: disparo único al entrar en el trigger (OnTriggerEnter) 
    public void ActivarAlarma1()
    {
        if (alarma1 != null && !alarma1.isPlaying)
        {
            alarma1.Play();
        }
    }
    
    //Reproduce la alarma2 si no está ya sonando
    //Uso: sonido continuo mientras se está dentro del trigger (OnTriggerStay)
    public void ActivarAlarma2()
    {
        if (alarma2 != null && !alarma2.isPlaying)
        {
            alarma2.Play();
        }
    }
    /*
    Coroutine que hace parpadear la luz y la emisión del material
    Alterna entre activado (rojo) y desactivado (negro) cada 0.5 segundos
    */
    private IEnumerator ActivarAlarmaVisual()
    {
        var renderer = foco != null ? foco.GetComponent<Renderer>() : null;
        bool estado = false;
        while (alarmaActiva)
        {
            //Alternar luz
            if (luzAlarma != null) luzAlarma.enabled = estado;

            //Alternar emisión del material entre rojo y negro
            if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", estado ? Color.red : Color.black);
            }
            //Cambiar estado y esperar antes del siguiente parpadeo
            estado = !estado;
            yield return new WaitForSeconds(0.5f);
        }
        //Asegurar que luz y emisión están apagadas al terminar

        if (luzAlarma != null) luzAlarma.enabled = false;
        if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            renderer.material.SetColor("_EmissionColor", Color.black);
    }

    // ---------------------------
    // Utilidades para spawner efectos de impacto
    // ---------------------------

    /*
    Instancia el prefab de impacto en la posición y rotación especificadas
    La rotación se alinea con la normal de la superficie (punto de contacto)
    Uso: SpawnImpactFX(collision.contacts[0].point, collision.contacts[0].normal);
    */
    public void SpawnImpactFX(Vector3 position, Vector3 normal)
    {
        if (impactoPF == null) return;

        // Align the prefab to the surface using the contact normal
        Quaternion rotation = Quaternion.LookRotation(normal);
        Instantiate(impactoPF, position, rotation);
    }

    /*
     Detecta colisiones físicas (no-trigger) y spawea automáticamente el prefab de impacto
    en cada punto de contacto.
      
     Nota: Este método solo funciona si el collider de este GameObject NO está marcado como Trigger.
     Para triggers, usa SpawnImpactFX() manualmente.
     */
    void OnCollisionEnter(Collision collision)
    {
        if (impactoPF == null || collision == null || collision.contactCount == 0) return;

        // Recorrer todos los puntos de contacto y spawear efectos en cada uno
        foreach (var c in collision.contacts)
        {
            SpawnImpactFX(c.point, c.normal);
        }
    }
}
