using TMPro;
using UnityEngine;

public class DebugRuler : MonoBehaviour
{
    [SerializeField]
    private TMP_Text debugText; // Reference to the TextMeshPro Text component

    [SerializeField]
    private GameObject object1; // Reference to the first game object

    [SerializeField]
    private GameObject object2; // Reference to the second game object

    [SerializeField]
    private GameObject object3; // Reference to the third game object

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

        if (object1 != null)
        {
            positions += $"Object 1 Position: {object1.transform.position}\n";
        }

        if (object2 != null)
        {
            positions += $"Object 2 Position: {object2.transform.position}\n";
        }

        if (object3 != null)
        {
            positions += $"Object 3 Position: {object3.transform.position}\n";
        }

        return positions;
    }
}
