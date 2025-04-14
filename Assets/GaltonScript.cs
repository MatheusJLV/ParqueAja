using System.Collections;
using UnityEngine;

public class GaltonScript : MonoBehaviour
{
    [SerializeField]
    private GameObject bolas; // Parent GameObject to hold the instantiated objects

    [SerializeField]
    private GameObject bola; // Prefab or GameObject to be instantiated

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // Method to instantiate a specified number of "bola" objects as children of "bolas"
    public void InstanciarBolas(int numberOfBolas = 1)
    {
        // Check if 'bolas' is null and find it as a child of this GameObject
        if (bolas == null)
        {
            bolas = transform.Find("bolas")?.gameObject;

            // Log a warning if 'bolas' is still null after attempting to find it
            if (bolas == null)
            {
                Debug.LogWarning("Bolas GameObject not found as a child of this GameObject.");
                return;
            }
        }

        // Start the coroutine to instantiate the bolas
        StartCoroutine(InstantiateBolasCoroutine(numberOfBolas));
    }

    // Coroutine to handle the instantiation with a delay
    private IEnumerator InstantiateBolasCoroutine(int numberOfBolas)
    {
        for (int i = 0; i < numberOfBolas; i++)
        {
            if (bolas != null && bola != null)
            {
                // Instantiate the "bola" object at the position of "bolas" and set its parent
                GameObject newBola = Instantiate(bola, bolas.transform.position, Quaternion.identity);
                newBola.transform.SetParent(bolas.transform);
            }

            // Wait for half a second before instantiating the next object
            yield return new WaitForSeconds(0.5f);
        }
    }
}
