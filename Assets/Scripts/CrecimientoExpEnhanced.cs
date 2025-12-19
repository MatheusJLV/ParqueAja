using System.Collections;
using UnityEngine;
using UnityEngine.UI;
//0.002357 altura comprobada

/*
 Controla el experimento de crecimiento.
 Eleva una tapa mediante bisagra y luego instancia
 anillos grandes y pequeños según valores de sliders.
*/

public class CrecimientoExpEnhanced : MonoBehaviour
{
    // Padres donde se instanciarán los aros
    [Header("Parents for instantiation")]
    public Transform ArosGrandes;
    public Transform ArosPeque�os;

    // Prefabs de los aros
    [Header("Prefabs")]
    public GameObject PrefabGrandes;
    public GameObject PrefabPeque�os;

    // Controles de interfaz
    [Header("UI")]
    public Slider pequeSd;
    public Slider grandeSd;
    public Button instanciarBtn;

    // Referencia a la tapa con bisagra
    [Header("Tapa reference")]
    public GameObject Tapa;   // The lid to raise

    // Parámetros de elevación de la tapa
    [Header("Tapa raise settings")]
    public float tapaTargetAngle = 120f;
    public float tapaSpringStrength = 500f;
    public float tapaDamper = 10f;
    public float tapaKickForce = 10f;

    void Start()
    {
        // Asocia el botón a la secuencia principal
        if (instanciarBtn != null)
        {
            instanciarBtn.onClick.AddListener(() => StartCoroutine(RaiseTapaAndInstantiate()));
        }
    }

    // Eleva la tapa y luego instancia los aros
    private IEnumerator RaiseTapaAndInstantiate()
    {
        if (Tapa != null)
        {
            HingeJoint hinge = Tapa.GetComponent<HingeJoint>();
            if (hinge != null)
            {
                // Eleva la tapa antes de instanciar
                yield return StartCoroutine(RaiseTapa(hinge, tapaTargetAngle));
            }
            else
            {
                Debug.LogWarning("CrecimientoExpEnhanced: Tapa has no HingeJoint!");
            }
        }
        // Espera breve antes de instanciar
        yield return new WaitForSeconds(0.5f);
         // Instancia los aros según los sliders
        InstanciarPeque�as();
        InstanciarGrandes();
    }

    // Eleva la tapa usando un resorte en la bisagra
    private IEnumerator RaiseTapa(HingeJoint hinge, float targetAngle)
    {
        // Configura el resorte
        JointSpring spring = hinge.spring;
        spring.spring = tapaSpringStrength;
        spring.damper = tapaDamper;
        spring.targetPosition = targetAngle;
        hinge.spring = spring;
        hinge.useSpring = true;

        Debug.Log("Applying spring to raise tapa toward " + targetAngle);

         // Aplica un impulso para romper el estado de reposo
        Rigidbody rb = hinge.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 axis = hinge.axis; // hinge axis in local space
            rb.AddTorque(hinge.transform.TransformDirection(axis) * tapaKickForce, ForceMode.Impulse);
            Debug.Log("Applied kick torque to unstick tapa");
        }

        // Espera hasta alcanzar el ángulo deseado
        while (Mathf.Abs(hinge.angle - targetAngle) > 1f)
        {
            yield return null;
        }

        // Buffer de estabilización
        yield return new WaitForSeconds(0.5f);

        hinge.useSpring = false;    // Desactiva el resorte

        Debug.Log("Tapa reached target and spring disabled.");
    }

    // Instancia el n�mero de prefabs peque�os seg�n el valor del slider, usando corutina
    public void InstanciarPeque�as()
    {
        if (ArosPeque�os == null || PrefabPeque�os == null || pequeSd == null)
            return;

        int cantidad = Mathf.RoundToInt(pequeSd.value);
        StartCoroutine(InstanciarPeque�asCoroutine(cantidad));
    }

    private IEnumerator InstanciarPeque�asCoroutine(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Instantiate(PrefabPeque�os, ArosPeque�os);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // Instancia el n�mero de prefabs grandes seg�n el valor del slider, usando corutina
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
        ClearChildren(ArosPeque�os);
    }
    
    // Destruye todos los hijos de un Transform
    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
