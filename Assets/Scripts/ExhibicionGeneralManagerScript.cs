using System.Collections.Generic;
using UnityEngine;

/*
 * ExhibicionGeneralManagerScript:
 * Gestiona las exhibiciones generales en el parque temático VR, permitiendo
 * cargar, eliminar, resetear, reactivar y suspender exhibiciones individuales,
 * además de calibrar posiciones de objetos basados en la altura de la cámara.
 */

public class ExhibicionGeneralManagerScript : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> exhibiciones; // Lista de objetos de juego que contienen ExhibicionScript

    [SerializeField]
    private List<GameObject> calibracionTarget; // Lista de objetos de juego a calibrar

    [SerializeField]
    private GameObject AlturaCamara; // Referencia al objeto de altura de la cámara

    private const float referenceHeightDif = 0.6f; // Altura de referencia para calibración

    [SerializeField]
    private Transform PtoReferencia; // Punto de referencia para calibración

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Log de debug al iniciar
        Debug.Log("ExhibicionGeneralManagerScript: Start");
    }

    // Update is called once per frame
    void Update()
    {
        // Método vacío, sin lógica por ahora
    }

    // Método para llamar Eliminar en la exhibición especificada
    public void Eliminar(string nombre)
    {
        // Log de debug
        Debug.Log("ExhibicionGeneralManagerScript: Eliminar");

        try
        {
            // Buscar la exhibición por nombre y llamar Eliminar
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

    // Método para llamar Cargar en la exhibición especificada
    public void Cargar(string nombre)
    {
        // Log de debug
        Debug.Log("ExhibicionGeneralManagerScript: Cargar");

        try
        {
            // Buscar la exhibición por nombre y llamar Cargar
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

    // Método para llamar ResetExhibicion en la exhibición especificada
    public void ResetExhibicion(string nombre)
    {
        // Log de debug
        Debug.Log("ExhibicionGeneralManagerScript: ResetExhibicion");

        try
        {
            // Buscar la exhibición por nombre y llamar ResetExhibicion
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

    // Método para llamar ReactivacionExhibicion en la exhibición especificada
    public void ReactivacionExhibicion(string nombre)
    {
        // Log de debug
        Debug.Log("ExhibicionGeneralManagerScript: ReactivacionExhibicion");

        try
        {
            // Buscar la exhibición por nombre y llamar ReactivacionExhibicion
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

// Método para llamar SuspensionExhibicion en la exhibición especificada
    public void SuspensionExhibicion(string nombre)
    {
        // Log de debug
        Debug.Log("ExhibicionGeneralManagerScript: SuspensionExhibicion");

        try
        {
            // Buscar la exhibición por nombre y llamar SuspensionExhibicion
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

    // Método para calibrar las posiciones de los objetos en calibracionTarget
    public void Calibrar()
    {
        // Verificar que AlturaCamara esté asignada
        if (AlturaCamara != null)
        {
            // Calcular altura de la cámara y referencia
            float alturaCamaraY = AlturaCamara.transform.position.y;
            float referencia = PtoReferencia.position.y;
            // Calcular ajuste basado en diferencia de referencia
            float adjustment = referenceHeightDif - (alturaCamaraY - referencia);

            // Aplicar ajuste a cada target en la lista
            foreach (GameObject target in calibracionTarget)
            {
                if (target != null)
                {
                    Vector3 newPosition = target.transform.position;
                    newPosition.y -= adjustment; // Ajustar altura
                    target.transform.position = newPosition;
                }
            }
        }
        else
        {
            Debug.LogError("AlturaCamara is not set in Calibrar method of ExhibicionGeneralManagerScript");
        }
    }

    // Método auxiliar para encontrar un ExhibicionScript por nombre
    private ExhibicionScript FindExhibicionByName(string nombre)
    {
        // Log de debug
        Debug.Log("ExhibicionGeneralManagerScript: FindExhibicionByName");

        try
        {
            // Buscar en la lista de exhibiciones por nombre
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
        return null; // Retornar null si no se encuentra
    }
}
