using TMPro;
using UnityEngine;
using System.Text;

public class CubeDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private CuboEspacialEnhanced cubeLogic;

    [Header("Update Settings")]
    [SerializeField] private float refreshRate = 0.2f;

    private float timer;

    private void Update()
    {
        if (debugText == null || cubeLogic == null)
            return;

        timer += Time.deltaTime;
        if (timer >= refreshRate)
        {
            timer = 0f;
            UpdateDebugText();
        }
    }

    private void UpdateDebugText()
    {
        StringBuilder sb = new StringBuilder();

        // Internal
        sb.AppendLine("<b>Internal Boundaries</b>");
        sb.AppendLine($"Complete: {cubeLogic.internalBoundries.complete}");
        sb.AppendLine($"CurrentCount: {cubeLogic.internalBoundries.cantidad}");

        // Externals
        sb.AppendLine("\n<b>External Boundaries</b>");
        for (int i = 0; i < cubeLogic.externalBoundries.Count; i++)
        {
            var bound = cubeLogic.externalBoundries[i];
            if (bound != null)
            {
                sb.AppendLine($"[{i}] {bound.name} ? inside={bound.hayObjetoDentro} | count={bound.cantidad}");
            }
            else
            {
                sb.AppendLine($"[{i}] NULL reference");
            }
        }

        debugText.text = sb.ToString();
    }
}

