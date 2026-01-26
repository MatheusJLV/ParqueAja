using UnityEngine;

public class SimplePlatformMove : MonoBehaviour
{
    public float amplitude = 1f;   // meters
    public float speed = 1f;       // cycles per second

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        float x = Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * amplitude;
        transform.position = startPos + new Vector3(x, 0f, 0f);
    }
}
