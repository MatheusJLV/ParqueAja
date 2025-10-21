using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class SingleAudioTrigger : MonoBehaviour
{
    [Header("Match Settings")]
    [Tooltip("Only objects with this tag will trigger playback.")]
    public string matchTag = "Player"; // set in Inspector to your ball/coin tag

    [Header("Audio")]
    [Tooltip("Leave empty to use the AudioSource on this GameObject.")]
    public AudioSource source;

    // Track matching colliders currently inside so we don't stop early
    private readonly HashSet<Collider> _inside = new HashSet<Collider>();

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

    private void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        var col = GetComponent<Collider>();
        if (!col.isTrigger) col.isTrigger = true;
    }

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

    // Optional: if the object gets disabled/destroyed while playing, stop cleanly
    private void OnDisable()
    {
        _inside.Clear();
        if (source != null && source.isPlaying) source.Stop();
    }

#if UNITY_EDITOR
    // Nice gizmo to see the zone in the editor
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
