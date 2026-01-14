using System.Collections;
using UnityEngine;
using TMPro;

// Registrador de rendimiento: monitorea FPS, tiempo de frame y uso de memoria en tiempo real.
public class PerformanceLogger : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text debugText; // Texto donde se muestra el log de rendimiento

    [Header("Settings")]
    [SerializeField] private float logInterval = 1f; // Intervalo en segundos entre registros

    private float deltaTime = 0.0f;         // Promedio de tiempo entre frames
    private float timeAccumulator = 0.0f;   // Acumulador de tiempo
    private int frameCount = 0;             // Contador de frames en el intervalo

    // Inicia la corutina de logging de rendimiento.
    private void Start()
    {
        StartCoroutine(LogPerformanceRoutine());
    }

    // Cada frame: actualiza el acumulador de tiempo y el contador de frames.
    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        timeAccumulator += Time.unscaledDeltaTime;
        frameCount++;
    }

    // Corutina que registra FPS, tiempo de frame y uso de memoria a intervalos regulares.
    private IEnumerator LogPerformanceRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(logInterval);

            float fps = frameCount / timeAccumulator;
            float msPerFrame = 1000.0f / Mathf.Max(fps, 0.0001f);
            long memoryUsedMB = System.GC.GetTotalMemory(false) / (1024 * 1024);

            string logLine = $"[{System.DateTime.Now:HH:mm:ss}] FPS: {fps:F1}, Frame: {msPerFrame:F2} ms, RAM: {memoryUsedMB} MB";

            if (debugText != null)
                debugText.text = logLine + "\n";

            frameCount = 0;
            timeAccumulator = 0f;
        }
    }

    // Limpia el texto del log de rendimiento.
    public void ClearLog()
    {
        if (debugText != null)
            debugText.text = string.Empty;
    }
}
