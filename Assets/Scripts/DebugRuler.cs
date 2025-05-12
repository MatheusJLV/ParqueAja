using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugRuler : MonoBehaviour
{
    [SerializeField]
    private TMP_Text debugText; // Reference to the TextMeshPro Text component

    [SerializeField]
    private List<GameObject> objects; // List of game objects

    // Update is called once per frame
    void Update()
    {
        if (debugText != null)
        {
            string positions = GetPositions();
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
}
