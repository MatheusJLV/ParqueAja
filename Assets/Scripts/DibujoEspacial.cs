using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

/*
 * Espejo:
 * Se puede flippear con escalado -1 en los distintos ejes Y y Z
 */

public class DibujoEspacial : MonoBehaviour
{
    // Sistema de dibujo espacial a dos manos con espejo opcional y selección de color
    //public InputActionReference drawAction; // Vincula esto a gatillo o agarre
    public GameObject linePrefab;
    // Punto desde donde origina la línea (generalmente la punta del controlador)
    public Transform drawingTipRight;
    public GameObject linePrefab2;
    // Punto desde donde origina la línea izquierda
    public Transform drawingTipLeft;
    // Distancia mínima entre vértices para suavizar el trazo
    public float minDistance = 0.01f;

    // Renderer de la línea derecha actual
    private LineRenderer currentLine;
    // Renderer de la línea izquierda actual
    private LineRenderer currentLine2;
    // Último punto registrado de la mano derecha
    private Vector3 lastPoint;
    // Último punto registrado de la mano izquierda
    private Vector3 lastPoint2;
    // Bandera: si está activo el dibujo en mano derecha
    private bool isDrawing = false;
    // Bandera: si está activo el dibujo en mano izquierda
    private bool isDrawing2 = false;

    // Bandera: permite dibujar solo dentro del área permitida (trigger zone)
    private bool canDraw = false;

    // Lista de todas las líneas dibujadas por la mano derecha
    private List<GameObject> drawnLines = new List<GameObject>();
    // Lista de todas las líneas dibujadas por la mano izquierda
    private List<GameObject> drawnLines2 = new List<GameObject>();

    // Variables para flippear el espejo
    public bool YBool = true;
    public bool ZBool = false;
    public GameObject espejo;

    public Toggle yToggle;
    public Toggle zToggle;

    public Slider colorSlider;
    public Slider colorSlider2;

    private Color currentColor = Color.red; // Color inicial
    private Color currentColor2 = Color.blue; // Color inicial

    public Image colorSliderFill;
    public Image colorSlider2Fill;

    public Material lineMaterial;
    public Material lineMaterial2;




    void Start()
    {
        // Enlaza toggles y sliders solo si están presentes en la escena
        if (yToggle != null)
            yToggle.onValueChanged.AddListener(SetYBool);

        if (zToggle != null)
            zToggle.onValueChanged.AddListener(SetZBool);

        if (colorSlider != null)
            colorSlider.onValueChanged.AddListener(OnColorSliderChanged);
        if (colorSlider2 != null)
            colorSlider2.onValueChanged.AddListener(OnColorSliderChanged2);

        SetYBool(true);

    }

    void OnColorSliderChanged(float value)
    {
        // Mapea la posición del slider al tono HSV y actualiza material y preview UI
        currentColor = Color.HSVToRGB(value, 1f, 1f);
        if (colorSliderFill != null)
            colorSliderFill.color = currentColor;
        if (lineMaterial != null)
            lineMaterial.color = currentColor;
    }


    void OnColorSliderChanged2(float value)
    {
        // Variante para la segunda mano: color independiente
        currentColor2 = Color.HSVToRGB(value, 1f, 1f);
        if (colorSlider2Fill != null)
            colorSlider2Fill.color = currentColor2;
        if (lineMaterial2 != null)
            lineMaterial2.color = currentColor2;
    }

    void OnDestroy()
    {
        // Limpia listeners para evitar fugas al recargar escenas
        if (yToggle != null)
            yToggle.onValueChanged.RemoveListener(SetYBool);

        if (zToggle != null)
            zToggle.onValueChanged.RemoveListener(SetZBool);
    }

    public void FlipEspejo()
    {
        // Invierte ejes del espejo mediante escala local según los toggles activos
        if (espejo == null) return;

        float yScale = YBool ? -1f : 1f;
        float zScale = ZBool ? -1f : 1f;
        espejo.transform.localScale = new Vector3(1f, yScale, zScale);
    }

    public void SetYBool(bool value)
    {
        YBool = value;
        FlipEspejo();
    }

    public void SetZBool(bool value)
    {
        ZBool = value;
        FlipEspejo();
    }

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


    // Estados de botones del frame anterior (para detectar cambios de estado)
    private bool prevRightPrimary = false;
    private bool prevRightSecondary = false;
    private bool prevLeftPrimary = false;
    private bool prevLeftSecondary = false;

    void Update()
    {
        if (!canDraw) return;

        // Lee dispositivos XR de ambas manos
        var rightHandDevices = new List<InputDevice>();
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);

        // Estados de botones actuales
        bool rightPrimaryPressed = false;
        bool rightSecondaryPressed = false;
        bool leftPrimaryPressed = false;
        bool leftSecondaryPressed = false;

