using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugRuler : MonoBehaviour
{
    [SerializeField]
    private TMP_Text debugText; // Reference to the TextMeshPro Text component

    [SerializeField]
    private List<GameObject> objects; // List of game objects

    [SerializeField]
    private AsientoRotatorio asientoRotatorio; // Referencia al AsientoRotatorio

    // Update is called once per frame
    void Update()
    {
        if (debugText != null)
        {
            string positions = GetPositions2();


            //string pelotaInfo = GetPelotaInfo();
            debugText.text = positions;
        }
    }

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


    // Método para mostrar el estado de pelotaNeeded y pelotaWanted
    private string GetPelotaInfo()
    {
        if (asientoRotatorio != null)
        {
            return $"pelotaNeeded: {asientoRotatorio.pelotaNeeded}\npelotaWanted: {asientoRotatorio.pelotaWanted}\n";
        }
        else
        {
            return "AsientoRotatorio no asignado.\n";
        }
    }
}
