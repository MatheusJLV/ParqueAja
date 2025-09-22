using UnityEngine;
using UnityEngine.VFX;

[DisallowMultipleComponent]
public class VFXAutoStop : MonoBehaviour
{
    [Header("Timing")]
    [Min(0f)] public float seconds = 8f;   // how long the VFX should run
    public bool useUnscaledTime = true;      // ignore Time.timeScale (recommended)

    [Header("Behavior")]
    public bool playOnEnable = true;         // auto-Play when this component enables
    public bool clearOnStop = false;         // Reinit to clear particles instantly
    public bool searchChildrenIfMissing = false; // find a VFX on children if needed

    VisualEffect vfx;

    void Awake()
    {
        // Grab the VFX on this object; optionally look in children
        vfx = GetComponent<VisualEffect>();
        if (!vfx && searchChildrenIfMissing)
            vfx = GetComponentInChildren<VisualEffect>(true);
    }

    void OnEnable()
    {
        if (!vfx) { vfx = GetComponent<VisualEffect>(); if (!vfx) return; }

        if (playOnEnable)
        {
            // (Optional) ensure a clean start
            // vfx.Reinit();   // uncomment if you want a hard reset every time
            vfx.Play();
        }

        StartTimer();
    }

    void OnDisable()
    {
        CancelInvoke(nameof(DoStop));
        StopAllCoroutines();
    }

    // You can call this manually if you want to restart the countdown
    public void StartTimer()
    {
        CancelInvoke(nameof(DoStop));
        if (useUnscaledTime) Invoke(nameof(DoStop), seconds);
        else StartCoroutine(StopAfterScaled(seconds));
    }

    System.Collections.IEnumerator StopAfterScaled(float s)
    {
        yield return new WaitForSeconds(s);  // scaled by timeScale
        DoStop();
    }

    public void DoStop()
    {
        if (!vfx) return;
        vfx.Stop();            // stops all spawners; existing particles finish by lifetime
        if (clearOnStop) vfx.Reinit();  // immediate clear (optional)
    }
}

