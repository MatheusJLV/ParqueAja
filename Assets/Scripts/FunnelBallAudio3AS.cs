using UnityEngine;

/*
 * FunnelBallAudioLite:
 * Gestiona audio para una bola en un embudo, reproduciendo sonidos continuos
 * y espontáneos al entrar/salir de triggers marcados con "Funel".
 */

public class FunnelBallAudioLite : MonoBehaviour
{
    [Header("Audio Sources (assign in Inspector)")]
    [SerializeField] private AudioSource continuousAS;   // Fuente de audio continua (loop)
    [SerializeField] private AudioSource spontaneousAS;  // Fuente de audio espontánea (one-shot)

    [Header("Clips (assign)")]
    //[SerializeField] private AudioClip rollClip;     // played on continuousAS (loop)
    [SerializeField] private AudioClip enterOneShot; // Clip de audio al entrar (opcional)

    [SerializeField] private AudioSource slowAS; // Fuente para velocidad lenta
    [SerializeField] private AudioSource mediumAS; // Fuente para velocidad media
    [SerializeField] private AudioSource fastAS; // Fuente para velocidad rápida

    private int funelOverlapCount = 0; // Contador de overlaps con triggers Funel

    void Reset()
    {
        // Auto-grab primeros dos AudioSources si están presentes
        var srcs = GetComponentsInChildren<AudioSource>(true);
        if (srcs.Length > 0) continuousAS = srcs[0];
        if (srcs.Length > 1) spontaneousAS = srcs[1];
    }

    void Awake()
    {
        // Configurar AudioSources para reproducción 3D y loop
        if (continuousAS)
        {
            continuousAS.playOnAwake = false;
            continuousAS.loop = true;
            /*if (rollClip) continuousAS.clip = rollClip;
            continuousAS.spatialBlend = 1f; // 3D*/
        }
        if (spontaneousAS)
        {
            spontaneousAS.playOnAwake = false;
            spontaneousAS.loop = false;
            spontaneousAS.spatialBlend = 1f; // 3D
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar si el collider es un trigger "Funel"
        if (!other || !other.CompareTag("Funel")) return;

        funelOverlapCount++;

        // Iniciar loop de audio continuo (comentado)
        /*if (continuousAS && rollClip)
        {
            if (continuousAS.clip != rollClip) continuousAS.clip = rollClip;
            if (!continuousAS.isPlaying) continuousAS.Play();
        }*/

        // Reproducir one-shot al entrar (opcional)
        if (spontaneousAS && enterOneShot)
            spontaneousAS.PlayOneShot(enterOneShot);
    }

    void OnTriggerStay(Collider other)
    {
        // Verificar si el collider es un trigger "Funel"
        if (!other || !other.CompareTag("Funel")) return;

        // Seguridad: asegurar que el loop siga vivo
        /*if (continuousAS && rollClip && funelOverlapCount > 0 && !continuousAS.isPlaying)
            continuousAS.Play();*/
    }

    void OnTriggerExit(Collider other)
    {
        // Verificar si el collider es un trigger "Funel"
        if (!other || !other.CompareTag("Funel")) return;

        funelOverlapCount = Mathf.Max(0, funelOverlapCount - 1);
        // Detener loop si no hay más overlaps
        if (funelOverlapCount == 0 && continuousAS && continuousAS.isPlaying)
            continuousAS.Stop();
    }

    void OnDisable()
    {
        // Detener audio continuo al deshabilitar el objeto
        if (continuousAS && continuousAS.isPlaying) continuousAS.Stop();
    }
}
