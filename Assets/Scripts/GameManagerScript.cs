using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class GameManagerScript : MonoBehaviour
{
    [SerializeField]
    private GameObject toggleObject; // Reference to the toggle GameObject

    [SerializeField]
    private List<GameObject> objectsToToggle; // List of GameObjects to toggle

    [SerializeField]
    private GameObject dropdownObject; // Reference to the dropdown GameObject

    [SerializeField]
    private List<GameObject> teleportationAnchors; // List of GameObjects containing TeleportationAnchor component

    private Toggle toggle; // Reference to the Toggle component
    private TMP_Dropdown dropdown; // Reference to the TMP_Dropdown component

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (toggleObject != null)
        {
            toggle = toggleObject.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(delegate { SetObjectsActive(); });
        }

        if (dropdownObject != null)
        {
            dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            dropdown.onValueChanged.AddListener(delegate { TeleportToSelectedAnchor(); });
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ReloadCurrentScene()
    {
        // Get the current active scene
        Scene currentScene = SceneManager.GetActiveScene();
        // Reload the current scene
        SceneManager.LoadScene(currentScene.name);
    }

    public void SetObjectsActive()
    {
        if (toggle != null)
        {
            bool isActive = toggle.isOn;
            foreach (GameObject obj in objectsToToggle)
            {
                if (obj != null)
                {
                    obj.SetActive(isActive);
                }
            }
        }
    }

    public void TeleportToSelectedAnchor()
    {
        if (dropdown != null)
        {
            string selectedOption = dropdown.options[dropdown.value].text;
            foreach (GameObject anchorObject in teleportationAnchors)
            {
                if (anchorObject != null && anchorObject.name.Contains(selectedOption))
                {
                    TeleportationAnchor anchor = anchorObject.GetComponent<TeleportationAnchor>();
                    if (anchor != null)
                    {
                        anchor.RequestTeleport();
                        break;
                    }
                }
            }
        }
    }
}
