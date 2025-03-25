using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugLogger : MonoBehaviour
{
    [SerializeField]
    private TMP_Text debugText; // Reference to the TextMeshPro Text component

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (debugText != null)
        {
            debugText.text += logString + "\n";
        }
    }

    // Method to clear the debug text
    public void ClearLog()
    {
        if (debugText != null)
        {
            debugText.text = string.Empty;
        }
    }
}

