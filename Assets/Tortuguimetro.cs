using System.Collections;
using UnityEngine;

public class Tortuguimetro : MonoBehaviour
{
    [SerializeField] private AudioSource alarma1;
    [SerializeField] private AudioSource alarma2;
    [SerializeField] private MeshCollider tortugaSil;
    [SerializeField] private Light luzAlarma;
    [SerializeField] private GameObject foco;

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
            if (alarma1 != null && alarma1.isPlaying)
            {
                alarma1.Stop();
            }
            if (alarma2 != null && alarma2.isPlaying)
            {
                alarma2.Stop();
            }
            alarmaActiva = false;
            if (alarmaVisualCoroutine != null)
            {
                StopCoroutine(alarmaVisualCoroutine);
                alarmaVisualCoroutine = null;
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
            if (luzAlarma != null)
                luzAlarma.enabled = estado;

            if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", estado ? Color.red : Color.black);
            }

            estado = !estado;
            yield return new WaitForSeconds(0.5f);
        }
        // Al salir, asegúrate de dejar todo apagado
        if (luzAlarma != null)
            luzAlarma.enabled = false;
        if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
            renderer.material.SetColor("_EmissionColor", Color.black);
    }
}
