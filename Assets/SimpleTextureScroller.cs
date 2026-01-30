using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SimpleTextureScroller : MonoBehaviour
{
    [Header("Texture Property (URP/Lit)")]
    public string textureProperty = "_BaseMap"; // URP Lit base map

    [Header("Scroll")]
    public float speed = 0.25f; // UV units per second

    [Header("Axis")]
    public bool moveU = false;  // X axis (U)
    public bool moveV = true;   // Y axis (V)

    [Header("Direction")]
    public bool reverseU = false;
    public bool reverseV = false;

    [Header("Material Target")]
    public bool useSharedMaterial = false; // false = only this renderer instance

    private Renderer rend;
    private Material mat;
    private Vector2 offset;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        mat = useSharedMaterial ? rend.sharedMaterial : rend.material;

        if (mat == null)
        {
            Debug.LogWarning("[SimpleTextureScroller] No material found.");
            enabled = false;
            return;
        }

        if (!mat.HasProperty(textureProperty))
        {
            Debug.LogWarning($"[SimpleTextureScroller] Material '{mat.name}' does not have property '{textureProperty}'. Shader: {mat.shader.name}");
            enabled = false;
            return;
        }

        offset = mat.GetTextureOffset(textureProperty);
    }

    private void Update()
    {
        float du = (moveU ? speed : 0f) * (reverseU ? -1f : 1f) * Time.deltaTime;
        float dv = (moveV ? speed : 0f) * (reverseV ? -1f : 1f) * Time.deltaTime;

        offset.x += du;
        offset.y += dv;

        mat.SetTextureOffset(textureProperty, offset);
    }
}
