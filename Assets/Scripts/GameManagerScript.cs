using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerScript : MonoBehaviour
{

    [SerializeField]
    private GameObject toggleObject; // Reference to the toggle GameObject

    [SerializeField]
    private List<GameObject> objectsToToggle; // List of GameObjects to toggle

    private Toggle toggle; // Reference to the Toggle component


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (toggleObject != null)
        {
            toggle = toggleObject.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(delegate { SetObjectsActive(); });
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
}
