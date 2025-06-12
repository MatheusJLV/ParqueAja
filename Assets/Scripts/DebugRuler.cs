using System.Collections.Generic;
using TMPro;
using Unity.VRTemplate;
using UnityEngine;

public class DebugRuler : MonoBehaviour
{
    [SerializeField]
    private TMP_Text debugText; // Reference to the TextMeshPro Text component

    [SerializeField]
    private List<GameObject> objects; // List of game objects

    /*[SerializeField]
    private XRKnob rueda;*/

    //[SerializeField]
    //private AsientoRotatorio asientoRotatorio; // Referencia al AsientoRotatorio

    // Update is called once per frame
    void Update()
    {
        if (debugText != null)
        {
            string salida = GetPositions();


            //string pelotaInfo = GetPelotaInfo();
            debugText.text = salida;
        }
    }

    /*private string getAngle()
    {
        string angle;
        angle = "" + rueda.value;
        return angle;
    }*/

    // Method to get the positions of the game objects
    private string GetPositions()
    {
        string positions = "";

        if (objects != null && objects.Count > 0)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
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
