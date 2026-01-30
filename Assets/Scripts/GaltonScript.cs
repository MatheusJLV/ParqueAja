using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class GaltonScript : MonoBehaviour
{
    /*
     Controla el sistema de generador de bolas de Galton con ajustes dinámicos
     de física, fricción, rebote, masa y amortiguamiento a través de sliders.
    */

    // Generador y referencias de instanciación
    public GameObject bolas;
    public GameObject bolita;   // PREFAB (asset)
    public float tiempo = 1f;
    public int cantidad = 0;
    public float escala = 1f;

    public Transform referencia1;
    public Transform referencia2;

    // Controles UI para cantidad de bolas, intervalo y botón de inicio
    public Slider cantidadSlider;
    public Slider tiempoSlider;
    public Button boton;

    // Material de física y sliders asociados
    [Header("Physics Material (asset)")]
    public PhysicsMaterial targetPhysicMaterial;

    [Header("UI Sliders")]
    [Tooltip("Dynamic = v, Static = v+0.2 (clamped)")]
    // Controles para fricción dinámica y rebote
    public Slider frictionSlider;      // 0..1
    public Slider bouncinessSlider;    // 0..1

    // Valores por defecto de Rigidbody que se aplican al prefab e instancias nuevas
    [Header("Rigidbody Defaults (Prefab + New Instances)")]
    public Slider massSlider;          // e.g. 0.01..1.0
    public Slider dampingSlider;       // Rigidbody.drag 0..5

    // Referencia cacheada al Rigidbody del prefab para actualizar valores por defecto
    Rigidbody rbPrefab;

    [Header("Input Safety")]
    public float cooldownBoton = 1.0f;   // seconds
    private float proximoClickPermitido = 0f;


    void Start()
    {
        // Suscribe el botón al evento de presión
        if (boton) boton.onClick.AddListener(OnBotonPresionado);

        // Cachea el Rigidbody del prefab (si existe)
        if (bolita) rbPrefab = bolita.GetComponent<Rigidbody>();

        // Crea un material de física en tiempo de ejecución si no se asignó uno
        if (!targetPhysicMaterial)
            targetPhysicMaterial = new PhysicsMaterial("RuntimeGaltonPM");

        // Inicializa sliders con rangos y valores actuales del material de física
        SetupSlider(frictionSlider, 0f, 1f, targetPhysicMaterial ? targetPhysicMaterial.dynamicFriction : 0.2f, OnFrictionChanged);
        SetupSlider(bouncinessSlider, 0f, 1f, targetPhysicMaterial ? targetPhysicMaterial.bounciness : 1.0f, OnBouncinessChanged);

        // Inicializa sliders de masa y amortiguamiento con valores del prefab
        float prefabMassInit = rbPrefab ? rbPrefab.mass : 0.1f;
        float prefabDragInit = rbPrefab ? rbPrefab.linearDamping : 0.0f;
        SetupSlider(massSlider, 0.01f, 5f, prefabMassInit, OnMassChanged);
        SetupSlider(dampingSlider, 0f, 5f, prefabDragInit, OnDampingChanged);

        // Aplica valores iniciales al material de física y prefab
        ApplyAllPhysicValues();
        ApplyRBDefaultsToPrefab();
    }

    void OnDestroy()
    {
        // Desuscribe el botón y sliders para evitar referencias colgantes
        if (boton) boton.onClick.RemoveListener(OnBotonPresionado);

        if (frictionSlider) frictionSlider.onValueChanged.RemoveListener(OnFrictionChanged);
        if (bouncinessSlider) bouncinessSlider.onValueChanged.RemoveListener(OnBouncinessChanged);
        if (massSlider) massSlider.onValueChanged.RemoveListener(OnMassChanged);
        if (dampingSlider) dampingSlider.onValueChanged.RemoveListener(OnDampingChanged);
    }

    // Manejador del botón de generación
    void OnBotonPresionado()
    {
        if (Time.time < proximoClickPermitido) return;
        proximoClickPermitido = Time.time + cooldownBoton;

        int valorCantidad = Mathf.RoundToInt(cantidadSlider ? cantidadSlider.value : 1f);
        Instanciar(valorCantidad);
    }


    // Inicia el proceso de generación de bolas o agrega a la cola
    public void Instanciar(int valor)
    {
        if (valor <= 0) valor = 1;

        // Si no hay generación activa, inicia la corrutina
        if (cantidad <= 0)
        {
            cantidad = valor;
            StartCoroutine(InstanciarBolitas());
        }
        else cantidad += valor;
    }

    // Genera bolas respetando el intervalo de tiempo configurado
    IEnumerator InstanciarBolitas()
    {
        while (cantidad > 0)
        {
            // Busca el contenedor de bolas si no está asignado
            if (!bolas)
            {
                bolas = transform.Find("Bolas")?.gameObject;
                if (!bolas) yield break;
            }

            // Instancia bola en el centro
            var b1 = Instantiate(bolita, bolas.transform.position, Quaternion.identity, bolas.transform);
            b1.transform.localScale = Vector3.one * escala;
            ApplyRBToInstance(b1);

            // Instancia bola en referencia 1 si existe
            if (referencia1)
            {
                var b2 = Instantiate(bolita, referencia1.position, Quaternion.identity, bolas.transform);
                b2.transform.localScale = Vector3.one * escala;
                ApplyRBToInstance(b2);
            }
            // Instancia bola en referencia 2 si existe
            if (referencia2)
            {
                var b3 = Instantiate(bolita, referencia2.position, Quaternion.identity, bolas.transform);
                b3.transform.localScale = Vector3.one * escala;
                ApplyRBToInstance(b3);
            }

            cantidad--;
            // Ajusta el tiempo de espera según el slider de velocidad
            float tiempoEspera = tiempoSlider ? (tiempo / Mathf.Max(0.001f, tiempoSlider.value)) : tiempo;
            yield return new WaitForSeconds(tiempoEspera);
        }
    }

    // Configurador genérico de sliders con rango inicial
    void SetupSlider(Slider s, float min, float max, float initial, UnityEngine.Events.UnityAction<float> cb)
    {
        if (!s) return;
        s.minValue = min;
        s.maxValue = max;
        // Asegura que el valor inicial esté dentro del rango
        s.value = Mathf.Clamp(initial, min, max);
        s.onValueChanged.AddListener(cb);
    }

    // Callback de cambio de fricción: actualiza dinámica y estática
    void OnFrictionChanged(float v)
    {
        if (!targetPhysicMaterial) return;
        // Fricción dinámica directa, estática con offset de 0.2
        float dyn = Mathf.Clamp01(v);
        float sta = Mathf.Clamp01(v + 0.2f);
        targetPhysicMaterial.dynamicFriction = dyn;
        targetPhysicMaterial.staticFriction = sta;
    }

    // Callback de cambio de rebote
    void OnBouncinessChanged(float v)
    {
        if (!targetPhysicMaterial) return;
        targetPhysicMaterial.bounciness = Mathf.Clamp01(v);
    }

    // Aplica todos los valores de física del material de una vez
    void ApplyAllPhysicValues()
    {
        if (!targetPhysicMaterial) return;

        // Lee valores actuales de los sliders
        float f = frictionSlider ? frictionSlider.value : 0.2f;
        float bou = bouncinessSlider ? bouncinessSlider.value : 1.0f;

        // Aplica los valores al material
        targetPhysicMaterial.dynamicFriction = Mathf.Clamp01(f);
        targetPhysicMaterial.staticFriction = Mathf.Clamp01(f + 0.2f);
        targetPhysicMaterial.bounciness = Mathf.Clamp01(bou);
    }

    // Callbacks para actualizar valores por defecto del Rigidbody en el prefab
    // Afectan a todas las instancias futuras
    void OnMassChanged(float v)
    {
        if (rbPrefab) rbPrefab.mass = Mathf.Max(0.0001f, v);
    }

    // Callback para cambio de amortiguamiento (drag)
    void OnDampingChanged(float v)
    {
        if (rbPrefab) rbPrefab.linearDamping = Mathf.Max(0f, v);
    }

    // Aplica los valores actuales de los sliders al Rigidbody del prefab
    void ApplyRBDefaultsToPrefab()
    {
        if (!rbPrefab) return;
        rbPrefab.mass = Mathf.Max(0.0001f, massSlider ? massSlider.value : rbPrefab.mass);
        rbPrefab.linearDamping = Mathf.Max(0f, dampingSlider ? dampingSlider.value : rbPrefab.linearDamping);
    }

    // Aplica los valores actuales de masa y amortiguamiento a una bola instanciada
    void ApplyRBToInstance(GameObject go)
    {
        if (!go) return;
        var rb = go.GetComponent<Rigidbody>();
        if (!rb) rb = go.GetComponentInChildren<Rigidbody>();
        if (!rb) return;

        // Usa slider si existe, sino el prefab, sino mantiene el valor actual
        rb.mass = Mathf.Max(0.0001f, massSlider ? massSlider.value : (rbPrefab ? rbPrefab.mass : rb.mass));
        rb.linearDamping = Mathf.Max(0f, dampingSlider ? dampingSlider.value : (rbPrefab ? rbPrefab.linearDamping : rb.linearDamping));
    }

    // Limpia todas las bolas: detiene audios/efectos y las destruye
    public void LimpiarBolas()
    {
        if (bolas == null) return;

        // Recorre todos los hijos directos de 'bolas' (orden inverso para evitar índices inválidos)
        for (int i = bolas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = bolas.transform.GetChild(i);

            // Detiene AudioSource en la bola
            AudioSource audio = child.GetComponent<AudioSource>();
            if (audio != null)
            {
                audio.Stop();
            }

            // Busca y detiene VisualEffect en hijos de la bola
            foreach (Transform subChild in child)
            {
                VisualEffect vfx = subChild.GetComponent<VisualEffect>();
                if (vfx != null)
                {
                    vfx.Stop();
                }
            }

            // Destruye la bola
            Destroy(child.gameObject);
        }
    }
}
