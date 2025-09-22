using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class GaltonTrailController : MonoBehaviour
{
    [Header("Trail setup")]
    public Material trailMaterial;                 // Assign an Unlit additive-like material
    [Range(0f, 10f)] public float trailTime = 1.2f;
    [Range(0.001f, 0.2f)] public float minVertexDistance = 0.03f;
    [Range(0f, 2f)] public float width = 0.02f;
    public AnimationCurve widthCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // taper tail
    public bool clearOnEnable = true;

    [Header("Color (randomized on start)")]
    public bool randomizeColorOnStart = true;
    [Range(0f, 1f)] public float saturation = 1f;
    [Range(0f, 1f)] public float value = 1f;
    [Range(0f, 1f)] public float startAlpha = 1f;
    [Range(0f, 1f)] public float endAlpha = 0f;

    [Header("Goal detection")]
    public string goalTag = "Goal";
    public string goalObjectName = "GoalZone";
    public bool preferTagCheck = true;            // Tag check is cheapest & most robust

    TrailRenderer tr;

    void Awake() => SetupTrail();

    void OnEnable()
    {
        if (!tr) SetupTrail();
        if (clearOnEnable) tr.Clear();

        if (randomizeColorOnStart)
        {
            float h = Random.value;                                  // 0..1 hue
            Color c = Color.HSVToRGB(h, Mathf.Clamp01(saturation), Mathf.Clamp01(value));
            ApplyGradient(c, startAlpha, endAlpha);
        }

        tr.emitting = true;  // start trail right away
    }

    void SetupTrail()
    {
        tr = GetComponent<TrailRenderer>();
        if (!tr) tr = gameObject.AddComponent<TrailRenderer>();

        tr.time = trailTime;
        tr.minVertexDistance = minVertexDistance;
        tr.widthMultiplier = width;
        tr.widthCurve = widthCurve;
        tr.alignment = LineAlignment.View;              // billboard ribbon
        tr.textureMode = LineTextureMode.Stretch;
        tr.numCornerVertices = 0;                       // perf
        tr.numCapVertices = 0;                          // perf
        tr.shadowCastingMode = ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.generateLightingData = false;
        tr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        tr.autodestruct = false;
        tr.emitting = false;

        if (trailMaterial) tr.material = trailMaterial;
    }

    void ApplyGradient(Color c, float a0, float a1)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a1, 1f) }
        );
        tr.colorGradient = g;
    }

    // ---- Stop trail when we hit the goal (tag or name) ----
    void OnTriggerEnter(Collider other) { if (IsGoal(other)) StopTrail(); }
    void OnCollisionEnter(Collision col) { if (IsGoal(col.collider)) StopTrail(); }

    bool IsGoal(Collider other)
    {
        if (preferTagCheck && !string.IsNullOrEmpty(goalTag) && other.CompareTag(goalTag)) return true;
        if (!string.IsNullOrEmpty(goalObjectName) && other.name == goalObjectName) return true;
        // In case the trigger is a child named "GoalZone":
        if (other.transform.parent && other.transform.parent.name == goalObjectName) return true;
        return false;
    }

    public void StopTrail()
    {
        if (!tr) return;
        tr.emitting = false;      // existing ribbon fades out according to 'trailTime'
        // If you wanted an instant cutoff: tr.Clear();
    }

    // Optional public setters if you want to drive from UI later
    public void SetTrailLength(float seconds) { if (tr) tr.time = Mathf.Max(0f, seconds); }
    public void SetTrailWidth(float w) { if (tr) tr.widthMultiplier = Mathf.Max(0f, w); }
}
