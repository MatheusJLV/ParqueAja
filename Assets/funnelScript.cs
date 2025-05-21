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
    public GameObject pelotaPlayerPrefab;     // Prefab de la pelota
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
    private Vector3 jugadorRigOriginalScale;
    private bool ejecutandoIngresarEIniciar = false;

    private Vector3 jugadorRigOriginalWorldScale;


    void Start()
    {
        if (iniciarBtn != null)
            iniciarBtn.onClick.AddListener(Iniciar);
        if (ingresarBtn != null)
            ingresarBtn.onClick.AddListener(IngresarEIniciar);
        if (salirBtn != null)
            salirBtn.onClick.AddListener(Salir);

        //GuardarEstado();
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

    // Obtener la escala global (world scale)
    Vector3 GetWorldScale(Transform t)
    {
        return Vector3.Scale(t.localScale, t.parent ? t.parent.lossyScale : Vector3.one);
    }

    // Establecer la escala local para una escala global deseada
    void SetWorldScale(Transform t, Vector3 worldScale)
    {
        if (t.parent)
        {
            Vector3 parentScale = t.parent.lossyScale;
            t.localScale = new Vector3(
                worldScale.x / parentScale.x,
                worldScale.y / parentScale.y,
                worldScale.z / parentScale.z
            );
        }
        else
        {
            t.localScale = worldScale;
        }
    }


    public void Ingresar()
    {
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

        /*if (asientoGO != null && jugadorRig != null)
        {
            jugadorRig.transform.SetParent(asientoGO.transform);
            // Guardar la escala original y reducirla por 100
            jugadorRigOriginalScale = jugadorRig.transform.localScale;
            jugadorRig.transform.localScale = jugadorRigOriginalScale / 100f;
            Debug.Log("Jugador ahora es hijo del asiento y su escala ha sido reducida por 100.");
        }*/
        if (asientoGO != null && jugadorRig != null)
        {
            // Guardar la escala global antes de cambiar el padre
            jugadorRigOriginalWorldScale = jugadorRig.transform.lossyScale;

            jugadorRig.transform.SetParent(asientoGO.transform);

            // Ajustar la escala local para que la global sea 1/100 de la original
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale / 100f);

            Debug.Log("Jugador ahora es hijo del asiento y su escala global ha sido reducida por 100.");
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

        /*if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            // Restaurar la escala original
            jugadorRig.transform.localScale = jugadorRigOriginalScale;
            Debug.Log("Jugador liberado del asiento y su escala restaurada.");
        }*/
        if (jugadorRig != null)
        {
            // Guardar la escala global actual antes de quitar el padre (opcional)
            // Vector3 currentWorldScale = jugadorRig.transform.lossyScale;

            jugadorRig.transform.SetParent(null);

            // Restaurar la escala global original
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale);

            Debug.Log("Jugador liberado del asiento y su escala global restaurada.");
        }
        else
        {
            Debug.LogWarning("jugadorRig no está asignado.");
        }

        playerDentro = false;
        ingresarBtn.interactable = true;
    }



    public void Finalizar(GameObject objeto)
    {
        if (objeto.CompareTag("canica"))
        {
            var canicaFunel = objeto.GetComponent<CanicaFunnel>();
            if (canicaFunel != null && canicaFunel.isPlayer)
            {
                Salir();
            }
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
            xrCamera.farClipPlane = 100f;
        }

        Debug.Log("Estado modificado en los objetos: YOffset, Height y NearClip a 0.1; FarClip a 1.");
    }

    public void IngresarEIniciar()
    {
        StartCoroutine(IngresarEIniciarCoroutine());
    }

    public IEnumerator IngresarEIniciarCoroutine()
    {
        if (ejecutandoIngresarEIniciar || playerDentro || !puedeIniciar)
        {
            Debug.LogWarning("IngresarEIniciarCoroutine already running or blocked.");
            yield break;
        }

        ejecutandoIngresarEIniciar = true;
        puedeIniciar = false;

        if (iniciarBtn != null)
            iniciarBtn.interactable = false;

        // STEP 1: Initial cooldown
        if (cooldownInicio > 0f)
            yield return new WaitForSeconds(cooldownInicio);

        Rigidbody rb = null;
        Collider col = null;

        // STEP 2: Instantiate player-ball
        if (pelotaPlayerPrefab != null && pelotas2 != null)
        {
            GameObject nuevaPelota2 = Instantiate(
                pelotaPlayerPrefab,
                pelotas2.transform.position,
                pelotas2.transform.rotation,
                pelotas2.transform
            );

            // Disable Rigidbody and Collider temporarily
            rb = nuevaPelota2.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // or rb.simulated = false (if using 2D)
            col = nuevaPelota2.GetComponent<Collider>();
            if (col != null) col.enabled = false;


            pelotaActual2 = nuevaPelota2;
            asientoGO = nuevaPelota2;

            // STEP 3: Fetch TeleportAnchor
            TeleportationAnchor anchor = nuevaPelota2.GetComponentInChildren<TeleportationAnchor>();
            if (anchor != null)
            {
                asientoTP = anchor;
                Debug.Log("TeleportationAnchor assigned.");
            }
            else
            {
                Debug.LogWarning("TeleportationAnchor not found.");
            }

            // Optional: Set color
            Renderer rend2 = nuevaPelota2.GetComponent<Renderer>();
            if (rend2 != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                rend2.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(Random.value, Random.value, Random.value, 0.5f));
                rend2.SetPropertyBlock(block);
            }

            Debug.Log("Player-ball instantiated.");
        }
        else
        {
            Debug.LogWarning("pelotaPlayerPrefab or pelotas2 missing.");
        }

        // Small pause to allow prefab instantiation to finalize
        yield return new WaitForSeconds(0.1f); // Or WaitForEndOfFrame

        // STEP 4: Teleport
        if (asientoTP != null)
        {
            asientoTP.RequestTeleport();
            Debug.Log("Teleport requested.");
        }
        else
        {
            Debug.LogWarning("TeleportationAnchor is null.");
        }

        // Wait at least 1 frame (or more) to allow teleport to apply
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForEndOfFrame(); // Optional: more reliable than seconds

        // STEP 5: Reparent & scale player
        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRigOriginalWorldScale = jugadorRig.transform.lossyScale;
            jugadorRig.transform.SetParent(asientoGO.transform);


            jugadorRig.transform.localPosition = Vector3.zero;
            jugadorRig.transform.localRotation = Quaternion.identity;


            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale / 100f);

            Debug.Log("Player reparented and scaled.");
        }
        else
        {
            Debug.LogWarning("Missing asientoGO or jugadorRig.");
        }

        // STEP 6: Final cooldown
        yield return new WaitForSeconds(cooldownFinal);

        playerDentro = true;
        if (ingresarBtn != null)
            ingresarBtn.interactable = false;
        if (iniciarBtn != null)
            iniciarBtn.interactable = true;

        puedeIniciar = true;
        ejecutandoIngresarEIniciar = false;

        // Re-enable physics AFTER setup
        yield return new WaitForSeconds(0.3f); // let hierarchy settle
        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;

    }


    /*public IEnumerator IngresarEIniciarCoroutine()
    {
        Rigidbody rb = null;
        Collider col = null;
        // Validación para evitar duplicados o ejecuciones simultáneas
        if (ejecutandoIngresarEIniciar || playerDentro || !puedeIniciar)
        {
            Debug.LogWarning("IngresarEIniciarCoroutine ya está en ejecución, el jugador ya está dentro o está en cooldown.");
            yield break;
        }

        ejecutandoIngresarEIniciar = true;
        puedeIniciar = false;

        if (iniciarBtn != null)
            iniciarBtn.interactable = false;

        // --- Cooldown al inicio ---
        if (cooldownInicio > 0f)
            yield return new WaitForSeconds(cooldownInicio);

        // --- Instanciar pelotaPlayerPrefab en pelotas2 ---
        if (pelotaPlayerPrefab != null && pelotas2 != null)
        {
            GameObject nuevaPelota2 = Instantiate(
                pelotaPlayerPrefab,
                pelotas2.transform.position,
                pelotas2.transform.rotation,
                pelotas2.transform
            );
            pelotaActual2 = nuevaPelota2;
            asientoGO = nuevaPelota2;

            yield return new WaitForSeconds(0.1f);

            // Buscar el componente TeleportationAnchor en los hijos
            TeleportationAnchor anchor = nuevaPelota2.GetComponentInChildren<TeleportationAnchor>();
            if (anchor != null)
            {
                asientoTP = anchor;
                Debug.Log("TeleportationAnchor encontrado y asignado a asientoTP.");
            }
            else
            {
                Debug.LogWarning("No se encontró TeleportationAnchor en los hijos de la pelota instanciada.");
            }

            yield return new WaitForSeconds(0.1f);

            // Opcional: cambiar color/transparencia si lo deseas
            Renderer rend2 = pelotaActual2.GetComponent<Renderer>();
            if (rend2 != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                rend2.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(Random.value, Random.value, Random.value, 0.5f));
                rend2.SetPropertyBlock(block);
            }

            Debug.Log("Pelota 2 instanciada como hija de 'pelotas2'.");
        }
        else
        {
            Debug.LogWarning("pelotaPlayerPrefab o pelotas2 no están asignados.");
        }

        // --- Teletransportar al jugador usando el nuevo asientoTP ---
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
            // Guardar la escala global antes de cambiar el padre
            jugadorRigOriginalWorldScale = jugadorRig.transform.lossyScale;

            jugadorRig.transform.SetParent(asientoGO.transform);

            // Ajustar la escala local para que la global sea 1/100 de la original
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale / 200f);

            Debug.Log("Jugador ahora es hijo del asiento y su escala global ha sido reducida por 100.");
        }
        else
        {
            Debug.LogWarning("asientoGO o jugadorRig no están asignados.");
        }

        playerDentro = true;
        if (ingresarBtn != null)
            ingresarBtn.interactable = false;

        // --- Cooldown al final ---
        if (cooldownFinal > 0f)
            yield return new WaitForSeconds(cooldownFinal);

        puedeIniciar = true;
        if (iniciarBtn != null)
            iniciarBtn.interactable = true;
        ejecutandoIngresarEIniciar = false;
    }*/
}
