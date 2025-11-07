using TMPro;
using UnityEngine;
using UnityEngine.UI;

/* Este script muestra en un texto de la UI (TextMeshPro) los mensajes de debug
 que normalmente aparecerían en la consola de Unity.
 También permite limpiar el texto mediante un método público.*/
public class DebugLogger : MonoBehaviour
{
    [SerializeField]
    private TMP_Text debugText; // Reference to the TextMeshPro Text component

    // ===== Suscripción a eventos =====
    private void OnEnable()
    {
        // Se suscribe al evento de mensajes de log de Unity
        // Cada vez que Unity genera un log, se llamará a HandleLog
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        // Se desuscribe del evento al desactivar el objeto
        // Esto evita errores y referencias inválidas
        Application.logMessageReceived -= HandleLog;
    }

    // ===== Manejo de logs =====
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
         // Verifica que la referencia al TMP_Text no sea nula
        if (debugText != null)
        {
            // Añade el mensaje recibido al texto existente
            debugText.text += logString + "\n";
        }
    }

    // Method to clear the debug text
    public void ClearLog()
    {
        if (debugText != null)
        {
            debugText.text = string.Empty; // Vacía el contenido del texto
        }
    }
}

