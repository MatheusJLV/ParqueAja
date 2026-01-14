using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class GaltonTrailController : MonoBehaviour
{
    /*
     Controla un trail renderer para bolas de Galton con color aleatorio,
     detección de meta y apagado suave al alcanzarla.
    */

    [Header("Trail setup")]
    public Material trailMaterial;                 // Material sin luz aditivo
    [Range(0f, 10f)] public float trailTime = 1.2f;
    [Range(0.001f, 0.2f)] public float minVertexDistance = 0.03f;
    [Range(0f, 2f)] public float width = 0.02f;
    public AnimationCurve widthCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Curve que afina la cola
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
    public bool preferTagCheck = true;            // Verificación de tag es más rápida y robusta

    TrailRenderer tr;

    void Awake() => SetupTrail();

    void OnEnable()
    {
        // Inicializa el trail si no existe
        if (!tr) SetupTrail();
        // Limpia el trail anterior si está configurado
        if (clearOnEnable) tr.Clear();

        // Color aleatorio al iniciar si está habilitado
        if (randomizeColorOnStart)
        {
            float h = Random.value;                                  // 0..1 matiz
            Color c = Color.HSVToRGB(h, Mathf.Clamp01(saturation), Mathf.Clamp01(value));
            ApplyGradient(c, startAlpha, endAlpha);
        }

        // Inicia el trail de inmediato
        tr.emitting = true;
    }

    void SetupTrail()
    {
        // Obtiene o crea el TrailRenderer
        tr = GetComponent<TrailRenderer>();
        if (!tr) tr = gameObject.AddComponent<TrailRenderer>();

        // Configuración de trail: tiempo, distancia mínima, ancho
        tr.time = trailTime;
        tr.minVertexDistance = minVertexDistance;
        tr.widthMultiplier = width;
        tr.widthCurve = widthCurve;
        // Renderizado: ribbon tipo billboard
        tr.alignment = LineAlignment.View;
        tr.textureMode = LineTextureMode.Stretch;
        // Optimizaciones de rendimiento
        tr.numCornerVertices = 0;
        tr.numCapVertices = 0;
        tr.shadowCastingMode = ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.generateLightingData = false;
        tr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        tr.autodestruct = false;
        // No emite hasta OnEnable
        tr.emitting = false;

        // Asigna el material si está disponible
        if (trailMaterial) tr.material = trailMaterial;
    }

    void ApplyGradient(Color c, float a0, float a1)
    {
        // Crea un gradiente uniforme de color con alpha que desvanece
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a1, 1f) }
        );
        tr.colorGradient = g;
    }

    // Detiene el trail al alcanzar la meta (por tag o nombre)
    void OnTriggerEnter(Collider other) { if (IsGoal(other)) StopTrail(); }
    void OnCollisionEnter(Collision col) { if (IsGoal(col.collider)) StopTrail(); }

    bool IsGoal(Collider other)
    {
        // Intenta primero por tag (más rápido)
        if (preferTagCheck && !string.IsNullOrEmpty(goalTag) && other.CompareTag(goalTag)) return true;
        // Intenta por nombre del objeto
        if (!string.IsNullOrEmpty(goalObjectName) && other.name == goalObjectName) return true;
        // Fallback: comprueba si el padre coincide (para triggers hijos)
        if (other.transform.parent && other.transform.parent.name == goalObjectName) return true;
        return false;
    }

    public void StopTrail()
    {
        if (!tr) return;
        // Detiene la emisión; el trail existente se desvanece según trailTime
        tr.emitting = false;
        // Si quisieras un corte instantáneo: tr.Clear();
    }

    // Setters públicos para control dinámico desde UI
    public void SetTrailLength(float seconds) { if (tr) tr.time = Mathf.Max(0f, seconds); }
    public void SetTrailWidth(float w) { if (tr) tr.widthMultiplier = Mathf.Max(0f, w); }
}
