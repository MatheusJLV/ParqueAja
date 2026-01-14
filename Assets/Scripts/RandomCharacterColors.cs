using UnityEngine;
using TMPro;

// Aplica colores aleatorios a cada carácter individual de un texto TextMeshPro
public class RandomCharacterColors : MonoBehaviour
{
    public TMP_Text textMeshProComponent; // Componente TextMeshProUGUI o TextMeshPro a colorear

    // Inicializa el script aplicando colores aleatorios al texto al comenzar
    void Start()
    {
        if (textMeshProComponent == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            return;
        }

        ApplyRandomColorsToCharacters(textMeshProComponent.text);
    }

    // Genera colores aleatorios para cada carácter del texto y aplica tags de rich text
    void ApplyRandomColorsToCharacters(string originalText)
    {
        string coloredText = "";
        foreach (char c in originalText)
        {
            // Generate a random color
            Color randomColor = new Color(Random.value, Random.value, Random.value);

            // Convert the color to a hexadecimal string for the rich text tag
            string hexColor = ColorUtility.ToHtmlStringRGB(randomColor);

            // Append the color tag and the character
            coloredText += $"<color=#{hexColor}>{c}</color>";
        }

        textMeshProComponent.text = coloredText;
    }
}
