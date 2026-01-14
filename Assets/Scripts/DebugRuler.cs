using System.Collections.Generic;
using TMPro;
using Unity.VRTemplate;
using UnityEngine;
using UnityEngine.VFX;

public class DebugRuler : MonoBehaviour
{
    // Utilidad visual para inspeccionar transformaciones y propiedades físicas en tiempo de ejecución
    [SerializeField]
    private TMP_Text debugText; // Referencia al componente TextMeshPro Text

    [SerializeField]
    private List<GameObject> objects; // Lista de objetos a inspeccionar

    public VisualEffect staticFieldVFX;

    // VFX bindings permiten verificar que los parámetros de atracción estén llegando al sistema visual

    /*[SerializeField]
    private XRKnob rueda;*/

    //[SerializeField]
    //private AsientoRotatorio asientoRotatorio; // Referencia al AsientoRotatorio

    // Se ejecuta una vez por frame
    void Update()
    {
        // Evita errores si el texto de depuración no está asignado en escena
        if (debugText != null)
        {
            string salida = DescribeTransform();


            //string pelotaInfo = GetPelotaInfo();
            debugText.text = salida;
        }
    }

    public string DescribeTransform()
    {
        // Siempre toma el primer objeto de la lista como referencia de medición
        Transform t = objects[0].transform;

        // Devuelve un bloque de texto legible para monitorear posición, rotación y escala en consola/UI
        return $"Object: {this.name}\n" +
               $"Position: {t.position}\n" +
               $"Rotation (Euler): {t.rotation.eulerAngles}\n" +
               $"Rotation: {t.rotation}\n" +
               $"Rotation (LocalEuler): {t.localRotation.eulerAngles}\n" +
               $"Rotation: {t.localRotation}\n" +
               $"Scale: {t.localScale}";
    }

    private string GetVFXProperties()
    {
        // Recolecta los flags y posiciones del sistema VFX para depuración
        if (staticFieldVFX == null)
            return "VisualEffect no asignado.\n";

        bool intruder1 = staticFieldVFX.HasBool("Atractor1") ? staticFieldVFX.GetBool("Atractor1") : false;
        bool intruder2 = staticFieldVFX.HasBool("Atractor2") ? staticFieldVFX.GetBool("Atractor2") : false;
        Vector3 intruderTip = staticFieldVFX.HasVector3("IntruderPosition") ? staticFieldVFX.GetVector3("IntruderPosition") : Vector3.zero;
        Vector3 intruderTip2 = staticFieldVFX.HasVector3("IntruderPosition2") ? staticFieldVFX.GetVector3("IntruderPosition2") : Vector3.zero;

        string result = $"Atractor1: {intruder1}\n" +
                        $"Atractor2: {intruder2}\n" +
                        $"IntruderTip: {intruderTip}\n" +
                        $"IntruderTip2: {intruderTip2}\n";

        return result;
    }

    /*private string getAngle()
    {
        string angle;
        angle = "" + rueda.value;
        return angle;
    }*/

    // Método para obtener las posiciones de los game objects
    private string GetPositions()
    {
        string positions = "";

        if (objects != null && objects.Count > 0)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    // Reporta la posición mundial de cada objeto observado
                    positions += $"Object {i + 1} Position: {objects[i].transform.position}\n";
                }
            }
        }

        return positions;
    }

    private string GetPositions2()
    {
        string positions = "";

        if (objects != null && objects.Count > 0)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                GameObject parent = objects[i];
                if (parent != null)
                {
                    int childCount = parent.transform.childCount;
                    for (int j = 0; j < childCount; j++)
                    {
                        Transform child = parent.transform.GetChild(j);
                        if (child != null)
                        {
                            // Muestra posición y rotación de cada hijo directo para diagnosticar jerarquías
                            positions += $"Object {i + 1} Child {j + 1} Position: {child.position}  Rotation: {child.rotation.eulerAngles}\n";
                        }
                    }
                }
            }
        }

        return positions;
    }
    private string GetRigidBodyStats()
    {
        string stats = "";

        if (objects != null && objects.Count > 0)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                GameObject obj = objects[i];
                if (obj != null)
                {
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Lista propiedades clave del Rigidbody para revisar configuraciones físicas
                        stats += $"Object {i + 1} ({obj.name}) Rigidbody:\n";
                        stats += $"  Mass: {rb.mass}\n";
                        stats += $"  Linear Damping: {rb.linearDamping}\n";
                        stats += $"  Angular Damping: {rb.angularDamping}\n";
                        stats += $"  Use Gravity: {rb.useGravity}\n";
                        stats += $"  Is Kinematic: {rb.isKinematic}\n";
                        stats += $"  Interpolation: {rb.interpolation}\n";
                        stats += $"  Collision Detection: {rb.collisionDetectionMode}\n";
                        stats += $"  Constraints: {rb.constraints}\n";
                        stats += $"  Velocity: {rb.linearVelocity}\n";
                        stats += $"  Angular Velocity: {rb.angularVelocity}\n";
                    }
                    else
                    {
                        stats += $"Object {i + 1} ({obj.name}): No Rigidbody found.\n";
                    }
                }
            }
        }

        return stats;
    }

    // Método para mostrar el estado de pelotaNeeded y pelotaWanted
    /*private string GetPelotaInfo()
    {
        if (asientoRotatorio != null)
        {
            return $"pelotaNeeded: {asientoRotatorio.pelotaNeeded}\npelotaWanted: {asientoRotatorio.pelotaWanted}\n";
        }
        else
        {
            return "AsientoRotatorio no asignado.\n";
        }
    }*/
}
