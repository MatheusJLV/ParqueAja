using UnityEngine;

public class PinData
{
    public GameObject pinObject;
    public Transform anchor;
    public LineRenderer lineFromPrevious;

    public PinData(GameObject pin, Transform anchor)
    {
        this.pinObject = pin;
        this.anchor = anchor;
        this.lineFromPrevious = null;
    }
}