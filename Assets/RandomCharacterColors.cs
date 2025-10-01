using UnityEngine;
using TMPro;

public class RandomCharacterColors : MonoBehaviour
{
    public TMP_Text textMeshProComponent; // Assign your TextMeshProUGUI or TextMeshPro component in the Inspector

    void Start()
    {
        if (textMeshProComponent == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            return;
        }

        ApplyRandomColorsToCharacters(textMeshProComponent.text);
    }

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
