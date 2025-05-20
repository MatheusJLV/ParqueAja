using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.UI;
using System.Collections;

public class funnelScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject asientoGO; // Referencia al asiento (este objeto)
    public GameObject jugadorRig; // Referencia al jugador (XR Rig)
    public TeleportationAnchor asientoTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP;   // TeleportationAnchor para el suelo
    public GameObject pelotaPrefab;     // Prefab de la pelota
    public GameObject pelotas1; // Contenedor de pelotas 1
    public GameObject pelotas2; // Contenedor de pelotas 2
    public GameObject pelotaActual1; // Referencia a la pelota actual del contenedor 1
    public GameObject pelotaActual2; // Referencia a la pelota actual del contenedor 2

    // Botones para ingresar y salir
    public Button iniciarBtn;
    public Button ingresarBtn;
    public Button salirBtn;

    private bool puedeIniciar = true;
    public float cooldownInicio = 0.5f; // segundos de cooldown antes de instanciar
    public float cooldownFinal = 0.5f;  // segundos de cooldown después de instanciar

    public Collider metaZone; // Zona de meta
    public bool inmersivo = false;
    public bool playerDentro = false;

    // Referencias a los objetos a guardar/cargar
    public Transform xrOrigin; // El XR Origin (XR Rig)
    public CharacterController characterController; // CharacterController asociado
    public Camera xrCamera; // Cámara principal

    // Variables para almacenar el estado
    private float storedCameraYOffset;
    private float storedCharacterHeight;
    private float storedCameraNearClip;
    private float storedCameraFarClip;

    void Start()
    {
        if (iniciarBtn != null)
            iniciarBtn.onClick.AddListener(Iniciar);
        if (ingresarBtn != null)
            ingresarBtn.onClick.AddListener(Ingresar);
        if (salirBtn != null)
            salirBtn.onClick.AddListener(Salir);

        GuardarEstado();
    }

    public void Iniciar()
    {
        if (!puedeIniciar)
        {
            Debug.LogWarning("Iniciar está en cooldown o ya se está ejecutando.");
            return;
        }
        StartCoroutine(IniciarCoroutine());
    }

    private IEnumerator IniciarCoroutine()
    {
        puedeIniciar = false;
        if (iniciarBtn != null)
            iniciarBtn.interactable = false;

        // Cooldown al inicio
        if (cooldownInicio > 0f)
            yield return new WaitForSeconds(cooldownInicio);

        // Instanciar pelota para pelotas1
        if (pelotaPrefab != null && pelotas1 != null)
        {
            GameObject nuevaPelota1 = Instantiate(
                pelotaPrefab,
                pelotas1.transform.position,
                pelotas1.transform.rotation,
                pelotas1.transform
            );
            pelotaActual1 = nuevaPelota1;
            Renderer rend = pelotaActual1.GetComponent<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);

            // Apply a tint color (with transparency!)
            block.SetColor("_BaseColor", new Color(Random.value, Random.value, Random.value, 0.5f));
            rend.SetPropertyBlock(block);
            Debug.Log("Pelota 1 instanciada como hija de 'pelotas1'.");
        }
        else
        {
            Debug.LogWarning("pelotaPrefab o pelotas1 no están asignados.");
        }

        // Instanciar pelota para pelotas2
        if (pelotaPrefab != null && pelotas2 != null)
        {
            GameObject nuevaPelota2 = Instantiate(
                pelotaPrefab,
                pelotas2.transform.position,
                pelotas2.transform.rotation,
                pelotas2.transform
            );
            pelotaActual2 = nuevaPelota2;
            Renderer rend2 = pelotaActual2.GetComponent<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rend2.GetPropertyBlock(block);

            // Apply a tint color (with transparency!)
            block.SetColor("_BaseColor", new Color(Random.value, Random.value, Random.value, 0.5f));
            rend2.SetPropertyBlock(block);
            Debug.Log("Pelota 2 instanciada como hija de 'pelotas2'.");
        }
        else
        {
            Debug.LogWarning("pelotaPrefab o pelotas2 no están asignados.");
        }

        // Cooldown al final
        if (cooldownFinal > 0f)
            yield return new WaitForSeconds(cooldownFinal);

        puedeIniciar = true;
        if (iniciarBtn != null)
            iniciarBtn.interactable = true;
    }

    /*public Transform xrOrigin;          // Reference to XR Origin (XR Rig)
    public Transform ballTarget;        // Transform inside the ball

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isInside = false;*/
    public void Ingresar()
    {
        /*if (!isInside)
        {
            // Save current position
            originalPosition = xrOrigin.position;
            originalRotation = xrOrigin.rotation;

            // Get camera offset so camera aligns to target
            Camera xrCamera = xrOrigin.GetComponentInChildren<Camera>();
            Vector3 offset = xrOrigin.position - xrCamera.transform.position;

            // Teleport inside
            xrOrigin.position = ballTarget.position + offset;
            isInside = true;
        }
        else
        {
            // Return to saved position
            xrOrigin.position = originalPosition;
            xrOrigin.rotation = originalRotation;
            isInside = false;
        }*/
        if (playerDentro)
        {
            return;
        }
        if (asientoTP != null)
        {
            asientoTP.RequestTeleport();
            Debug.Log("Teletransportado al asiento.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'asientoTP' no está asignado.");
        }

        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRig.transform.SetParent(asientoGO.transform);
            Debug.Log("Jugador ahora es hijo del asiento.");
        }
        else
        {
            Debug.LogWarning("asientoGO o jugadorRig no están asignados.");
        }
        playerDentro = true;
        ingresarBtn.interactable = false;
    }

    public void Salir()
    {
        if (sueloTP != null)
        {
            sueloTP.RequestTeleport();
            Debug.Log("Teletransportado al suelo.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'sueloTP' no está asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            Debug.Log("Jugador liberado del asiento.");
        }
        else
        {
            Debug.LogWarning("jugadorRig no está asignado.");
        }
        playerDentro = false;
        ingresarBtn.interactable = true;
    }

    /*private void OnTriggerEnter(Collider other)
    {
        Finalizar(other.gameObject);
    }*/

    public void Finalizar(GameObject objeto)
    {
        if (objeto.CompareTag("canica"))
        {
            Destroy(objeto, 1f);
            Debug.Log("Canica detectada en metaZone. Se destruirá en 1 segundo.");
        }

        if (objeto.CompareTag("MainCamera"))
        {
            Debug.Log("MainCamera detectada en metaZone. Ejecutando Salir().");
            Salir();
        }
    }

    public void GuardarEstado()
    {
        if (xrOrigin != null && xrCamera != null)
        {
            // Y offset de la cámara respecto al XR Origin
            storedCameraYOffset = xrCamera.transform.localPosition.y;
        }
        if (characterController != null)
        {
            storedCharacterHeight = characterController.height;
        }
        if (xrCamera != null)
        {
            storedCameraNearClip = xrCamera.nearClipPlane;
            storedCameraFarClip = xrCamera.farClipPlane;
        }
        Debug.Log("Estado guardado.");
    }

    public void CargarEstado()
    {
        if (xrOrigin != null && xrCamera != null)
        {
            Vector3 camLocalPos = xrCamera.transform.localPosition;
            camLocalPos.y = storedCameraYOffset;
            xrCamera.transform.localPosition = camLocalPos;
        }
        if (characterController != null)
        {
            characterController.height = storedCharacterHeight;
        }
        if (xrCamera != null)
        {
            xrCamera.nearClipPlane = storedCameraNearClip;
            xrCamera.farClipPlane = storedCameraFarClip;
        }
        Debug.Log("Estado cargado.");
    }

    public void ModificarEstado()
    {
        // Modificar el Y offset de la cámara respecto al XR Origin
        if (xrOrigin != null && xrCamera != null)
        {
            Vector3 camLocalPos = xrCamera.transform.localPosition;
            camLocalPos.y = 0.1f;
            xrCamera.transform.localPosition = camLocalPos;
        }

        // Modificar la altura del CharacterController
        if (characterController != null)
        {
            characterController.height = 0.1f;
        }

        // Modificar los clipping planes de la cámara
        if (xrCamera != null)
        {
            xrCamera.nearClipPlane = 0.1f;
            xrCamera.farClipPlane = 1f;
        }

        Debug.Log("Estado modificado en los objetos: YOffset, Height y NearClip a 0.1; FarClip a 1.");
    }

}
