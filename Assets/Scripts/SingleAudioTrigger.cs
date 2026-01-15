using System.Collections.Generic;
using UnityEngine;

// Sistema de trigger de audio que reproduce un AudioSource cuando objetos con un tag específico entran en el collider.
// Maneja múltiples objetos simultáneamente y solo detiene el audio cuando todos han salido de la zona.
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class SingleAudioTrigger : MonoBehaviour
{
    [Header("Match Settings")]
    [Tooltip("Only objects with this tag will trigger playback.")]
    public string matchTag = "Player";   // Tag que deben tener los objetos para activar el audio

    [Header("Audio")]
    [Tooltip("Leave empty to use the AudioSource on this GameObject.")]
    public AudioSource source;           // Fuente de audio a reproducir (opcional, se obtiene automáticamente si está vacío)           // Fuente de audio a reproducir (opcional, se obtiene automáticamente si está vacío)

    // Rastrea los colliders que coinciden con el tag y están actualmente dentro del trigger para evitar detener el audio prematuramente
    private readonly HashSet<Collider> _inside = new HashSet<Collider>();

    // Configura automáticamente el componente en el editor con valores por defecto sensatos
    private void Reset()
    {
        // Make setup painless
        source = GetComponent<AudioSource>();
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Sensible audio defaults for 3D zones
        source.playOnAwake = false;
        source.loop = true;          // continuous zone by default
        source.spatialBlend = 1f;    // fully 3D
    }

    // Inicializa las referencias y asegura que el collider esté configurado como trigger
    private void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        var col = GetComponent<Collider>();
        if (!col.isTrigger) col.isTrigger = true;
    }

    // Detecta cuando un objeto con el tag correcto entra en el trigger e inicia la reproducción del audio
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(matchTag))
        {
            if (_inside.Add(other))
            {
                if (!source.isPlaying) source.Play();
            }
        }
    }

    // Maneja casos de entrada tardía para triggers anidados/superpuestos o cuando objetos aparecen dentro del trigger
    private void OnTriggerStay(Collider other)
    {
        // Late-enter safety for nested/overlapping triggers or spawn-inside cases
        if (other.CompareTag(matchTag))
        {
            if (_inside.Add(other))
            {
                if (!source.isPlaying) source.Play();
            }
        }
    }

    // Detecta cuando un objeto sale del trigger y detiene el audio solo si no quedan más objetos dentro
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(matchTag))
        {
            _inside.Remove(other);
            if (_inside.Count == 0 && source.isPlaying)
            {
                source.Stop();
            }
        }
    }

    // Limpia el seguimiento y detiene el audio si el componente se deshabilita o destruye
    private void OnDisable()
    {
        _inside.Clear();
        if (source != null && source.isPlaying) source.Stop();
    }

#if UNITY_EDITOR
    // Dibuja un gizmo visual en el editor para visualizar la zona del trigger
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider bc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(bc.center, bc.size);
        }
        else if (col is SphereCollider sc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(sc.center, sc.radius);
        }
    }
#endif
}
