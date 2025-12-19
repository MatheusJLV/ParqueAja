using UnityEngine;
/*
 Controla dos cubos y permite restaurar
 su posición y rotación inicial en la escena.
*/
public class CampSimEnhanced : MonoBehaviour
{
    // Cubos asignados desde el Inspector
    [Header("Cubes to manage")]
    public GameObject cubeA;
    public GameObject cubeB;

    // Almacenan la posición y rotación inicial de cada cubo
    private Vector3 cubeAInitialPos;
    private Quaternion cubeAInitialRot;
    private Vector3 cubeBInitialPos;
    private Quaternion cubeBInitialRot;

    // Guarda los valores iniciales al iniciar la escena
    void Start()
    {
        if (cubeA != null)
        {
            cubeAInitialPos = cubeA.transform.position;
            cubeAInitialRot = cubeA.transform.rotation;
        }

        if (cubeB != null)
        {
            cubeBInitialPos = cubeB.transform.position;
            cubeBInitialRot = cubeB.transform.rotation;
        }
    }

    // Restaura los cubos a su estado original
    public void ResetCubos()
    {
        if (cubeA != null)
        {
            cubeA.transform.position = cubeAInitialPos;
            cubeA.transform.rotation = cubeAInitialRot;
        }

        if (cubeB != null)
        {
            cubeB.transform.position = cubeBInitialPos;
            cubeB.transform.rotation = cubeBInitialRot;
        }
        // Mensaje de confirmación en consola
        Debug.Log("CuboReset: Cubes reset to initial positions and rotations.");
    }
}
