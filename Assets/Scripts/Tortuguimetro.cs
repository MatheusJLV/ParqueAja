using System.Collections;
using UnityEngine;

// Sistema de alarma de tortuguímetro que detecta colisiones con trigger y activa alarmas sonoras y visuales.
// Gestiona múltiples estados de alarma (entrada, permanencia, salida) y efectos visuales intermitentes.
// También maneja la generación de efectos de impacto en puntos de colisión.
public class Tortuguimetro : MonoBehaviour
{
    [Header("Audio / Visual")]
    [SerializeField] private AudioSource alarma1;       // Fuente de audio para la primera alarma (al entrar)
    [SerializeField] private AudioSource alarma2;       // Fuente de audio para la segunda alarma (al permanecer)
    [SerializeField] private MeshCollider tortugaSil;   // Collider de la tortuga que activa las alarmas
    [SerializeField] private Light luzAlarma;           // Luz que parpadea durante la alarma visual
    [SerializeField] private GameObject foco;           // GameObject del foco cuya emisión cambia durante la alarma

    [Header("Impact FX")]
    [SerializeField] private GameObject impactoPF;      // Prefab del efecto de impacto que se instancia en colisiones

    private bool alarmaActiva = false;                  // Indica si la alarma visual está actualmente activa
    private Coroutine alarmaVisualCoroutine = null;     // Referencia a la corrutina de alarma visual en ejecución     // Referencia a la corrutina de alarma visual en ejecución

    // Inicializa el sistema configurando el trigger, apagando la luz de alarma y estableciendo la emisión del foco en negro
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

    // Maneja el evento cuando un collider entra en el trigger, activando la primera alarma
    void OnTriggerEnter(Collider other)
    {
        if (other == tortugaSil)
        {
            ActivarAlarma1();

            // Si tu "impacto" se detecta vía triggers y aún quieres un spawn:
            // Vector3 p = other.ClosestPoint(transform.position);
            // SpawnImpactFX(p, Vector3.up);
        }
    }

    // Maneja el evento cuando un collider permanece en el trigger, activando la segunda alarma y el efecto visual
    void OnTriggerStay(Collider other)
    {
        if (other == tortugaSil)
        {
            ActivarAlarma2();
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

    // Maneja el evento cuando un collider sale del trigger, deteniendo todas las alarmas y reseteando los efectos visuales
    void OnTriggerExit(Collider other)
    {
        if (other == tortugaSil)
        {
            if (alarma1 != null && alarma1.isPlaying) alarma1.Stop();
            if (alarma2 != null && alarma2.isPlaying) alarma2.Stop();

            alarmaActiva = false;
            if (alarmaVisualCoroutine != null)
            {
                StopCoroutine(alarmaVisualCoroutine);
                alarmaVisualCoroutine = null;
            }
            if (luzAlarma != null) luzAlarma.enabled = false;

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

    // Activa la primera alarma de audio si no está reproduciéndose actualmente
    public void ActivarAlarma1()
    {
        if (alarma1 != null && !alarma1.isPlaying)
        {
            alarma1.Play();
        }
    }

    // Activa la segunda alarma de audio si no está reproduciéndose actualmente
    public void ActivarAlarma2()
    {
        if (alarma2 != null && !alarma2.isPlaying)
        {
            alarma2.Play();
        }
    }

    // Corrutina que alterna la luz de alarma y el color de emisión del foco entre rojo y negro cada 0.5 segundos
    private IEnumerator ActivarAlarmaVisual()
    {
        var renderer = foco != null ? foco.GetComponent<Renderer>() : null;
        bool estado = false;
        while (alarmaActiva)
        {
            if (luzAlarma != null) luzAlarma.enabled = estado;

            if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", estado ? Color.red : Color.black);
            }

            estado = !estado;
            yield return new WaitForSeconds(0.5f);
        }
        if (luzAlarma != null) luzAlarma.enabled = false;
        if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            renderer.material.SetColor("_EmissionColor", Color.black);
    }

    // ---------------------------
    // Utilidades de generación de impactos
    // ---------------------------

    // Instancia un efecto de impacto en la posición y con la orientación especificadas.
    // Llama esto desde tu método existente de detección de impacto con el punto y normal de contacto.
    // Ejemplo: SpawnImpactFX(collision.contacts[0].point, collision.contacts[0].normal);
    public void SpawnImpactFX(Vector3 position, Vector3 normal)
    {
        if (impactoPF == null) return;

        // Alinear el prefab a la superficie usando la normal de contacto
        Quaternion rotation = Quaternion.LookRotation(normal);
        Instantiate(impactoPF, position, rotation);
    }

    // Opcional: si usas colisiones físicas (no-trigger), esto generará automáticamente el prefab de impacto
    // en cada punto de contacto de la colisión.
    void OnCollisionEnter(Collision collision)
    {
        if (impactoPF == null || collision == null || collision.contactCount == 0) return;

        foreach (var c in collision.contacts)
        {
            SpawnImpactFX(c.point, c.normal);
        }
    }
}
