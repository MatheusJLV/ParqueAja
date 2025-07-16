using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using System.Collections.Generic;

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

    public float playerScaleFactor = 100f;

    // Valores originales de la cámara, almacenados para restaurar después
    private float originalFOV;
    private float originalNearClip;
    private float originalFarClip;
    private bool fovReducido = false;

    // XR locomotion references
    public UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionMediator locomotionSystem;
    public TeleportationProvider teleportationProvider;
    public ContinuousMoveProvider moveProvider; // base class works for smooth move
    public ContinuousTurnProvider turnProvider;

    public void DesactivarCharacterController()
    {
        if (characterController != null && characterController.enabled)
        {
            characterController.enabled = false;
        }
    }
    
    // New variable: List of GameObjects
    [SerializeField]
    private List<GameObject> objetos; // List of objects to activate/deactivate

    // Method to activate all objects in the list
    public void ActivarObjetos()
    {
        foreach (GameObject obj in objetos)
        {
            if (obj != null)
            {
                obj.SetActive(true); // Activate the object
            }
        }
    }

    // Method to deactivate all objects in the list
    public void DesactivarObjetos()
    {
        foreach (GameObject obj in objetos)
        {
            if (obj != null)
            {
                obj.SetActive(false); // Deactivate the object
            }
        }
    }

    public void ActivarCharacterController()
    {
        if (characterController != null && !characterController.enabled)
        {
            characterController.enabled = true;
        }
    }

    public void DesactivarLocomocion()
    {
        if (moveProvider != null)
            moveProvider.enabled = false;
        if (turnProvider != null)
            turnProvider.enabled = false;
        if (teleportationProvider != null)
            teleportationProvider.enabled = false;
    }

    public void ActivarLocomocion()
    {
        if (moveProvider != null)
            moveProvider.enabled = true;
        if (turnProvider != null)
            turnProvider.enabled = true;
        if (teleportationProvider != null)
            teleportationProvider.enabled = true;
    }


    public void ReducirFOV()
    {
        if (xrCamera == null || fovReducido)
            return;

        // Guardar valores originales
        originalFOV = xrCamera.fieldOfView;
        originalNearClip = xrCamera.nearClipPlane;
        originalFarClip = xrCamera.farClipPlane;

        // Aplicar nuevos valores para efecto miniatura
        xrCamera.fieldOfView = 45f;         // Ajusta según necesidad
        xrCamera.nearClipPlane = 0.01f;     // Para evitar que se corte la geometría cercana
        xrCamera.farClipPlane = 50f;        // Ajusta según la escala y entorno

        fovReducido = true;
    }


    public void AumentarFOV()
    {
        if (xrCamera == null || !fovReducido)
            return;

        // Restaurar valores originales
        xrCamera.fieldOfView = originalFOV;
        xrCamera.nearClipPlane = originalNearClip;
        xrCamera.farClipPlane = originalFarClip;

        fovReducido = false;
    }



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
            return;
        }
        DesactivarObjetos();
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


  

    public void Salir()
    {
        ActivarObjetos();
        if (sueloTP != null)
        {
            sueloTP.RequestTeleport();
            AumentarFOV();

        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'sueloTP' no está asignado.");
        }

        if (jugadorRig != null)
        {
            // Guardar la escala global actual antes de quitar el padre (opcional)
            // Vector3 currentWorldScale = jugadorRig.transform.lossyScale;

            jugadorRig.transform.SetParent(null);

            // Restaurar la escala global original
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale);

        }
        else
        {
            Debug.LogWarning("jugadorRig no está asignado.");
        }

        playerDentro = false;
        ingresarBtn.interactable = true;
    }



    /*public void Finalizar(GameObject objeto)
    {
        if (objeto.CompareTag("canica"))
        {
            var canicaFunel = objeto.GetComponent<CanicaFunnel>();
            if (canicaFunel != null && canicaFunel.isPlayer)
            {
                ActivarCharacterController();
                ActivarLocomocion();
                
                Salir();
            }
            Destroy(objeto, 1f);
        }

        if (objeto.CompareTag("MainCamera"))
        {
            ActivarCharacterController();
            ActivarLocomocion();
            
            Salir();
        }
    }*/

    public void Finalizar(GameObject objeto)
    {
        if (objeto.CompareTag("canica"))
        {
            var canicaFunel = objeto.GetComponent<CanicaFunnel>();
            if (canicaFunel != null && canicaFunel.isPlayer)
            {
                StartCoroutine(SalirConRecuperacion());
            }
            Destroy(objeto, 1f);
        }

        if (objeto.CompareTag("MainCamera"))
        {
            StartCoroutine(SalirConRecuperacion());
        }
    }

    private IEnumerator SalirConRecuperacion()
    {
        Salir();

        // Wait for teleport to fully resolve and player to be placed
        yield return new WaitForSeconds(0.2f);
        yield return new WaitForEndOfFrame(); // safer in case of XR update delay

        ActivarCharacterController();
        ActivarLocomocion();
        //ActivarObjetos();
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

    }

    public void IngresarEIniciar()
    {
        ReducirFOV();
        DesactivarLocomocion();
        DesactivarCharacterController();
        //DesactivarObjetos();
        StartCoroutine(IngresarEIniciarCoroutine());
    }

    public IEnumerator IngresarEIniciarCoroutine()
    {
        if (ejecutandoIngresarEIniciar || playerDentro || !puedeIniciar)
        {
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
            }
            else
            {
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

        }
        else
        {
        }

        // Small pause to allow prefab instantiation to finalize
        yield return new WaitForSeconds(0.1f); // Or WaitForEndOfFrame

        // STEP 4: Teleport
        if (asientoTP != null)
        {
            asientoTP.RequestTeleport();
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


            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale / playerScaleFactor);

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
}
