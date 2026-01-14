using UnityEngine;

public class FunnelBallAudioLite : MonoBehaviour
{
    /*
     Gestiona audios al entrar, permanecer y salir de triggers con tag "Funel".
     Usa una fuente continua (loop) y otra para eventos puntuales (one-shot).
    */

    [Header("Audio Sources (assign in Inspector)")]
    [SerializeField] private AudioSource continuousAS;   // loop source
    [SerializeField] private AudioSource spontaneousAS;  // one-shot on enter

    [Header("Clips (assign)")]
    //[SerializeField] private AudioClip rollClip;     // played on continuousAS (loop)
    [SerializeField] private AudioClip enterOneShot; // played once on spontaneousAS (optional)

    // Fuentes adicionales según velocidad (asignar en el Inspector)
    [SerializeField] private AudioSource slowAS;
    [SerializeField] private AudioSource mediumAS;
    [SerializeField] private AudioSource fastAS;

    // Maneja solapes con múltiples triggers para no cortar el loop antes de tiempo
    private int funelOverlapCount = 0; // handle multiple adjacent Funel triggers

    void Reset()
    {
        // Autollenado rápido de las dos primeras AudioSources en hijos
        var srcs = GetComponentsInChildren<AudioSource>(true);
        if (srcs.Length > 0) continuousAS = srcs[0];
        if (srcs.Length > 1) spontaneousAS = srcs[1];
    }

    void Awake()
    {
        // Configura la fuente continua: sin auto-play, en loop, modo 3D
        if (continuousAS)
        {
            continuousAS.playOnAwake = false;
            continuousAS.loop = true;
            /*if (rollClip) continuousAS.clip = rollClip;
            continuousAS.spatialBlend = 1f; // 3D*/
        }
        // Configura la fuente de eventos: sin auto-play, sin loop, modo 3D
        if (spontaneousAS)
        {
            spontaneousAS.playOnAwake = false;
            spontaneousAS.loop = false;
            spontaneousAS.spatialBlend = 1f; // 3D
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other || !other.CompareTag("Funel")) return;

        // Incrementa el conteo al entrar en un trigger válido
        funelOverlapCount++;

        // Start loop
        /*if (continuousAS && rollClip)
        {
            if (continuousAS.clip != rollClip) continuousAS.clip = rollClip;
            if (!continuousAS.isPlaying) continuousAS.Play();
        }*/

        // Fire one-shot on enter (optional)
        if (spontaneousAS && enterOneShot)
            spontaneousAS.PlayOneShot(enterOneShot);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other || !other.CompareTag("Funel")) return;

        // Seguridad: podría reactivar el loop si se deseara
        /*if (continuousAS && rollClip && funelOverlapCount > 0 && !continuousAS.isPlaying)
            continuousAS.Play();*/
    }

    void OnTriggerExit(Collider other)
    {
        if (!other || !other.CompareTag("Funel")) return;

        // Reduce el conteo y detiene el loop cuando ya no hay solapes
        funelOverlapCount = Mathf.Max(0, funelOverlapCount - 1);
        if (funelOverlapCount == 0 && continuousAS && continuousAS.isPlaying)
            continuousAS.Stop();
    }

    void OnDisable()
    {
        // Limpieza: asegura que el loop quede detenido al deshabilitar
        if (continuousAS && continuousAS.isPlaying) continuousAS.Stop();
    }
}
