using System.Collections;
using UnityEngine;
using UnityEngine.UI;
//0.002357 altura comprobada
public class CrecimientoExpEnhanced : MonoBehaviour
{
    [Header("Parents for instantiation")]
    public Transform ArosGrandes;
    public Transform ArosPequeños;

    [Header("Prefabs")]
    public GameObject PrefabGrandes;
    public GameObject PrefabPequeños;

    [Header("UI")]
    public Slider pequeSd;
    public Slider grandeSd;
    public Button instanciarBtn;

    [Header("Tapa reference")]
    public GameObject Tapa;   // The lid to raise

    [Header("Tapa raise settings")]
    public float tapaTargetAngle = 120f;
    public float tapaSpringStrength = 500f;
    public float tapaDamper = 10f;
    public float tapaKickForce = 10f;

    void Start()
    {
        if (instanciarBtn != null)
        {
            instanciarBtn.onClick.AddListener(() => StartCoroutine(RaiseTapaAndInstantiate()));
        }
    }

    /// <summary>
    /// Orchestrates raising the Tapa before instancing rings.
    /// </summary>
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

    /// <summary>
    /// Raises the tapa using its hinge spring up to targetAngle, with a kick torque to break rest state.
    /// </summary>
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

    // Instancia el número de prefabs pequeños según el valor del slider, usando corutina
    public void InstanciarPequeñas()
    {
        if (ArosPequeños == null || PrefabPequeños == null || pequeSd == null)
            return;

        int cantidad = Mathf.RoundToInt(pequeSd.value);
        StartCoroutine(InstanciarPequeñasCoroutine(cantidad));
    }

    private IEnumerator InstanciarPequeñasCoroutine(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Instantiate(PrefabPequeños, ArosPequeños);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // Instancia el número de prefabs grandes según el valor del slider, usando corutina
    public void InstanciarGrandes()
    {
        if (ArosGrandes == null || PrefabGrandes == null || grandeSd == null)
            return;

        int cantidad = Mathf.RoundToInt(grandeSd.value);
        StartCoroutine(InstanciarGrandesCoroutine(cantidad));
    }

    private IEnumerator InstanciarGrandesCoroutine(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Instantiate(PrefabGrandes, ArosGrandes);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // Elimina todos los hijos de ambos padres
    public void Clear()
    {
        ClearChildren(ArosGrandes);
        ClearChildren(ArosPequeños);
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
