using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Plays one-shot sounds on grab (select entered) and drop (select exited).
/// - Auto-finds XRGrabInteractable on this GameObject (or parent).
/// - Uses PlayOneShot so it won't interfere with other scripts/loops.
/// - Creates an AudioSource if none exists (optional).
/// </summary>
[DisallowMultipleComponent]
public class GrabDropAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip grabClip;
    public AudioClip dropClip;

    [Header("Levels")]
    [Range(0f, 1f)] public float grabVolume = 1f;
    [Range(0f, 1f)] public float dropVolume = 1f;

    [Header("Variation")]
    public bool randomizePitch = true;
    [Range(0f, 0.3f)] public float pitchVariance = 0.06f;

    [Header("Audio Source")]
    [Tooltip("If empty, we'll reuse an AudioSource on this object; if none, one is created.")]
    public AudioSource audioSource;
    [Tooltip("If we create an AudioSource, apply these defaults.")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // 3D by default
    public float minDistance = 0.3f;
    public float maxDistance = 6f;

    private XRGrabInteractable _grab;

    void OnEnable()
    {
        // Find the interactable on this object (or parent) at runtime, so instantiation is safe
        _grab = GetComponent<XRGrabInteractable>() ?? GetComponentInParent<XRGrabInteractable>();
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnSelectEntered); // Fix: Use AddListener for UnityEvent
            _grab.selectExited.AddListener(OnSelectExited);   // Fix: Use AddListener for UnityEvent
        }

        // Prepare / create an AudioSource if needed (but don't touch existing .clip)
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = spatialBlend;
                audioSource.minDistance = minDistance;
                audioSource.maxDistance = maxDistance;
            }
        }
    }

    void OnDisable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnSelectEntered); // Fix: Use RemoveListener for UnityEvent
            _grab.selectExited.RemoveListener(OnSelectExited);   // Fix: Use RemoveListener for UnityEvent
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!grabClip || audioSource == null) return;
        float pitch = randomizePitch ? 1f + Random.Range(-pitchVariance, pitchVariance) : 1f;
        var oldPitch = audioSource.pitch;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(grabClip, grabVolume);
        audioSource.pitch = oldPitch; // restore in case other systems use this source later
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (!dropClip || audioSource == null) return;
        float pitch = randomizePitch ? 1f + Random.Range(-pitchVariance, pitchVariance) : 1f;
        var oldPitch = audioSource.pitch;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(dropClip, dropVolume);
        audioSource.pitch = oldPitch;
    }
}
