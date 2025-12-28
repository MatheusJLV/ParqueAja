using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

/*
 * DibujoEspacial:
 * Permite dibujar líneas en el espacio 3D usando controles VR,
 * con soporte para dos manos, colores personalizables y flip de espejo.
 */

public class DibujoEspacial : MonoBehaviour
{
    //public InputActionReference drawAction; // Bind this to trigger or grip
    public GameObject linePrefab; // Prefab para las líneas dibujadas con la mano derecha
    public Transform drawingTipRight; // Punto de origen de la línea para la mano derecha (generalmente la punta del controlador)
    public GameObject linePrefab2; // Prefab para las líneas dibujadas con la mano izquierda
    public Transform drawingTipLeft; // Punto de origen de la línea para la mano izquierda
    public float minDistance = 0.01f; // Distancia mínima entre puntos para agregar un nuevo punto a la línea

    private LineRenderer currentLine; // LineRenderer actual para la mano derecha
    private LineRenderer currentLine2; // LineRenderer actual para la mano izquierda
    private Vector3 lastPoint; // Último punto agregado a la línea derecha
    private Vector3 lastPoint2; // Último punto agregado a la línea izquierda
    private bool isDrawing = false; // Indica si se está dibujando con la mano derecha
    private bool isDrawing2 = false; // Indica si se está dibujando con la mano izquierda

    private bool canDraw = false; // Indica si se puede dibujar (basado en colisión con zona de dibujo)

    private List<GameObject> drawnLines = new List<GameObject>(); // Lista de líneas dibujadas con la mano derecha
    private List<GameObject> drawnLines2 = new List<GameObject>(); // Lista de líneas dibujadas con la mano izquierda

    // Variables para flippear el espejo
    public bool YBool = true; // Estado del flip en el eje Y
    public bool ZBool = false; // Estado del flip en el eje Z
    public GameObject espejo; // Objeto del espejo a flippear

    public Toggle yToggle; // Toggle para activar/desactivar flip en Y
    public Toggle zToggle; // Toggle para activar/desactivar flip en Z

    public Slider colorSlider; // Slider para cambiar el color de la línea derecha
    public Slider colorSlider2; // Slider para cambiar el color de la línea izquierda

    private Color currentColor = Color.red; // Color actual para la línea derecha
    private Color currentColor2 = Color.blue; // Color actual para la línea izquierda

    public Image colorSliderFill; // Imagen de relleno del slider de color derecho
    public Image colorSlider2Fill; // Imagen de relleno del slider de color izquierdo

    public Material lineMaterial; // Material para las líneas de la mano derecha
    public Material lineMaterial2; // Material para las líneas de la mano izquierda




    void Start()
    {
        // Configurar listeners para los toggles y sliders al iniciar
        if (yToggle != null)
            yToggle.onValueChanged.AddListener(SetYBool);

        if (zToggle != null)
            zToggle.onValueChanged.AddListener(SetZBool);

        if (colorSlider != null)
            colorSlider.onValueChanged.AddListener(OnColorSliderChanged);
        if (colorSlider2 != null)
            colorSlider2.onValueChanged.AddListener(OnColorSliderChanged2);

        SetYBool(true); // Inicializar flip en Y como true

    }

    void OnColorSliderChanged(float value)
    {
        // Cambiar el color actual basado en el valor del slider (HSV)
        currentColor = Color.HSVToRGB(value, 1f, 1f);
        if (colorSliderFill != null)
            colorSliderFill.color = currentColor;
        if (lineMaterial != null)
            lineMaterial.color = currentColor;
    }


    void OnColorSliderChanged2(float value)
    {
        // Cambiar el color actual para la mano izquierda basado en el valor del slider (HSV)
        currentColor2 = Color.HSVToRGB(value, 1f, 1f);
        if (colorSlider2Fill != null)
            colorSlider2Fill.color = currentColor2;
        if (lineMaterial2 != null)
            lineMaterial2.color = currentColor2;
    }

    void OnDestroy()
    {
        // Limpiar listeners al destruir el objeto para evitar memory leaks
        if (yToggle != null)
            yToggle.onValueChanged.RemoveListener(SetYBool);

        if (zToggle != null)
            zToggle.onValueChanged.RemoveListener(SetZBool);
    }

    public void FlipEspejo()
    {
        // Aplicar flip al espejo basado en los valores de YBool y ZBool
        if (espejo == null) return;

        float yScale = YBool ? -1f : 1f;
        float zScale = ZBool ? -1f : 1f;
        espejo.transform.localScale = new Vector3(1f, yScale, zScale);
    }

    public void SetYBool(bool value)
    {
        // Establecer el estado del flip en Y y aplicar el cambio
        YBool = value;
        FlipEspejo();
    }

    public void SetZBool(bool value)
    {
        // Establecer el estado del flip en Z y aplicar el cambio
        ZBool = value;
        FlipEspejo();
    }

    void OnTriggerEnter(Collider other)
    {
        // Permitir dibujar cuando el jugador entra en la zona de dibujo
        if (other.CompareTag("Player")) // Asegúrate de usar el tag correcto
            canDraw = true;
    }

