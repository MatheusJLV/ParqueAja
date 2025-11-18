using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using System.Collections;
using System.Collections.Generic;

/* Controla la experiencia inmersiva basada en un "dodecaedro" y una pelota que recorre pins.
 - Instancia una pelota para que el jugador "entre" en ella (followBall).
 - Ajusta cámara, locomoción y colliders durante la experiencia.
 - Recorre un camino definido por pins y restablece estado al salir.*/
public class HammiltonInmersivo : MonoBehaviour
{
    [Header("References")]
    public GameObject jugadorRig; // Rigidbody/transform principal del jugador (XR Rig)
    public Transform xrOrigin; // Origen XR (usado si hace falta para transformaciones)
    public CharacterController characterController; // CharacterController del jugador (se desactiva/activa)
    public Camera xrCamera; // Cámara usada para ajustar FOV y planos cercanos/lejanos
    public FiltroAudio filtro; // Componente opcional para aplicar efectos de audio (muffled)
    public DodecaedroScript dodecaedroScript; // Script que contiene los pins colocados (estructura de datos)

    [Header("Locomotion")]
    public TeleportationProvider teleportationProvider; // Provider de teletransporte (se desactiva en modo inmersivo)
    public ContinuousMoveProvider moveProvider;  // Movimiento continuo (se desactiva/activa)
    public ContinuousTurnProvider turnProvider;  // Giro continuo (referencia por si se necesita)

    [Header("Prefabs & Positions")]
    public GameObject pelotaPlayerPrefab;  // Prefab de la "pelota" que contiene al jugador
    public Transform spawnPoint = null; // Punto donde se instancia la pelota (seteado desde pins)

    [Header("Teleport Targets")]
    public TeleportationAnchor sueloTP; // Anchor para teletransportar al jugador al salir (target de seguridad)

    [Header("World Immersion (Dodecahedron-Based)")]
    public Transform dodecaedro; // Transform del dodecaedro que se escala y desplaza para inmersión
    public float dodecaedroScaleFactor = 250f; // Factor de escala aplicado durante inmersión
    public Vector3 skyOffset = new Vector3(0f, 200f, 0f); // estaba en 100/ Offset aplicado al dodecaedro para simular cielo

    [Header("Traversal Settings")]
    public float traversalSpeed = 5f; // Velocidad a la que se mueve la pelota durante el recorrido

    [SerializeField]  
    private List<GameObject> objetos;  // Lista de objetos del escenario que se activan/desactivan
    
    // Instancia actual de la pelota y referencia a su "asiento"
    private GameObject currentBallInstance;
    private GameObject asientoGO;
    
    // Estado y flags
    private Vector3 jugadorRigOriginalWorldScale;
    private bool playerDentro = false;  // Indica si el jugador está dentro de la experiencia
    private bool ejecutandoIngresar = false; // Evita reentradas en la rutina de ingreso
    private bool followBall = false; // Si true, la rig sigue la posición de la pelota

    // Parámetros de cámara para restauración
    private float originalFOV;
    private float originalNearClip;
    private float originalFarClip;
    private bool fovReducido = false; // Flag para saber si se redujo el FOV

    // Variables auxiliares para spawn y offsets
    private Vector3 anchorSpawn;
    private Vector3 pinSpawn;
    private Vector3 rawOffset;

    // Guardado de transform originales del dodecaedro
    private Vector3 originalDodecaedroPosition;
    private Quaternion originalDodecaedroRotation;
    private Vector3 originalDodecaedroScale;

    [Header("Offset Settings")]
    public float offsetDistance = 1.5f; // Puedes ajustar este valor desde el Inspector


    // FixedUpdate: sincroniza la posición del jugador con la pelota si corresponde.
    void FixedUpdate()
    {
        if (followBall && currentBallInstance != null && jugadorRig != null)
        {
            jugadorRig.transform.position = currentBallInstance.transform.position;
        }
    }

