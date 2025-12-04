using System.Collections;
using UnityEngine;
using TMPro;

public class PerformanceLogger : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text debugText;

    [Header("Settings")]
    [SerializeField] private float logInterval = 1f;

    private float deltaTime = 0.0f;
    private float timeAccumulator = 0.0f;
    private int frameCount = 0;

    private void Start()
    {
        StartCoroutine(LogPerformanceRoutine());
    }

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        timeAccumulator += Time.unscaledDeltaTime;
        frameCount++;
    }

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

    public void ClearLog()
    {
        if (debugText != null)
            debugText.text = string.Empty;
    }
}
