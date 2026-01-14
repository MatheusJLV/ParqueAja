using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Reproduce sonidos puntuales al agarrar (select entered) y soltar (select exited).
/// - Encuentra automáticamente XRGrabInteractable en este GameObject o padre.
/// - Usa PlayOneShot para no interferir con otros scripts o loops.
/// - Crea un AudioSource si no existe (opcional).
/// </summary>
[DisallowMultipleComponent]
public class GrabDropAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip grabClip;     // Sonido al agarrar
    public AudioClip dropClip;     // Sonido al soltar

    [Header("Levels")]
    [Range(0f, 1f)] public float grabVolume = 1f;   // Volumen del sonido de agarre
    [Range(0f, 1f)] public float dropVolume = 1f;   // Volumen del sonido de suelta

    [Header("Variation")]
    public bool randomizePitch = true;              // Varía el pitch para sonar más natural
    [Range(0f, 0.3f)] public float pitchVariance = 0.06f;

    [Header("Audio Source")]
    [Tooltip("If empty, we'll reuse an AudioSource on this object; if none, one is created.")]
    public AudioSource audioSource;
    [Tooltip("If we create an AudioSource, apply these defaults.")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // 3D by default
    public float minDistance = 0.3f;
    public float maxDistance = 6f;

    // Referencia al interactuable XR
    private XRGrabInteractable _grab;

    void OnEnable()
    {
        // Busca el interactuable en este objeto o padre en tiempo de ejecución
        _grab = GetComponent<XRGrabInteractable>() ?? GetComponentInParent<XRGrabInteractable>();
        if (_grab != null)
        {
            // Suscribe a eventos de selección (agarre y suelta)
            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        // Prepara o crea un AudioSource si es necesario (sin tocar clips existentes)
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // Si no existe, crea uno nuevo con configuración por defecto
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
        // Desuscribe los listeners para evitar referencias colgantes
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnSelectEntered);
            _grab.selectExited.RemoveListener(OnSelectExited);
        }
    }

    // Reproduce sonido cuando se agarra el objeto
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!grabClip || audioSource == null) return;
        // Calcula pitch aleatorio si está habilitado
        float pitch = randomizePitch ? 1f + Random.Range(-pitchVariance, pitchVariance) : 1f;
        // Guarda el pitch anterior y lo restaura después de PlayOneShot
        var oldPitch = audioSource.pitch;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(grabClip, grabVolume);
        audioSource.pitch = oldPitch; // Restaura para no afectar otros sistemas
    }

    // Reproduce sonido cuando se suelta el objeto
    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (!dropClip || audioSource == null) return;
        // Calcula pitch aleatorio si está habilitado
        float pitch = randomizePitch ? 1f + Random.Range(-pitchVariance, pitchVariance) : 1f;
        // Guarda el pitch anterior y lo restaura después de PlayOneShot
        var oldPitch = audioSource.pitch;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(dropClip, dropVolume);
        audioSource.pitch = oldPitch;
    }
}
