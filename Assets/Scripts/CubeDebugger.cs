using TMPro;
using UnityEngine;
using System.Text;

/*
 Muestra información de depuración en pantalla
 sobre el estado interno y externo de un cubo espacial.
*/

public class CubeDebugger : MonoBehaviour
{
    // Referencias necesarias para la depuración
    [Header("References")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private CuboEspacialEnhanced cubeLogic;
    
    // Configuración de refresco del texto
    [Header("Update Settings")]
    [SerializeField] private float refreshRate = 0.2f;

    // Temporizador interno
    private float timer;

    private void Update()
    {
        // Verifica referencias obligatorias
        if (debugText == null || cubeLogic == null)
            return;

        // Controla la frecuencia de actualización
        timer += Time.deltaTime;
        if (timer >= refreshRate)
        {
            timer = 0f;
            UpdateDebugText();
        }
    }

    // Actualiza el texto de depuración
    private void UpdateDebugText()
    {
        StringBuilder sb = new StringBuilder();

        // Información de límites internos
        sb.AppendLine("<b>Internal Boundaries</b>");
        sb.AppendLine($"Complete: {cubeLogic.internalBoundries.complete}");
        sb.AppendLine($"CurrentCount: {cubeLogic.internalBoundries.cantidad}");

        // Información de límites externos
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
        // Asigna el texto al componente UI
        debugText.text = sb.ToString();
    }
}