    void OnTriggerExit(Collider other)
    {
        // Deshabilitar dibujar cuando el jugador sale de la zona de dibujo
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
    private bool prevRightPrimary = false; // Estado del botón primario derecho en el frame anterior
    private bool prevRightSecondary = false; // Estado del botón secundario derecho en el frame anterior
    private bool prevLeftPrimary = false; // Estado del botón primario izquierdo en el frame anterior
    private bool prevLeftSecondary = false; // Estado del botón secundario izquierdo en el frame anterior

    void Update()
    {
        // Solo procesar si se puede dibujar
        if (!canDraw) return;

        // Obtener dispositivos de entrada para ambas manos
        var rightHandDevices = new List<InputDevice>();
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);

        // Variables para los estados de los botones
        bool rightPrimaryPressed = false;
        bool rightSecondaryPressed = false;
        bool leftPrimaryPressed = false;
        bool leftSecondaryPressed = false;

        // Leer el estado de los botones de la mano derecha
        foreach (var device in rightHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondaryPressed);
        }

        // Leer el estado de los botones de la mano izquierda
        foreach (var device in leftHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out leftPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out leftSecondaryPressed);
        }

        // === TOGGLE DRAWING FOR RIGHT LINE (Secondary Right) ===
        if (rightSecondaryPressed && !prevRightSecondary)
        {
            // Alternar el estado de dibujo para la línea derecha
            isDrawing = !isDrawing;
            if (isDrawing)
            {
                StartLine(); // Iniciar nueva línea
                lastPoint = drawingTipRight.position;
            }
            else
            {
                EndLine(); // Finalizar línea actual
            }
        }

        // === TOGGLE DRAWING FOR LEFT LINE (Secondary Left) ===
        if (leftSecondaryPressed && !prevLeftSecondary)
        {
            // Alternar el estado de dibujo para la línea izquierda
            isDrawing2 = !isDrawing2;
            if (isDrawing2)
            {
                StartLine2(); // Iniciar nueva línea izquierda
                lastPoint2 = drawingTipLeft.position;
            }
            else
            {
                EndLine2(); // Finalizar línea izquierda
            }
        }

        // === CLEAR BOTH LINES (Primary Left) ===
        if (leftPrimaryPressed && !prevLeftPrimary)
        {
            // Limpiar todas las líneas de ambas manos
            ClearAllLines();
            ClearAllLines2();
        }

        // === DRAW POINTS IF ACTIVE ===
        if (isDrawing)
        {
            // Agregar puntos a la línea derecha si se está moviendo lo suficiente
            Vector3 currentPos = drawingTipRight.position;
            if (Vector3.Distance(currentPos, lastPoint) > minDistance)
            {
                AddPoint(currentPos);
                lastPoint = currentPos;
            }
        }

        if (isDrawing2)
        {
            // Agregar puntos a la línea izquierda si se está moviendo lo suficiente
            Vector3 currentPos = drawingTipLeft.position;
            if (Vector3.Distance(currentPos, lastPoint2) > minDistance)
            {
                AddPoint2(currentPos);
                lastPoint2 = currentPos;
            }
        }

        // === UPDATE PREVIOUS BUTTON STATES ===
        // Actualizar los estados previos para detectar cambios en el siguiente frame
        prevRightPrimary = rightPrimaryPressed;
        prevRightSecondary = rightSecondaryPressed;
        prevLeftPrimary = leftPrimaryPressed;
        prevLeftSecondary = leftSecondaryPressed;
    }

    void StartLine()
    {
        // Crear e inicializar una nueva línea para la mano derecha
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
        // Agregar un nuevo punto a la línea derecha actual
        currentLine.positionCount += 1;
        currentLine.SetPosition(currentLine.positionCount - 1, point);
    }

    void EndLine()
    {
        // Finalizar el dibujo de la línea derecha
        isDrawing = false;
        currentLine = null;

        // Detener el AudioSource de la punta derecha
        var audio = drawingTipRight.GetComponent<AudioSource>();
        if (audio != null) audio.Stop();
    }

    public void ClearAllLines()
    {
        // Destruir y limpiar todas las líneas dibujadas con la mano derecha
        foreach (var line in drawnLines)
        {
            if (line != null)
                Destroy(line);
        }
        drawnLines.Clear();
    }

    void StartLine2()
    {
        // Crear e inicializar una nueva línea para la mano izquierda
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
        // Agregar un nuevo punto a la línea izquierda actual
        currentLine2.positionCount += 1;
        currentLine2.SetPosition(currentLine2.positionCount - 1, point);
    }

    void EndLine2()
    {
        // Finalizar el dibujo de la línea izquierda
        isDrawing2 = false;
        currentLine2 = null;

        // Detener el AudioSource de la punta izquierda
        var audio = drawingTipLeft.GetComponent<AudioSource>();
        if (audio != null) audio.Stop();
    }

    public void ClearAllLines2()
    {
        // Destruir y limpiar todas las líneas dibujadas con la mano izquierda
        foreach (var line in drawnLines2)
        {
            if (line != null)
                Destroy(line);
        }
        drawnLines2.Clear();
    }
}
