using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

public class DibujoEspacial : MonoBehaviour
{
    //public InputActionReference drawAction; // Bind this to trigger or grip
    public GameObject linePrefab;
    public Transform drawingTipRight; // Where the line originates from (usually controller tip)
    public GameObject linePrefab2;
    public Transform drawingTipLeft;
    public float minDistance = 0.01f;

    private LineRenderer currentLine;
    private LineRenderer currentLine2;
    private Vector3 lastPoint;
    private Vector3 lastPoint2;
    private bool isDrawing = false;
    private bool isDrawing2 = false;

    private bool canDraw = false;

    private List<GameObject> drawnLines = new List<GameObject>();
    private List<GameObject> drawnLines2 = new List<GameObject>();

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Asegúrate de usar el tag correcto
            canDraw = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canDraw = false;

            // Finaliza cualquier trazo activo y resetea los estados
            if (isDrawing)
                EndLine();
            if (isDrawing2)
                EndLine2();

            // Opcional: también puedes resetear los estados de los botones previos
            prevRightPrimary = false;
            prevRightSecondary = false;
            prevLeftPrimary = false;
            prevLeftSecondary = false;
        }
    }


    // Previous frame button states
    private bool prevRightPrimary = false;
    private bool prevRightSecondary = false;
    private bool prevLeftPrimary = false;
    private bool prevLeftSecondary = false;

    void Update()
    {
        if (!canDraw) return;

        var rightHandDevices = new List<InputDevice>();
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);

        bool rightPrimaryPressed = false;
        bool rightSecondaryPressed = false;
        bool leftPrimaryPressed = false;
        bool leftSecondaryPressed = false;

        foreach (var device in rightHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondaryPressed);
        }

        foreach (var device in leftHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out leftPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out leftSecondaryPressed);
        }

        // === TOGGLE DRAWING FOR RIGHT LINE (Primary Right) ===
        if (rightPrimaryPressed && !prevRightPrimary)
        {
            isDrawing = !isDrawing;
            if (isDrawing)
            {
                StartLine();
                lastPoint = drawingTipRight.position;
            }
            else
            {
                EndLine();
            }
        }

        // === TOGGLE DRAWING FOR LEFT LINE (Primary Left) ===
        if (leftPrimaryPressed && !prevLeftPrimary)
        {
            isDrawing2 = !isDrawing2;
            if (isDrawing2)
            {
                StartLine2();
                lastPoint2 = drawingTipLeft.position;
            }
            else
            {
                EndLine2();
            }
        }

        // === DRAW POINTS IF ACTIVE ===
        if (isDrawing)
        {
            Vector3 currentPos = drawingTipRight.position;
            if (Vector3.Distance(currentPos, lastPoint) > minDistance)
            {
                AddPoint(currentPos);
                lastPoint = currentPos;
            }
        }

        if (isDrawing2)
        {
            Vector3 currentPos = drawingTipLeft.position;
            if (Vector3.Distance(currentPos, lastPoint2) > minDistance)
            {
                AddPoint2(currentPos);
                lastPoint2 = currentPos;
            }
        }

        // === CLEAR RIGHT LINE (Secondary Right) ===
        if (rightSecondaryPressed && !prevRightSecondary)
        {
            ClearAllLines();
        }

        // === CLEAR LEFT LINE (Secondary Left) ===
        if (leftSecondaryPressed && !prevLeftSecondary)
        {
            ClearAllLines2();
        }

        // === UPDATE PREVIOUS BUTTON STATES ===
        prevRightPrimary = rightPrimaryPressed;
        prevRightSecondary = rightSecondaryPressed;
        prevLeftPrimary = leftPrimaryPressed;
        prevLeftSecondary = leftSecondaryPressed;
    }

    /*void Update()
    {
        if (!canDraw) return;

        // Obtén los dispositivos de las manos derecha e izquierda
        var rightHandDevices = new List<InputDevice>();
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);

        // Variables para verificar los estados de los botones
        bool rightPrimaryPressed = false;
        bool rightSecondaryPressed = false;
        bool leftPrimaryPressed = false;
        bool leftSecondaryPressed = false;

        // Verifica los botones del controlador derecho
        foreach (var device in rightHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondaryPressed);
        }

        // Verifica los botones del controlador izquierdo
        foreach (var device in leftHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out leftPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out leftSecondaryPressed);
        }

        if (rightPrimaryPressed)
        {
            if (!isDrawing)
            {
                StartLine();
            }

            Vector3 currentPos = drawingTipRight.position;
            if (Vector3.Distance(currentPos, lastPoint) > minDistance)
            {
                AddPoint(currentPos);
                lastPoint = currentPos;
            }
        }
        else if (isDrawing)
        {
            EndLine();
        }

        if (rightSecondaryPressed)
        {
            if (!isDrawing2)
            {
                StartLine2();
            }

            Vector3 currentPos = drawingTipLeft.position;
            if (Vector3.Distance(currentPos, lastPoint2) > minDistance)
            {
                AddPoint2(currentPos);
                lastPoint2 = currentPos;
            }
        }
        else if (isDrawing2)
        {
            EndLine2();
        }

        if (leftSecondaryPressed)
        {
            ClearAllLines2();
        }
        if (leftPrimaryPressed)
        {
            ClearAllLines();
        }

    }*/



    void StartLine()
    {
        GameObject lineObj = Instantiate(linePrefab);
        currentLine = lineObj.GetComponent<LineRenderer>();
        drawnLines.Add(lineObj);
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, drawingTipRight.position);
        lastPoint = drawingTipRight.position;
        isDrawing = true;
    }

    void AddPoint(Vector3 point)
    {
        currentLine.positionCount += 1;
        currentLine.SetPosition(currentLine.positionCount - 1, point);
    }

    void EndLine()
    {
        isDrawing = false;
        currentLine = null;
    }

    public void ClearAllLines()
    {
        foreach (var line in drawnLines)
        {
            if (line != null)
                Destroy(line);
        }
        drawnLines.Clear();
    }

    void StartLine2()
    {
        GameObject lineObj = Instantiate(linePrefab2);
        currentLine2 = lineObj.GetComponent<LineRenderer>();
        drawnLines2.Add(lineObj);
        currentLine2.positionCount = 1;
        currentLine2.SetPosition(0, drawingTipLeft.position);
        lastPoint = drawingTipLeft.position;
        isDrawing2 = true;
    }

    void AddPoint2(Vector3 point)
    {
        currentLine2.positionCount += 1;
        currentLine2.SetPosition(currentLine2.positionCount - 1, point);
    }

    void EndLine2()
    {
        isDrawing2 = false;
        currentLine2 = null;
    }

    public void ClearAllLines2()
    {
        foreach (var line in drawnLines2)
        {
            if (line != null)
                Destroy(line);
        }
        drawnLines2.Clear();
    }
}