        // Lee el estado de los botones primario y secundario de cada mano
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

        // === ALTERNAR DIBUJO LÍNEA DERECHA (Botón Secundario Derecha) ===
        if (rightSecondaryPressed && !prevRightSecondary)
        {
            // Alterna el estado de dibujo en la mano derecha y crea/termina la línea
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

        // === ALTERNAR DIBUJO LÍNEA IZQUIERDA (Botón Secundario Izquierda) ===
        if (leftSecondaryPressed && !prevLeftSecondary)
        {
            // Alterna el estado de dibujo en la mano izquierda
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

        // === LIMPIAR AMBAS LÍNEAS (Botón Primario Izquierda) ===
        if (leftPrimaryPressed && !prevLeftPrimary)
        {
            // Limpia todos los trazos existentes de ambas manos
            ClearAllLines();
            ClearAllLines2();
        }

        // === DIBUJAR PUNTOS SI ESTÁ ACTIVO ===
        if (isDrawing)
        {
            Vector3 currentPos = drawingTipRight.position;
            // Solo agrega vértices cuando se supera la distancia mínima para suavizar la línea
            if (Vector3.Distance(currentPos, lastPoint) > minDistance)
            {
                AddPoint(currentPos);
                // Lee botones primario/secundario de cada mano en XR para controlar dibujo
                lastPoint = currentPos;
            }
        }

        if (isDrawing2)
        {
            Vector3 currentPos = drawingTipLeft.position;
            // Misma lógica de espaciamiento mínimo aplicada a la línea izquierda
            if (Vector3.Distance(currentPos, lastPoint2) > minDistance)
            {
                AddPoint2(currentPos);
                lastPoint2 = currentPos;
            }
        }

        // === ACTUALIZAR ESTADOS PREVIOS DE BOTONES ===
        prevRightPrimary = rightPrimaryPressed;
        prevRightSecondary = rightSecondaryPressed;
        prevLeftPrimary = leftPrimaryPressed;
        prevLeftSecondary = leftSecondaryPressed;
    }

    void StartLine()
    {
        // Instancia y prepara una nueva línea para la mano derecha
        GameObject lineObj = Instantiate(linePrefab);
        currentLine = lineObj.GetComponent<LineRenderer>();
        drawnLines.Add(lineObj);
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, drawingTipRight.position);
        lastPoint = drawingTipRight.position;
        isDrawing = true;

        // Reproducir el AudioSource de la punta derecha
        var audio = drawingTipRight.GetComponent<AudioSource>();
        if (audio != null) audio.Play();
    }


    void AddPoint(Vector3 point)
    {
        // Agrega un nuevo vértice a la línea derecha en la posición especificada
        currentLine.positionCount += 1;
        currentLine.SetPosition(currentLine.positionCount - 1, point);
    }

    void EndLine()
    {
        // Finaliza el trazo activo de la mano derecha
        isDrawing = false;
        currentLine = null;

        // Detener el AudioSource de la punta derecha
        var audio = drawingTipRight.GetComponent<AudioSource>();
        if (audio != null) audio.Stop();
    }

    public void ClearAllLines()
    {
        // Destruye todas las líneas creadas por la mano derecha
        foreach (var line in drawnLines)
        {
            if (line != null)
                Destroy(line);
        }
        drawnLines.Clear();
    }

    void StartLine2()
    {
        // Instancia y prepara una nueva línea para la mano izquierda
        GameObject lineObj = Instantiate(linePrefab2);
        currentLine2 = lineObj.GetComponent<LineRenderer>();
        drawnLines2.Add(lineObj);
        currentLine2.positionCount = 1;
        currentLine2.SetPosition(0, drawingTipLeft.position);
        lastPoint2 = drawingTipLeft.position;
        isDrawing2 = true;

        // Reproducir el AudioSource de la punta izquierda
        var audio = drawingTipLeft.GetComponent<AudioSource>();
        if (audio != null) audio.Play();
    }

    void AddPoint2(Vector3 point)
    {
        // Agrega un nuevo vértice a la línea izquierda en la posición especificada
        currentLine2.positionCount += 1;
        currentLine2.SetPosition(currentLine2.positionCount - 1, point);
    }

    void EndLine2()
    {
        // Finaliza el trazo activo de la mano izquierda
        isDrawing2 = false;
        currentLine2 = null;

        // Detener el AudioSource de la punta izquierda
        var audio = drawingTipLeft.GetComponent<AudioSource>();
        if (audio != null) audio.Stop();
    }

    public void ClearAllLines2()
    {
        // Destruye todas las líneas creadas por la mano izquierda
        foreach (var line in drawnLines2)
        {
            if (line != null)
                Destroy(line);
        }
        drawnLines2.Clear();
    }
}
