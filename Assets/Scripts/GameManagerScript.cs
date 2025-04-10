using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
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

    // New variables for Left and Right controllers
    [SerializeField]
    private NearFarInteractor leftNearFarInteractor; // Reference to the Near-Far Interactor for the left controller

    [SerializeField]
    private TMP_Dropdown leftDropdown; // Reference to the Dropdown for the left controller

    [SerializeField]
    private NearFarInteractor rightNearFarInteractor; // Reference to the Near-Far Interactor for the right controller

    [SerializeField]
    private TMP_Dropdown rightDropdown; // Reference to the Dropdown for the right controller

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

        // Add listeners for the Left and Right dropdowns
        if (leftDropdown != null)
        {
            leftDropdown.onValueChanged.AddListener(delegate { HandleLeftDropdownSelection(); });
        }

        if (rightDropdown != null)
        {
            rightDropdown.onValueChanged.AddListener(delegate { HandleRightDropdownSelection(); });
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

    // Method to handle the Left Dropdown selection
    public void HandleLeftDropdownSelection()
    {
        if (leftDropdown != null && leftNearFarInteractor != null)
        {
            string selectedOption = leftDropdown.options[leftDropdown.value].text;

            switch (selectedOption)
            {
                case "Near":
                    leftNearFarInteractor.enableNearCasting = true;
                    leftNearFarInteractor.enableFarCasting = false;
                    break;

                case "Far":
                    leftNearFarInteractor.enableNearCasting = false;
                    leftNearFarInteractor.enableFarCasting = true;
                    break;

                case "Hibrido":
                    leftNearFarInteractor.enableNearCasting = true;
                    leftNearFarInteractor.enableFarCasting = true;
                    break;

                default:
                    Debug.LogWarning("Unknown option selected in Left Dropdown.");
                    break;
            }
        }
    }

    // Method to handle the Right Dropdown selection
    public void HandleRightDropdownSelection()
    {
        if (rightDropdown != null && rightNearFarInteractor != null)
        {
            string selectedOption = rightDropdown.options[rightDropdown.value].text;

            switch (selectedOption)
            {
                case "Near":
                    rightNearFarInteractor.enableNearCasting = true;
                    rightNearFarInteractor.enableFarCasting = false;
                    break;

                case "Far":
                    rightNearFarInteractor.enableNearCasting = false;
                    rightNearFarInteractor.enableFarCasting = true;
                    break;

                case "Hibrido":
                    rightNearFarInteractor.enableNearCasting = true;
                    rightNearFarInteractor.enableFarCasting = true;
                    break;

                default:
                    Debug.LogWarning("Unknown option selected in Right Dropdown.");
                    break;
            }
        }
    }
}
