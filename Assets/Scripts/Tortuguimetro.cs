using System.Collections;
using UnityEngine;

public class Tortuguimetro : MonoBehaviour
{
    [Header("Audio / Visual")]
    [SerializeField] private AudioSource alarma1;
    [SerializeField] private AudioSource alarma2;
    [SerializeField] private MeshCollider tortugaSil;
    [SerializeField] private Light luzAlarma;
    [SerializeField] private GameObject foco;

    [Header("Impact FX")]
    [SerializeField] private GameObject impactoPF; //  Assign the impact prefab in Inspector

    private bool alarmaActiva = false;
    private Coroutine alarmaVisualCoroutine = null;

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

    public void ActivarAlarma1()
    {
        if (alarma1 != null && !alarma1.isPlaying)
        {
            alarma1.Play();
        }
    }

    public void ActivarAlarma2()
    {
        if (alarma2 != null && !alarma2.isPlaying)
        {
            alarma2.Play();
        }
    }

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
    // Impact spawning utilities
    // ---------------------------

    /// <summary>
    /// Call this from your existing impact-detection method with the contact point and normal.
    /// Example: SpawnImpactFX(collision.contacts[0].point, collision.contacts[0].normal);
    /// </summary>
    public void SpawnImpactFX(Vector3 position, Vector3 normal)
    {
        if (impactoPF == null) return;

        // Align the prefab to the surface using the contact normal
        Quaternion rotation = Quaternion.LookRotation(normal);
        Instantiate(impactoPF, position, rotation);
    }

    /// <summary>
    /// Optional: if you use physics collisions (non-trigger), this will auto-spawn the impact prefab
    /// at every contact point.
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        if (impactoPF == null || collision == null || collision.contactCount == 0) return;

        foreach (var c in collision.contacts)
        {
            SpawnImpactFX(c.point, c.normal);
        }
    }
}
