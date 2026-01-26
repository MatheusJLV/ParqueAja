using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class PlataformaGiratoriaScript : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject plataforma;
    [SerializeField] private GameObject jugadorRig;

    [Header("UI")]
    [SerializeField] private Button iniciarPlataformaBtn;
    [SerializeField] private Slider velocidadSlider;
    [SerializeField] private Slider duracionSlider;

    [Header("Teleport Anchors")]
    [SerializeField] private TeleportationAnchor plataformaTP;
    [SerializeField] private TeleportationAnchor sueloTP;

    [Header("Giro")]
    [SerializeField] private float velocidadMaxima = 25f; // degrees/sec
    [SerializeField] private int duracion = 8;           // seconds

    public enum Eje { X, Y, Z }
    [SerializeField] private Eje ejeRotacion = Eje.Z;

    [Header("Opciones de entrada/salida")]
    [SerializeField] private float pausaTrasTeleport = 0f;

    [Header("Influence by Hand Distance")]
    [SerializeField] private GameObject controlDer;
    [SerializeField] private GameObject controlIzq;

    // meters (Unity units)
    [SerializeField] private float handsDistanceMin = 0.08f;   // 5cm (hands almost touching)
    [SerializeField] private float handsDistanceNeutral = 0.38f; // 38cm (shoulder-ish)
    [SerializeField] private float handsDistanceMax = 1.90f;   // 170cm (arm span-ish)

    // how much influence boosts speed at extremes
    [SerializeField] private float influenciaMultiplicador = 2.0f; // 1 = +100% at extremes

    // smoothing so the speed doesnt jitter
    [SerializeField] private float influenciaSuavizado = 12f;

    private float influencia = 1f;

    private bool rideEnCurso = false;

    [SerializeField] private Slider amplificadorSlider;

    // value set by slider
    [SerializeField] private float amplificador = 3f;

    // snapshot values (used only during ride)
    private float ride_baseSpeed = 0f;
    private float ride_boostMultiplier = 0f;

    private void Start()
    {
        // Button -> Start Ride
        if (iniciarPlataformaBtn != null)
        {
            iniciarPlataformaBtn.onClick.AddListener(() =>
            {
                if (rideEnCurso) return;
                StartCoroutine(Ride_TeleportOnly_ConstantSpin());
            });
        }

        // Speed slider -> velocidadMaxima
        if (velocidadSlider != null)
        {
            velocidadSlider.value = velocidadMaxima;

            velocidadSlider.onValueChanged.AddListener((v) =>
            {
                velocidadMaxima = v;
            });
        }

        // Duration slider -> duracion
        if (duracionSlider != null)
        {
            duracionSlider.wholeNumbers = true;
            duracionSlider.value = duracion;

            duracionSlider.onValueChanged.AddListener((v) =>
            {
                duracion = Mathf.RoundToInt(v);
            });
        }

        if (amplificadorSlider != null)
        {
            amplificadorSlider.value = amplificador;

            amplificadorSlider.onValueChanged.AddListener((v) =>
            {
                amplificador = v;
            });
        }

    }

    private IEnumerator Ride_TeleportOnly_ConstantSpin()
    {
        // Snapshot values so they don't change mid-ride
        ride_baseSpeed = velocidadMaxima;
        ride_boostMultiplier = influenciaMultiplicador * amplificador;

        rideEnCurso = true;

        // TP IN
        yield return StartCoroutine(TeleportToAnchor_WithTimeout(plataformaTP, 1.0f));

        // Parent rig to platform (so it follows rotation)
        if (jugadorRig != null && plataforma != null)
            jugadorRig.transform.SetParent(plataforma.transform, true);

        // Rotate for duration seconds at constant speed
        float t = 0f;
        while (t < duracion)
        {
            ActualizarInfluenciaPorDistancia();
            ApplyRotationStep();

            t += Time.deltaTime;
            yield return null;
        }


        // Unparent BEFORE teleport out
        if (jugadorRig != null)
            jugadorRig.transform.SetParent(null, true);

        // TP OUT
        yield return StartCoroutine(TeleportToAnchor_WithTimeout(sueloTP, 1.0f));

        rideEnCurso = false;
    }

    private void ApplyRotationStep()
    {
        if (plataforma == null) return;

        float speedFactor = 1f + (influencia * ride_boostMultiplier);
        float effectiveSpeed = ride_baseSpeed * speedFactor;

        float step = effectiveSpeed * Time.deltaTime;

        Vector3 axis = Vector3.forward;
        switch (ejeRotacion)
        {
            case Eje.X: axis = Vector3.right; break;
            case Eje.Y: axis = Vector3.up; break;
            case Eje.Z: axis = Vector3.forward; break;
        }

        plataforma.transform.Rotate(axis, step, Space.Self);
    }





    private IEnumerator TeleportToAnchor_WithTimeout(TeleportationAnchor anchor, float timeoutSeconds)
    {
        if (anchor == null)
            yield break;

        TeleportationProvider provider = anchor.teleportationProvider;

        if (provider == null)
            provider = FindObjectOfType<TeleportationProvider>();

        if (provider == null)
        {
            Debug.LogWarning("[PlataformaGiratoriaScript] No TeleportationProvider found in scene.");
            yield break;
        }

        float oldDelay = provider.delayTime;
        provider.delayTime = 0f;

        Transform target = anchor.teleportAnchorTransform != null ? anchor.teleportAnchorTransform : anchor.transform;

        TeleportRequest req = new TeleportRequest
        {
            destinationPosition = target.position,
            destinationRotation = target.rotation,
            matchOrientation = MatchOrientation.TargetUpAndForward,
            requestTime = Time.time
        };

        bool queued = provider.QueueTeleportRequest(req);

        yield return null;

        if (queued && jugadorRig != null)
        {
            float start = Time.time;

            while (Time.time - start < timeoutSeconds)
            {
                float d = Vector3.Distance(jugadorRig.transform.position, req.destinationPosition);
                if (d <= 0.25f)
                    break;

                yield return null;
            }
        }

        provider.delayTime = oldDelay;

        if (pausaTrasTeleport > 0f)
            yield return new WaitForSeconds(pausaTrasTeleport);
    }

    private void OnDestroy()
    {
        if (iniciarPlataformaBtn != null) iniciarPlataformaBtn.onClick.RemoveAllListeners();
        if (velocidadSlider != null) velocidadSlider.onValueChanged.RemoveAllListeners();
        if (duracionSlider != null) duracionSlider.onValueChanged.RemoveAllListeners();
        if (amplificadorSlider != null)
            amplificadorSlider.onValueChanged.RemoveAllListeners();

    }

    private void ActualizarInfluenciaPorDistancia()
    {
        if (controlDer == null || controlIzq == null)
            return;

        float d = Vector3.Distance(controlDer.transform.position, controlIzq.transform.position);

        // clamp into expected range
        d = Mathf.Clamp(d, handsDistanceMin, handsDistanceMax);

        // linear: close => 1, far => 0
        float rawInfluence = 1f - Mathf.InverseLerp(handsDistanceMin, handsDistanceMax, d);

        // smoothing
        influencia = Mathf.Lerp(influencia, rawInfluence, Time.deltaTime * influenciaSuavizado);
    }


}
