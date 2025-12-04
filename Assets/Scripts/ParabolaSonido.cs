using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class ParabolaSonido : MonoBehaviour
{
    [Header("Visual Effects (exactly four)")]
    [Tooltip("First VFX in the sequence")]
    public VisualEffect vfx1;
    [Tooltip("Second VFX in the sequence")]
    public VisualEffect vfx2;
    [Tooltip("Third VFX in the sequence")]
    public VisualEffect vfx3;
    [Tooltip("Fourth VFX in the sequence")]
    public VisualEffect vfx4;

    [Header("Start Offsets (delay BEFORE each VFX starts)")]
    [Min(0f)] public float startOffset1 = 0f;
    [Min(0f)] public float startOffset2 = 0.5f;
    [Min(0f)] public float startOffset3 = 0.25f;
    [Min(0f)] public float startOffset4 = 1f;

    [Header("Stop Offsets (delay BEFORE each VFX stops)")]
    [Min(0f)] public float stopOffset1 = 0f;
    [Min(0f)] public float stopOffset2 = 0.5f;
    [Min(0f)] public float stopOffset3 = 0.25f;
    [Min(0f)] public float stopOffset4 = 1f;

    [Header("Options")]
    [Tooltip("Automatically run StartAllSequential on Start()")]
    public bool playOnStart = false;

    [Tooltip("Force-stop all VFX on startup so nothing plays by default")]
    public bool initializeStopped = true;

    [Header("UI (optional)")]
    [Tooltip("Button that will trigger StartAllSequential()")]
    public Button startButton;

    [Tooltip("Button that will trigger StopAllSequential()")]
    public Button stopButton;

    [Tooltip("If true, automatically (re)wire the buttons on enable")]
    public bool autoWireButtons = true;

    Coroutine startRoutine;
    Coroutine stopRoutine;

    // Make sure nothing plays by default.
    // OnEnable runs before Start and is early enough to catch Play-On-Awake.
    void OnEnable()
    {
        if (initializeStopped)
        {
            ForceStopAllImmediate();
            StartCoroutine(ForceStopNextFrame());
        }

        if (autoWireButtons)
            WireButtons();
    }
    void OnDisable()
    {
        UnwireButtons();
    }

    void Start()
    {
        if (playOnStart)
            StartAllSequential();
    }

    public void StartAllSequential()
    {
        // If a stop is in progress, cancel it first
        if (stopRoutine != null) { StopCoroutine(stopRoutine); stopRoutine = null; }
        if (startRoutine != null) { StopCoroutine(startRoutine); }
        startRoutine = StartCoroutine(CoStartAll());
    }

    public void StopAllSequential()
    {
        // If a start is in progress, cancel it first
        if (startRoutine != null) { StopCoroutine(startRoutine); startRoutine = null; }
        if (stopRoutine != null) { StopCoroutine(stopRoutine); }
        stopRoutine = StartCoroutine(CoStopAll());
    }

    public void CancelSequences()
    {
        if (startRoutine != null) { StopCoroutine(startRoutine); startRoutine = null; }
        if (stopRoutine != null) { StopCoroutine(stopRoutine); stopRoutine = null; }
    }

    IEnumerator CoStartAll()
    {
        // Each offset is the delay BEFORE starting that specific effect.
        if (vfx1 != null) { yield return new WaitForSeconds(startOffset1); SafePlay(vfx1); }
        if (vfx2 != null) { yield return new WaitForSeconds(startOffset2); SafePlay(vfx2); }
        if (vfx3 != null) { yield return new WaitForSeconds(startOffset3); SafePlay(vfx3); }
        if (vfx4 != null) { yield return new WaitForSeconds(startOffset4); SafePlay(vfx4); }
        startRoutine = null;
    }

    IEnumerator CoStopAll()
    {
        // Each offset is the delay BEFORE stopping that specific effect.
        if (vfx1 != null) { yield return new WaitForSeconds(stopOffset1); SafeStop(vfx1); }
        if (vfx2 != null) { yield return new WaitForSeconds(stopOffset2); SafeStop(vfx2); }
        if (vfx3 != null) { yield return new WaitForSeconds(stopOffset3); SafeStop(vfx3); }
        if (vfx4 != null) { yield return new WaitForSeconds(stopOffset4); SafeStop(vfx4); }
        stopRoutine = null;
    }

    // VFX Graph helpers (guard against disabled/NULL)
    void SafePlay(VisualEffect vfx)
    {
        if (vfx != null && vfx.isActiveAndEnabled)
        {
            // If your graph expects a specific event, you can send it here instead:
            // vfx.SendEvent("OnPlay");
            vfx.Play();
        }
    }

    void SafeStop(VisualEffect vfx)
    {
        if (vfx != null && vfx.isActiveAndEnabled)
        {
            // Or: vfx.SendEvent("OnStop");
            vfx.Stop();
        }
    }

    void ForceStopAllImmediate()
    {
        SafeStop(vfx1);
        SafeStop(vfx2);
        SafeStop(vfx3);
        SafeStop(vfx4);
    }

    IEnumerator ForceStopNextFrame()
    {
        // Catch any late Play-on-Enable behavior.
        yield return null;
        ForceStopAllImmediate();
    }
    void WireButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartAllSequential);
            startButton.onClick.AddListener(StartAllSequential);
        }

        if (stopButton != null)
        {
            stopButton.onClick.RemoveListener(StopAllSequential);
            stopButton.onClick.AddListener(StopAllSequential);
        }
    }

    void UnwireButtons()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartAllSequential);

        if (stopButton != null)
            stopButton.onClick.RemoveListener(StopAllSequential);
    }
}

