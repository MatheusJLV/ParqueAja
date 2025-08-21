using UnityEngine;

public class CampSimEnhanced : MonoBehaviour
{
    [Header("Cubes to manage")]
    public GameObject cubeA;
    public GameObject cubeB;

    // Internal storage for initial transforms
    private Vector3 cubeAInitialPos;
    private Quaternion cubeAInitialRot;
    private Vector3 cubeBInitialPos;
    private Quaternion cubeBInitialRot;

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

        Debug.Log("CuboReset: Cubes reset to initial positions and rotations.");
    }
}
