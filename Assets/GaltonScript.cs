using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GaltonScript : MonoBehaviour
{
    //  Spawner 
    public GameObject bolas;
    public GameObject bolita;   // PREFAB (asset)
    public float tiempo = 1f;
    public int cantidad = 0;
    public float escala = 1f;

    public Transform referencia1;
    public Transform referencia2;

    public Slider cantidadSlider;
    public Slider tiempoSlider;
    public Button boton;

    //  Physics Material (asset) + sliders 
    [Header("Physics Material (asset)")]
    public PhysicsMaterial targetPhysicMaterial;

    [Header("UI Sliders")]
    [Tooltip("Dynamic = v, Static = v+0.2 (clamped)")]
    public Slider frictionSlider;      // 0..1
    public Slider bouncinessSlider;    // 0..1

    //  Rigidbody defaults (applied to prefab + new instances) 
    [Header("Rigidbody Defaults (Prefab + New Instances)")]
    public Slider massSlider;          // e.g. 0.01..1.0
    public Slider dampingSlider;       // Rigidbody.drag 0..5

    // cached prefab RB (for updating defaults)
    Rigidbody rbPrefab;

    void Start()
    {
        if (boton) boton.onClick.AddListener(OnBotonPresionado);

        // Cache RB on the PREFAB (if it has one)
        if (bolita) rbPrefab = bolita.GetComponent<Rigidbody>();

        // Ensure a PM asset to edit (so sliders don’t NRE even if none assigned)
        if (!targetPhysicMaterial)
            targetPhysicMaterial = new PhysicsMaterial("RuntimeGaltonPM");

        // Initialize sliders with sensible ranges and current values if available
        SetupSlider(frictionSlider, 0f, 1f, targetPhysicMaterial ? targetPhysicMaterial.dynamicFriction : 0.2f, OnFrictionChanged);
        SetupSlider(bouncinessSlider, 0f, 1f, targetPhysicMaterial ? targetPhysicMaterial.bounciness : 1.0f, OnBouncinessChanged);

        float prefabMassInit = rbPrefab ? rbPrefab.mass : 0.1f;
        float prefabDragInit = rbPrefab ? rbPrefab.linearDamping : 0.0f;
        SetupSlider(massSlider, 0.01f, 5f, prefabMassInit, OnMassChanged);
        SetupSlider(dampingSlider, 0f, 5f, prefabDragInit, OnDampingChanged);

        // Push initial values into PM + prefab RB
        ApplyAllPhysicValues();
        ApplyRBDefaultsToPrefab();
    }

    void OnDestroy()
    {
        if (boton) boton.onClick.RemoveListener(OnBotonPresionado);

        if (frictionSlider) frictionSlider.onValueChanged.RemoveListener(OnFrictionChanged);
        if (bouncinessSlider) bouncinessSlider.onValueChanged.RemoveListener(OnBouncinessChanged);
        if (massSlider) massSlider.onValueChanged.RemoveListener(OnMassChanged);
        if (dampingSlider) dampingSlider.onValueChanged.RemoveListener(OnDampingChanged);
    }

    //  Spawner 
    void OnBotonPresionado()
    {
        int valorCantidad = Mathf.RoundToInt(cantidadSlider ? cantidadSlider.value : 1f);
        Instanciar(valorCantidad);
    }

    public void Instanciar(int valor)
    {
        if (valor <= 0) valor = 1;

        if (cantidad <= 0)
        {
            cantidad = valor;
            StartCoroutine(InstanciarBolitas());
        }
        else cantidad += valor;
    }

    IEnumerator InstanciarBolitas()
    {
        while (cantidad > 0)
        {
            if (!bolas)
            {
                bolas = transform.Find("Bolas")?.gameObject;
                if (!bolas) yield break;
            }

            // Centro
            var b1 = Instantiate(bolita, bolas.transform.position, Quaternion.identity, bolas.transform);
            b1.transform.localScale = Vector3.one * escala;
            ApplyRBToInstance(b1);

            // Ref 1
            if (referencia1)
            {
                var b2 = Instantiate(bolita, referencia1.position, Quaternion.identity, bolas.transform);
                b2.transform.localScale = Vector3.one * escala;
                ApplyRBToInstance(b2);
            }
            // Ref 2
            if (referencia2)
            {
                var b3 = Instantiate(bolita, referencia2.position, Quaternion.identity, bolas.transform);
                b3.transform.localScale = Vector3.one * escala;
                ApplyRBToInstance(b3);
            }

            cantidad--;
            float tiempoEspera = tiempoSlider ? (tiempo / Mathf.Max(0.001f, tiempoSlider.value)) : tiempo;
            yield return new WaitForSeconds(tiempoEspera);
        }
    }

    //  UI wiring helpers 
    void SetupSlider(Slider s, float min, float max, float initial, UnityEngine.Events.UnityAction<float> cb)
    {
        if (!s) return;
        s.minValue = min;
        s.maxValue = max;
        s.value = Mathf.Clamp(initial, min, max);
        s.onValueChanged.AddListener(cb);
    }

    //  Sliders  PhysicMaterial asset 
    void OnFrictionChanged(float v)
    {
        if (!targetPhysicMaterial) return;
        float dyn = Mathf.Clamp01(v);
        float sta = Mathf.Clamp01(v + 0.2f);
        targetPhysicMaterial.dynamicFriction = dyn;
        targetPhysicMaterial.staticFriction = sta;
    }

    void OnBouncinessChanged(float v)
    {
        if (!targetPhysicMaterial) return;
        targetPhysicMaterial.bounciness = Mathf.Clamp01(v);
    }

    void ApplyAllPhysicValues()
    {
        if (!targetPhysicMaterial) return;

        float f = frictionSlider ? frictionSlider.value : 0.2f;
        float bou = bouncinessSlider ? bouncinessSlider.value : 1.0f;

        targetPhysicMaterial.dynamicFriction = Mathf.Clamp01(f);
        targetPhysicMaterial.staticFriction = Mathf.Clamp01(f + 0.2f);
        targetPhysicMaterial.bounciness = Mathf.Clamp01(bou);
    }

    //  Sliders  Prefab RB defaults (affects FUTURE Instantiate) 
    void OnMassChanged(float v)
    {
        if (rbPrefab) rbPrefab.mass = Mathf.Max(0.0001f, v);
    }

    void OnDampingChanged(float v)
    {
        if (rbPrefab) rbPrefab.linearDamping = Mathf.Max(0f, v);
    }

    void ApplyRBDefaultsToPrefab()
    {
        if (!rbPrefab) return;
        rbPrefab.mass = Mathf.Max(0.0001f, massSlider ? massSlider.value : rbPrefab.mass);
        rbPrefab.linearDamping = Mathf.Max(0f, dampingSlider ? dampingSlider.value : rbPrefab.linearDamping);
    }

    //  Ensure new instances match current sliders 
    void ApplyRBToInstance(GameObject go)
    {
        if (!go) return;
        var rb = go.GetComponent<Rigidbody>();
        if (!rb) rb = go.GetComponentInChildren<Rigidbody>();
        if (!rb) return;

        rb.mass = Mathf.Max(0.0001f, massSlider ? massSlider.value : (rbPrefab ? rbPrefab.mass : rb.mass));
        rb.linearDamping = Mathf.Max(0f, dampingSlider ? dampingSlider.value : (rbPrefab ? rbPrefab.linearDamping : rb.linearDamping));
    }
}
