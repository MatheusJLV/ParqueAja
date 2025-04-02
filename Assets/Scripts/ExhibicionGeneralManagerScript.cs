using System.Collections.Generic;
using UnityEngine;

public class ExhibicionGeneralManagerScript : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> exhibiciones; // List of game objects containing ExhibicionScript

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Debug log
        Debug.Log("ExhibicionGeneralManagerScript: Start");
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Method to call Eliminar on the specified exhibicion
    public void Eliminar(string nombre)
    {
        // Debug log
        Debug.Log("ExhibicionGeneralManagerScript: Eliminar");

        try
        {
            ExhibicionScript exhibicion = FindExhibicionByName(nombre);
            if (exhibicion != null)
            {
                exhibicion.Eliminar();
            }
            else
            {
                Debug.LogError("ExhibicionScript is null in Eliminar method of ExhibicionGeneralManagerScript");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in Eliminar method of ExhibicionGeneralManagerScript: " + ex.Message);
        }
    }

    // Method to call Cargar on the specified exhibicion
    public void Cargar(string nombre)
    {
        // Debug log
        Debug.Log("ExhibicionGeneralManagerScript: Cargar");

        try
        {
            ExhibicionScript exhibicion = FindExhibicionByName(nombre);
            if (exhibicion != null)
            {
                exhibicion.Cargar();
            }
            else
            {
                Debug.LogError("ExhibicionScript is null in Cargar method of ExhibicionGeneralManagerScript");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in Cargar method of ExhibicionGeneralManagerScript: " + ex.Message);
        }
    }

    // Method to call ResetExhibicion on the specified exhibicion
    public void ResetExhibicion(string nombre)
    {
        // Debug log
        Debug.Log("ExhibicionGeneralManagerScript: ResetExhibicion");

        try
        {
            ExhibicionScript exhibicion = FindExhibicionByName(nombre);
            if (exhibicion != null)
            {
                exhibicion.ResetExhibicion();
            }
            else
            {
                Debug.LogError("ExhibicionScript is null in ResetExhibicion method of ExhibicionGeneralManagerScript");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in ResetExhibicion method of ExhibicionGeneralManagerScript: " + ex.Message);
        }
    }

    // Method to call ReactivacionExhibicion on the specified exhibicion
    public void ReactivacionExhibicion(string nombre)
    {
        // Debug log
        Debug.Log("ExhibicionGeneralManagerScript: ReactivacionExhibicion");

        try
        {
            ExhibicionScript exhibicion = FindExhibicionByName(nombre);
            if (exhibicion != null)
            {
                exhibicion.ReactivacionExhibicion();
            }
            else
            {
                Debug.LogError("ExhibicionScript is null in ReactivacionExhibicion method of ExhibicionGeneralManagerScript");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in ReactivacionExhibicion method of ExhibicionGeneralManagerScript: " + ex.Message);
        }
    }

    // Method to call SuspensionExhibicion on the specified exhibicion
    public void SuspensionExhibicion(string nombre)
    {
        // Debug log
        Debug.Log("ExhibicionGeneralManagerScript: SuspensionExhibicion");

        try
        {
            ExhibicionScript exhibicion = FindExhibicionByName(nombre);
            if (exhibicion != null)
            {
                exhibicion.SuspensionExhibicion();
            }
            else
            {
                Debug.LogError("ExhibicionScript is null in SuspensionExhibicion method of ExhibicionGeneralManagerScript");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in SuspensionExhibicion method of ExhibicionGeneralManagerScript: " + ex.Message);
        }
    }

    // Helper method to find an ExhibicionScript by name
    private ExhibicionScript FindExhibicionByName(string nombre)
    {
        // Debug log
        Debug.Log("ExhibicionGeneralManagerScript: FindExhibicionByName");

        try
        {
            foreach (GameObject obj in exhibiciones)
            {
                if (obj != null && obj.name == nombre)
                {
                    return obj.GetComponent<ExhibicionScript>();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception in FindExhibicionByName method of ExhibicionGeneralManagerScript: " + ex.Message);
        }
        return null;
    }
}
