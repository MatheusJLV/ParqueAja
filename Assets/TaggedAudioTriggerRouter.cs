using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps trigger-enter events to AudioSources based on collider tags.
/// You can manage two groups of sources: "instantaneous" and "continuous".
/// For now they behave the same (both just Play()).
/// </summary>
[DisallowMultipleComponent]
public class TaggedAudioTriggerRouter : MonoBehaviour
{
    [Serializable]
    public struct TagToSource
    {
        public string tag;
        public AudioSource source;
    }

    [Header("Mappings (trigger tag -> audio source)")]
    public TagToSource[] instantaneous;
    public TagToSource[] continuous;

    // Internal quick-lookup
    private Dictionary<string, AudioSource> _instByTag;
    private Dictionary<string, AudioSource> _contByTag;

    // Track which specific colliders we've already �entered� to avoid replays every frame
    private readonly HashSet<Collider> _seen = new HashSet<Collider>();

    private void Awake()
    {
        _instByTag = BuildMap(instantaneous);
        _contByTag = BuildMap(continuous);

        // (Optional but recommended) Make sure at least one of the pair has a Rigidbody
        // Ball should already have one. If not, add here or warn:
        if (GetComponent<Rigidbody>() == null)
            Debug.LogWarning($"{nameof(TaggedAudioTriggerRouter)} on {name} has no Rigidbody; " +
                             "trigger messages require a Rigidbody on one of the colliders.");
    }

    private Dictionary<string, AudioSource> BuildMap(TagToSource[] arr)
    {
        var map = new Dictionary<string, AudioSource>(StringComparer.Ordinal);
        if (arr == null) return map;
        foreach (var e in arr)
        {
            if (!string.IsNullOrWhiteSpace(e.tag) && e.source != null)
                map[e.tag] = e.source; // last wins if duplicates
        }
        return map;
    }

    private void TryPlayForTag(string tag)
    {
        if (_instByTag.TryGetValue(tag, out var a) && a != null) a.Play();
        if (_contByTag.TryGetValue(tag, out var b) && b != null) b.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Normal path: entering a trigger volume boundary
        _seen.Add(other);
        TryPlayForTag(other.gameObject.tag); // compare TRIGGER's tag
    }

    private void OnTriggerStay(Collider other)
    {
        // Late-enter path: if we spawned/teleported already inside, OnTriggerEnter never fires.
        if (_seen.Add(other)) // returns true if it wasn't tracked yet
        {
            TryPlayForTag(other.gameObject.tag);
        }
        // otherwise do nothing; already handled
    }

    private void OnTriggerExit(Collider other)
    {
        // Allow future re-triggers when we come back
        _seen.Remove(other);
    }
}
