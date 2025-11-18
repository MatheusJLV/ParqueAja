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
        // Inicializar referencia al Toggle y añadir listener para cambiar visibilidad de objetos.
        if (toggleObject != null)
        {
            toggle = toggleObject.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(delegate { SetObjectsActive(); });
        }
        // Inicializar dropdown principal (teletransporte) y añadir listener.
        if (dropdownObject != null)
        {
            dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            dropdown.onValueChanged.AddListener(delegate { TeleportToSelectedAnchor(); });
        }

        // Añadir listeners a los dropdowns de los controladores (si existen).
        if (leftDropdown != null)
        {
            leftDropdown.onValueChanged.AddListener(delegate { HandleLeftDropdownSelection(); });
        }

        if (rightDropdown != null)
        {
            rightDropdown.onValueChanged.AddListener(delegate { HandleRightDropdownSelection(); });
        }
    }

    // Update se llama una vez por frame. 
    void Update()
    {

    }
    //Recarga la escena actual.
    //Uso: reiniciar el estado del nivel sin cambiar de escena.
    public void ReloadCurrentScene()
    {
        // Get the current active scene
        Scene currentScene = SceneManager.GetActiveScene();
        // Reload the current scene
        SceneManager.LoadScene(currentScene.name);
    }
    // Activa o desactiva los objetos listados según el estado del Toggle.
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
    // Teletransporta al jugador al anchor seleccionado en el dropdown principal.
    // Busca entre los GameObjects de teleportationAnchors por nombre que contenga la opción seleccionada.
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
                        break; // Finalizar búsqueda tras solicitar teletransporte
                    }
                }
            }
        }
    }

    // Method to handle the Left Dropdown selection
    // Opciones esperadas: "Near", "Far", "Hibrido".
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

    // Method to handle the Right Dropdown selection.
    // Ajusta las propiedades enableNearCasting / enableFarCasting del Interactor derecho.
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
