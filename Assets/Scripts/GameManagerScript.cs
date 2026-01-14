using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class GameManagerScript : MonoBehaviour
{
    /*
     Gestor central del juego que controla toggles, dropdowns, teletransportación
     y modos de interacción (near/far) para ambos controladores.
    */

    [SerializeField]
    private GameObject toggleObject; // Referencia al GameObject con toggle

    [SerializeField]
    private List<GameObject> objectsToToggle; // Lista de GameObjects a activar/desactivar

    [SerializeField]
    private GameObject dropdownObject; // Referencia al GameObject con dropdown

    [SerializeField]
    private List<GameObject> teleportationAnchors; // Lista con componentes TeleportationAnchor

    private Toggle toggle; // Referencia al componente Toggle
    private TMP_Dropdown dropdown; // Referencia al componente TMP_Dropdown

    // Referencias para controladores izquierdo y derecho
    [SerializeField]
    private NearFarInteractor leftNearFarInteractor; // Near-Far Interactor del controlador izquierdo

    [SerializeField]
    private TMP_Dropdown leftDropdown; // Dropdown para el controlador izquierdo

    [SerializeField]
    private NearFarInteractor rightNearFarInteractor; // Near-Far Interactor del controlador derecho

    [SerializeField]
    private TMP_Dropdown rightDropdown; // Dropdown para el controlador derecho

    // Inicialización: suscribe listeners a toggle y dropdowns
    void Start()
    {
        // Configura el toggle para activar/desactivar objetos
        if (toggleObject != null)
        {
            toggle = toggleObject.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(delegate { SetObjectsActive(); });
        }

        // Configura el dropdown para teletransportación a anclajes
        if (dropdownObject != null)
        {
            dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            dropdown.onValueChanged.AddListener(delegate { TeleportToSelectedAnchor(); });
        }

        // Configura listeners para dropdowns de modos de interacción izquierdo y derecho
        if (leftDropdown != null)
        {
            leftDropdown.onValueChanged.AddListener(delegate { HandleLeftDropdownSelection(); });
        }

        if (rightDropdown != null)
        {
            rightDropdown.onValueChanged.AddListener(delegate { HandleRightDropdownSelection(); });
        }
    }

    // Update vacío: se pueden agregar lógicas por frame si es necesario
    void Update()
    {

    }

    // Recarga la escena actual
    public void ReloadCurrentScene()
    {
        // Obtiene la escena activa actual
        Scene currentScene = SceneManager.GetActiveScene();
        // Recarga la escena
        SceneManager.LoadScene(currentScene.name);
    }

    // Activa o desactiva objetos según el estado del toggle
    public void SetObjectsActive()
    {
        if (toggle != null)
        {
            bool isActive = toggle.isOn;
            // Itera sobre la lista de objetos y cambia su estado
            foreach (GameObject obj in objectsToToggle)
            {
                if (obj != null)
                {
                    obj.SetActive(isActive);
                }
            }
        }
    }

    // Teletransporta al anclaje seleccionado en el dropdown
    public void TeleportToSelectedAnchor()
    {
        if (dropdown != null)
        {
            // Obtiene el texto de la opción seleccionada
            string selectedOption = dropdown.options[dropdown.value].text;
            // Busca el anclaje que coincide con el nombre
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

    // Maneja la selección del dropdown izquierdo: cambia modo near/far/híbrido
    public void HandleLeftDropdownSelection()
    {
        if (leftDropdown != null && leftNearFarInteractor != null)
        {
            // Obtiene la opción seleccionada
            string selectedOption = leftDropdown.options[leftDropdown.value].text;

            // Cambia el modo según la opción
            switch (selectedOption)
            {
                case "Near":
                    // Solo interacción cercana
                    leftNearFarInteractor.enableNearCasting = true;
                    leftNearFarInteractor.enableFarCasting = false;
                    break;

                case "Far":
                    // Solo interacción lejana (rayo)
                    leftNearFarInteractor.enableNearCasting = false;
                    leftNearFarInteractor.enableFarCasting = true;
                    break;

                case "Hibrido":
                    // Ambos modos habilitados
                    leftNearFarInteractor.enableNearCasting = true;
                    leftNearFarInteractor.enableFarCasting = true;
                    break;

                default:
                    Debug.LogWarning("Unknown option selected in Left Dropdown.");
                    break;
            }
        }
    }

    // Maneja la selección del dropdown derecho: cambia modo near/far/híbrido
    public void HandleRightDropdownSelection()
    {
        if (rightDropdown != null && rightNearFarInteractor != null)
        {
            // Obtiene la opción seleccionada
            string selectedOption = rightDropdown.options[rightDropdown.value].text;

            // Cambia el modo según la opción
            switch (selectedOption)
            {
                case "Near":
                    // Solo interacción cercana
                    rightNearFarInteractor.enableNearCasting = true;
                    rightNearFarInteractor.enableFarCasting = false;
                    break;

                case "Far":
                    // Solo interacción lejana (rayo)
                    rightNearFarInteractor.enableNearCasting = false;
                    rightNearFarInteractor.enableFarCasting = true;
                    break;

                case "Hibrido":
                    // Ambos modos habilitados
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
