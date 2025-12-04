using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a looping sound while the object is moving (by translation and/or rotation).
/// - Samples motion at a low rate (no per-frame polling).
/// - Fades in when motion crosses the threshold; fades out after inactivity.
/// - Uses Rigidbody if available; otherwise falls back to transform deltas.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MotionSoundController : MonoBehaviour
{
    public enum MotionMode { Linear, Angular, Either } // which motion triggers audio

    [Header("Audio")]
    [Tooltip("Looping clip that represents motion (e.g., whoosh/hum/whirr).")]
    public AudioClip loopClip;
    [Range(0f, 1f)] public float maxVolume = 1f;
    public float fadeIn = 0.25f;
    public float fadeOut = 0.35f;

    [Header("Motion Source")]
    public MotionMode motionMode = MotionMode.Either;
    [Tooltip("Linear speed threshold (m/s) to be considered moving.")]
    public float linearSpeedThreshold = 0.05f;
    [Tooltip("Angular speed threshold (rad/s) to be considered rotating.")]
    public float angularSpeedThreshold = 0.25f;

    [Header("Sampling")]
    [Tooltip("How many times per second to sample motion (lower = cheaper).")]
    public float sampleRateHz = 10f;
    [Tooltip("If motion stays below threshold for this long, fade out.")]
    public float idleTimeout = 0.5f;

    [Header("Optional Pitch Mapping")]
    public bool pitchBySpeed = false;
    [Tooltip("Pitch at zero threshold; usually 1.0.")]
    public float pitchMin = 0.95f;
    [Tooltip("Pitch at or above reference speed.")]
    public float pitchMax = 1.1f;
    [Tooltip("Linear speed that maps to pitchMax (m/s).")]
    public float pitchRefLinear = 1.0f;
    [Tooltip("Angular speed that maps to pitchMax (rad/s).")]
    public float pitchRefAngular = 2.5f;

    // Internals
    private AudioSource _src;
    private Rigidbody _rb;
    private Coroutine _sampler;
    private Coroutine _fade;
    private bool _active;              // currently considered "moving"
    private float _lastAboveTime;      // last time we were above threshold

    // Fallback deltas when Rigidbody is absent
    private Vector3 _prevPos;
    private Quaternion _prevRot;

    private void Awake()
    {
        _src = GetComponent<AudioSource>();
        _rb = GetComponent<Rigidbody>();

        _src.playOnAwake = false;
        _src.loop = true;
        _src.volume = 0f;
        if (loopClip != null) _src.clip = loopClip;

        _prevPos = transform.position;
        _prevRot = transform.rotation;
    }

    private void OnEnable()
    {
        if (_sampler != null) StopCoroutine(_sampler);
        _sampler = StartCoroutine(SampleMotionLoop());
    }

    private void OnDisable()
    {
        if (_sampler != null) StopCoroutine(_sampler);
        HardStop();
    }

    private void OnDestroy()
    {
        HardStop();
    }

    private IEnumerator SampleMotionLoop()
    {
        float dt = Mathf.Max(0.02f, 1f / Mathf.Max(1f, sampleRateHz));

        while (true)
        {
            float lin = 0f, ang = 0f;

            if (_rb != null)
            {
                lin = _rb.linearVelocity.magnitude;
                ang = _rb.angularVelocity.magnitude; // rad/s
            }
            else
            {
                // Fallback: measure deltas since last sample
                Vector3 pos = transform.position;
                Quaternion rot = transform.rotation;

                lin = (pos - _prevPos).magnitude / dt;
                // Convert delta rotation to angle in radians
                Quaternion dq = rot * Quaternion.Inverse(_prevRot);
                dq.ToAngleAxis(out float angleDeg, out _);
                ang = Mathf.Deg2Rad * Mathf.Abs(angleDeg) / dt;

                _prevPos = pos;
                _prevRot = rot;
            }

            bool above =
                motionMode == MotionMode.Linear ? (lin >= linearSpeedThreshold) :
                motionMode == MotionMode.Angular ? (ang >= angularSpeedThreshold) :
                /* Either */                       (lin >= linearSpeedThreshold || ang >= angularSpeedThreshold);

            if (above)
            {
                _lastAboveTime = Time.time;

                if (!_active)
                {
                    _active = true;
                    StartFade(_src.volume, maxVolume, fadeIn, startIfNeeded: true);
                }

                if (pitchBySpeed)
                {
                    // Choose best speed metric per mode for pitch mapping
                    float t =
                        motionMode == MotionMode.Linear ? Mathf.Clamp01(lin / Mathf.Max(0.0001f, pitchRefLinear)) :
                        motionMode == MotionMode.Angular ? Mathf.Clamp01(ang / Mathf.Max(0.0001f, pitchRefAngular)) :
                        Mathf.Clamp01(Mathf.Max(lin / Mathf.Max(0.0001f, pitchRefLinear),
                                                ang / Mathf.Max(0.0001f, pitchRefAngular)));
                    _src.pitch = Mathf.Lerp(pitchMin, pitchMax, t);
                }
            }
            else if (_active && (Time.time - _lastAboveTime) > idleTimeout)
            {
                _active = false;
                StartFade(_src.volume, 0f, fadeOut, startIfNeeded: false, stopAtEnd: true);
            }

            yield return new WaitForSeconds(dt);
        }
    }

    private void StartFade(float from, float to, float duration, bool startIfNeeded, bool stopAtEnd = false)
    {
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeCo(from, to, duration, startIfNeeded, stopAtEnd));
    }

    private IEnumerator FadeCo(float from, float to, float duration, bool startIfNeeded, bool stopAtEnd)
    {
        if (loopClip != null && startIfNeeded)
        {
            if (_src.clip != loopClip) _src.clip = loopClip;
            if (!_src.isPlaying) _src.Play();
        }

        duration = Mathf.Max(0.01f, duration);
        float t0 = Time.realtimeSinceStartup;

        while (true)
        {
            float a = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / duration);
            _src.volume = Mathf.Lerp(from, to, a);
            if (a >= 1f) break;
            yield return null;
        }

        if (stopAtEnd && to <= 0.0001f)
        {
            _src.volume = 0f;
            _src.Stop();
        }
    }

    public void HardStop()
    {
        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
        if (_src != null)
        {
            _src.Stop();
            _src.volume = 0f;
        }
        _active = false;
    }
}
