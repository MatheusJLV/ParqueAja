using UnityEngine;

// Estructura de datos que representa un pin: almacena el objeto, su ancla y la línea visual al pin anterior.
public class PinData
{
    public GameObject pinObject;          // Objeto del pin en escena
    public Transform anchor;              // Ancla de posición del pin
    public LineRenderer lineFromPrevious; // Línea visual que conecta con el pin previo

    // Constructor: inicializa el pin con su objeto y ancla, sin línea inicial.
    public PinData(GameObject pin, Transform anchor)
    {
        this.pinObject = pin;
        this.anchor = anchor;
        this.lineFromPrevious = null;
    }
}