using System.Collections.Generic;
using TMPro;
using Unity.VRTemplate;
using UnityEngine;
using UnityEngine.VFX;

public class DebugRuler : MonoBehaviour
{
    /*
     Herramienta de depuración para mostrar información detallada
     de objetos en escena, incluyendo transformaciones, posiciones,
     propiedades de efectos visuales y estadísticas de física.
    */

    // Texto de depuración para mostrar la información
    [SerializeField]
    private TMP_Text debugText; // Reference to the TextMeshPro Text component

    // Lista de objetos a monitorear
    [SerializeField]
    private List<GameObject> objects; // List of game objects

    // Efecto visual estático para propiedades VFX
    public VisualEffect staticFieldVFX;

    /*[SerializeField]
    private XRKnob rueda;*/

    //[SerializeField]
    //private AsientoRotatorio asientoRotatorio; // Referencia al AsientoRotatorio

    // Actualiza el texto de depuración en cada frame
    void Update()
    {
        if (debugText != null)
        {
            // Obtiene la descripción de la transformación del primer objeto
            string salida = DescribeTransform();


            //string pelotaInfo = GetPelotaInfo();
            debugText.text = salida;
        }
    }

    // Describe la transformación del primer objeto en la lista
    public string DescribeTransform()
    {
        // Obtiene la transformación del primer objeto
        Transform t = objects[0].transform;

        return $"Object: {this.name}\n" +
               $"Position: {t.position}\n" +
               $"Rotation (Euler): {t.rotation.eulerAngles}\n" +
               $"Rotation: {t.rotation}\n" +
               $"Rotation (LocalEuler): {t.localRotation.eulerAngles}\n" +
               $"Rotation: {t.localRotation}\n" +
               $"Scale: {t.localScale}";
    }

    // Obtiene las propiedades del efecto visual
    private string GetVFXProperties()
    {
        if (staticFieldVFX == null)
            return "VisualEffect no asignado.\n";

        // Verifica y obtiene las propiedades booleanas del VFX
        bool intruder1 = staticFieldVFX.HasBool("Atractor1") ? staticFieldVFX.GetBool("Atractor1") : false;
        bool intruder2 = staticFieldVFX.HasBool("Atractor2") ? staticFieldVFX.GetBool("Atractor2") : false;

        // Verifica y obtiene las posiciones vectoriales del VFX
        Vector3 intruderTip = staticFieldVFX.HasVector3("IntruderPosition") ? staticFieldVFX.GetVector3("IntruderPosition") : Vector3.zero;
        Vector3 intruderTip2 = staticFieldVFX.HasVector3("IntruderPosition2") ? staticFieldVFX.GetVector3("IntruderPosition2") : Vector3.zero;

        // Construye la cadena de resultado con las propiedades
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

    // Obtiene las posiciones de los objetos en la lista
    private string GetPositions()
    {
        string positions = "";

        if (objects != null && objects.Count > 0)
        {
            // Itera sobre cada objeto en la lista
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    // Agrega la posición del objeto a la cadena
                    positions += $"Object {i + 1} Position: {objects[i].transform.position}\n";
                }
            }
        }

        return positions;
    }

    // Obtiene las posiciones y rotaciones de los hijos de los objetos
    private string GetPositions2()
    {
        string positions = "";

        if (objects != null && objects.Count > 0)
        {
            // Itera sobre cada objeto padre
            for (int i = 0; i < objects.Count; i++)
            {
                GameObject parent = objects[i];
                if (parent != null)
                {
                    // Obtiene el número de hijos
                    int childCount = parent.transform.childCount;

                    // Itera sobre cada hijo
                    for (int j = 0; j < childCount; j++)
                    {
                        Transform child = parent.transform.GetChild(j);
                        if (child != null)
                        {
                            // Agrega la posición y rotación del hijo a la cadena
                            positions += $"Object {i + 1} Child {j + 1} Position: {child.position}  Rotation: {child.rotation.eulerAngles}\n";
                        }
                    }
                }
            }
        }

        return positions;
    }

    // Obtiene estadísticas de los componentes Rigidbody de los objetos
    private string GetRigidBodyStats()
    {
        string stats = "";

        if (objects != null && objects.Count > 0)
        {
            // Itera sobre cada objeto
            for (int i = 0; i < objects.Count; i++)
            {
                GameObject obj = objects[i];
                if (obj != null)
                {
                    // Intenta obtener el componente Rigidbody
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Construye la cadena con las estadísticas del Rigidbody
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
                        // Si no tiene Rigidbody, indica que no se encontró
                        stats += $"Object {i + 1} ({obj.name}): No Rigidbody found.\n";
                    }
                }
            }
        }

        return stats;
    }

    // M�todo para mostrar el estado de pelotaNeeded y pelotaWanted
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