    // IngresarNoParent: prepara la escena y crea la pelota para entrar en modo inmersivo.
    public void IngresarNoParent()
    {
        // Validaciones básicas: asegurar que existan pins colocados en el dodecaedro
        if (dodecaedroScript.placedPins.First == null)
        {
            Debug.LogError("No pins placed. Cannot enter immersive mode.");
            return;
        }

        var firstPin = dodecaedroScript.placedPins.First.Value;
        spawnPoint = firstPin.anchor;

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point missing on first pin.");
            return;
        }
        // Calcular offsets basados en la posición del pin y su anchor
        anchorSpawn = firstPin.anchor.position;
        pinSpawn = firstPin.pinObject.transform.position;
        rawOffset = (pinSpawn - anchorSpawn) * 2.5f;
        // Guardar estado original del dodecaedro para restaurarlo luego
        originalDodecaedroPosition = dodecaedro.position;
        originalDodecaedroRotation = dodecaedro.rotation;
        originalDodecaedroScale = dodecaedro.localScale;
        // Mover y escalar el dodecaedro para simular un mundo inmenso
        dodecaedro.position += skyOffset;
        dodecaedro.localScale *= dodecaedroScaleFactor;
        // Desactivar controladores y efectos que interfieran en el modo inmersivo
        DesactivarCharacterController();
        DesactivarLocomocion();
        ReducirFOV();
        DesactivarObjetos();
        // Iniciar la coroutine que instancia la pelota y activa el seguimiento
        StartCoroutine(IngresarNoParentCoroutine());
    }

    // Coroutine que instancia la pelota, la prepara y activa el estado "dentro".
    private IEnumerator IngresarNoParentCoroutine()
    {
        if (ejecutandoIngresar || playerDentro)
            yield break;

        ejecutandoIngresar = true;

        if (pelotaPlayerPrefab != null && spawnPoint != null)
        {
            // Instanciar la pelota en la posición calculada y parentearla al spawnPoint
            currentBallInstance = Instantiate(
                pelotaPlayerPrefab,
                spawnPoint.position + rawOffset,
                spawnPoint.rotation,
                spawnPoint
            );
            asientoGO = currentBallInstance;

            // Desactivar física y colisión si el prefab las trae (para control manual)
            var rb = currentBallInstance.GetComponent<Rigidbody>();
            var col = currentBallInstance.GetComponent<Collider>();
            if (rb) rb.isKinematic = true;
            if (col) col.enabled = false;
        }
        // Pequeña espera para estabilizar transformaciones antes de activar seguimiento
        yield return new WaitForSeconds(0.3f);

        followBall = true;
        playerDentro = true;
        ejecutandoIngresar = false;
        // Activar filtro de audio si existe
        filtro?.ActivarFiltroMuffled();
        // Iniciar recorrido por la ruta hamiltoniana
        StartHamiltonianPathTraversal();
    }

    // Salir: restaurar estados originales y teletransportar al jugador al suelo
    public void Salir()
    {
        // 1) Restaurar c�mara si estaba reducida
        AumentarFOV();

        // 2) Reset del dodecaedro
        dodecaedro.position = originalDodecaedroPosition;
        dodecaedro.rotation = originalDodecaedroRotation;
        dodecaedro.localScale = originalDodecaedroScale;

        followBall = false;

        // Desparentear la rig del asiento si estaba parentada
        if (jugadorRig != null)
            jugadorRig.transform.SetParent(null);
        // Reactivar controladores y locomoción
        ActivarCharacterController();
        ActivarLocomocion();
        filtro?.DesactivarFiltroMuffled();
        // Destruir la pelota instanciada con un pequeño delay
        if (currentBallInstance != null)
            Destroy(currentBallInstance, 1f);
        // Reactivar objetos del escenario
        ActivarObjetos();

        playerDentro = false;

        // Iniciar teletransporte demorado para asegurar posición segura
        StartCoroutine(DelayedTeleport());
    }

    // Red de seguridad: si el script se desactiva en mitad del ride, devolver c�mara
    void OnDisable()
    {
        if (fovReducido) AumentarFOV();
    }

    // Coroutine que espera un frame y luego solicita teletransporte al anchor del suelo
    private IEnumerator DelayedTeleport()
    {
        yield return new WaitForEndOfFrame(); // Let transforms settle
        yield return new WaitForSeconds(0.1f); // Extra delay if needed

        if (sueloTP != null)
            sueloTP.RequestTeleport();
    }

    // Inicia el recorrido únicamente si el jugador está dentro y existe la pelota
    public void StartHamiltonianPathTraversal()
    {
        if (!playerDentro || currentBallInstance == null)
        {
            Debug.LogWarning("Cannot start path traversal. Player not inside the ball.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(TraversePathCoroutine());
    }
    // Recorre la lista de pins y mueve la pelota entre objetivos calculados.
    // Durante el movimiento también aplica una rotación suave que combina forward + "gravedad" hacia el centro.
    private IEnumerator TraversePathCoroutine()
    {
        if (dodecaedroScript == null || dodecaedroScript.placedPins == null || dodecaedroScript.placedPins.Count < 2)
        {
            Debug.LogWarning("Not enough pins to traverse.");
            yield break;
        }

        var node = dodecaedroScript.placedPins.First;

        while (node != null)
        {
            Vector3 pinPos = node.Value.pinObject.transform.position;
            Vector3 direction = (pinPos - dodecaedro.position).normalized;
            Vector3 targetPos = pinPos + direction * offsetDistance;

            // Mover hasta estar suficientemente cerca del target
            while (Vector3.Distance(currentBallInstance.transform.position, targetPos) > 3f)
            {
                Vector3 moveDir = (targetPos - currentBallInstance.transform.position).normalized;
                currentBallInstance.transform.position += moveDir * traversalSpeed * Time.deltaTime;

                // --- Rotaci�n con gravedad simulada ---
                if (moveDir != Vector3.zero)
                {
                    // Forward: direcci�n del movimiento
                    Quaternion targetForwardRot = Quaternion.LookRotation(moveDir, Vector3.up);

                    // Gravedad: inclinaci�n hacia el centro del dodecaedro
                    Vector3 gravityDir = (dodecaedro.position - jugadorRig.transform.position).normalized;
                    Quaternion gravityRot = Quaternion.FromToRotation(jugadorRig.transform.up, gravityDir);

                    // Combina ambas y aplica suavemente
                    Quaternion targetRot = gravityRot * targetForwardRot;

                    jugadorRig.transform.rotation = Quaternion.Slerp(jugadorRig.transform.rotation, targetRot, 2f * Time.deltaTime);
                }

                yield return null;
            }
            // Asegurar posición exacta al llegar al target y avanzar al siguiente pin
            currentBallInstance.transform.position = targetPos;
            node = node.Next;
            yield return null;
        }

        // Al finalizar, restaurar estado y salir

        followBall = false;

        dodecaedro.position = originalDodecaedroPosition;
        dodecaedro.rotation = originalDodecaedroRotation;
        dodecaedro.localScale = originalDodecaedroScale;

        //filtro?.DesactivarFiltroMuffled();
        playerDentro = false;
        Salir();
    }

    // Reduce el FOV y ajusta clipping planes para mejorar sensación inmersiva
    void ReducirFOV()
    {
        if (xrCamera == null || fovReducido) return;

        originalFOV = xrCamera.fieldOfView;
        originalNearClip = xrCamera.nearClipPlane;
        originalFarClip = xrCamera.farClipPlane;

        xrCamera.fieldOfView = 45f;
        xrCamera.nearClipPlane = 0.01f;
        xrCamera.farClipPlane = 50f;

        fovReducido = true;
    }

    // Restaura los valores originales de la cámara
    void AumentarFOV()
    {
        if (xrCamera == null || !fovReducido) return;

        xrCamera.fieldOfView = originalFOV;
        xrCamera.nearClipPlane = originalNearClip;
        xrCamera.farClipPlane = originalFarClip;

        fovReducido = false;
    }

    // Desactiva el CharacterController para evitar interferencias con la posición manual
    void DesactivarCharacterController()
    {
        if (characterController != null && characterController.enabled)
            characterController.enabled = false;
    }
    // Reactiva el CharacterController
    void ActivarCharacterController()
    {
        if (characterController != null && !characterController.enabled)
            characterController.enabled = true;
    }

    // Desactiva providers de locomoción (movimiento y teletransporte)
    void DesactivarLocomocion()
    {
        if (moveProvider != null)
            moveProvider.enabled = false;
        if (teleportationProvider != null)
            teleportationProvider.enabled = false;
    }
    // Reactiva providers de locomoción
    void ActivarLocomocion()
    {
        if (moveProvider != null)
            moveProvider.enabled = true;
        if (teleportationProvider != null)
            teleportationProvider.enabled = true;
    }

    // Activa todos los objetos listados en 'objetos'
    public void ActivarObjetos()
    {
        foreach (GameObject obj in objetos)
            if (obj != null) obj.SetActive(true);
    }

    // Desactiva todos los objetos listados en 'objetos'
    public void DesactivarObjetos()
    {
        foreach (GameObject obj in objetos)
            if (obj != null) obj.SetActive(false);
    }
}
