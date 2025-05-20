using UnityEngine;

//El codigo y sus iteraciones estan muy taxing, recurrir a fisicas de unity.

public class CanicaFunnel : MonoBehaviour
{
    public Transform funnelCenter;
    public float radialBoost = 0.8f;        // Try 0.2 to 1.0
    public float maxEffectRadius = 3.0f;    // Beyond this, no boost
    public float maxHeight = 1.0f;          // Only boost if below this Y level

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 toCenter = funnelCenter.position - transform.position;
        float distance = toCenter.magnitude;

        // Only apply if close enough and low enough
        if (distance < maxEffectRadius && transform.position.y < maxHeight)
        {
            Vector3 outward = -toCenter.normalized;
            rb.AddForce(outward * radialBoost, ForceMode.Acceleration);
        }
    }


}