using System.Collections;
using UnityEngine;
using UnityEngine.UI;
//0.002357 altura comprobada

// Gestiona la instancia y el crecimiento de prefabs en los aros y controla la tapa (Hinge).
// Proporciona métodos para elevar la tapa y crear/limpiar instancias según los sliders.
public class CrecimientoExpEnhanced : MonoBehaviour
{
    [Header("Parents for instantiation")]
    // Padre donde se instancian los prefabs grandes.
    public Transform ArosGrandes;
    // Padre donde se instancian los prefabs pequeños.
    public Transform ArosPequeños;

    [Header("Prefabs")]
    // Prefab para instanciar en ArosGrandes.
    public GameObject PrefabGrandes;
    // Prefab para instanciar en ArosPequeños.
    public GameObject PrefabPequeños;

    [Header("UI")]
    // Slider que controla la cantidad de prefabs pequeños a crear.
    public Slider pequeSd;
    // Slider que controla la cantidad de prefabs grandes a crear.
    public Slider grandeSd;
    // Botón que inicia la rutina de elevación e instanciado.
    public Button instanciarBtn;

    [Header("Tapa reference")]
    // Referencia al objeto tapa que se eleva mediante un HingeJoint.
    public GameObject Tapa;   // The lid to raise

    [Header("Tapa raise settings")]
    // Ángulo objetivo para levantar la tapa.
    public float tapaTargetAngle = 120f;
    // Fuerza del resorte aplicado a la bisagra.
    public float tapaSpringStrength = 500f;
    // Amortiguación del resorte de la bisagra.
    public float tapaDamper = 10f;
    // Impulso inicial para despegue si la tapa está en reposo.
    public float tapaKickForce = 10f;

    // Inicializa el listener del botón para levantar la tapa e instanciar los aros.
    void Start()
    {
        if (instanciarBtn != null)
        {
            instanciarBtn.onClick.AddListener(() => StartCoroutine(RaiseTapaAndInstantiate()));
        }
    }

    // Orquesta la elevación de la tapa y, tras estabilizarla, instancia los prefabs.
    private IEnumerator RaiseTapaAndInstantiate()
    {
        if (Tapa != null)
        {
            HingeJoint hinge = Tapa.GetComponent<HingeJoint>();
            if (hinge != null)
            {
                // Raise tapa before instancing
                yield return StartCoroutine(RaiseTapa(hinge, tapaTargetAngle));
            }
            else
            {
                Debug.LogWarning("CrecimientoExpEnhanced: Tapa has no HingeJoint!");
            }
        }
        yield return new WaitForSeconds(0.5f);
        // After tapa is raised, instantiate the rings
        InstanciarPequeñas();
        InstanciarGrandes();
    }

    // Aplica un resorte en la bisagra hasta targetAngle y usa un impulso para
    // romper el estado de reposo si es necesario; espera hasta que la tapa se estabilice.
    private IEnumerator RaiseTapa(HingeJoint hinge, float targetAngle)
    {
        // Apply spring
        JointSpring spring = hinge.spring;
        spring.spring = tapaSpringStrength;
        spring.damper = tapaDamper;
        spring.targetPosition = targetAngle;
        hinge.spring = spring;
        hinge.useSpring = true;

        Debug.Log("Applying spring to raise tapa toward " + targetAngle);

        // ?? Kickstart with torque to break resting position
        Rigidbody rb = hinge.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 axis = hinge.axis; // hinge axis in local space
            rb.AddTorque(hinge.transform.TransformDirection(axis) * tapaKickForce, ForceMode.Impulse);
            Debug.Log("Applied kick torque to unstick tapa");
        }

        // Wait until tapa reaches the angle
        while (Mathf.Abs(hinge.angle - targetAngle) > 1f)
        {
            yield return null;
        }

        // Extra buffer to ensure settled
        yield return new WaitForSeconds(0.5f);

        hinge.useSpring = false; // stop applying force

        Debug.Log("Tapa reached target and spring disabled.");
    }

    // Instancia la cantidad de prefabs pequeños indicada por pequeSd (si está configurado).
    public void InstanciarPequeñas()
    {
        if (ArosPequeños == null || PrefabPequeños == null || pequeSd == null)
            return;

        int cantidad = Mathf.RoundToInt(pequeSd.value);
        StartCoroutine(InstanciarPequeñasCoroutine(cantidad));
    }

    // Corutina que crea 'cantidad' prefabs pequeños con un pequeño retardo.
    private IEnumerator InstanciarPequeñasCoroutine(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Instantiate(PrefabPequeños, ArosPequeños);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // Instancia la cantidad de prefabs grandes indicada por grandeSd (si está configurado).
    public void InstanciarGrandes()
    {
        if (ArosGrandes == null || PrefabGrandes == null || grandeSd == null)
            return;

        int cantidad = Mathf.RoundToInt(grandeSd.value);
        StartCoroutine(InstanciarGrandesCoroutine(cantidad));
    }

    // Corutina que crea 'cantidad' prefabs grandes con un pequeño retardo.
    private IEnumerator InstanciarGrandesCoroutine(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Instantiate(PrefabGrandes, ArosGrandes);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // Elimina todos los hijos tanto de ArosGrandes como de ArosPequeños.
    public void Clear()
    {
        ClearChildren(ArosGrandes);
        ClearChildren(ArosPequeños);
    }

    // Elimina todos los hijos del transform 'parent' si está presente.
    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
