using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using System.Collections;
using System.Collections.Generic;

public class HammiltonInmersivo : MonoBehaviour
{
    /*
     Controla la experiencia inmersiva dentro del dodecaedro siguiendo un camino Hamiltoniano,
     con efectos de cámara, filtro de audio y movimiento del jugador.
    */

    [Header("References")]
    public GameObject jugadorRig;
    public Transform xrOrigin;
    public CharacterController characterController;
    public Camera xrCamera;
    public FiltroAudio filtro;
    public DodecaedroScript dodecaedroScript;

    [Header("Locomotion")]
    public TeleportationProvider teleportationProvider;
    public ContinuousMoveProvider moveProvider;
    public ContinuousTurnProvider turnProvider;

    [Header("Prefabs & Positions")]
    public GameObject pelotaPlayerPrefab;
    public Transform spawnPoint = null;

    [Header("Teleport Targets")]
    public TeleportationAnchor sueloTP;

    [Header("World Immersion (Dodecahedron-Based)")]
    public Transform dodecaedro;
    public float dodecaedroScaleFactor = 250f;
    public Vector3 skyOffset = new Vector3(0f, 200f, 0f); // Offset del dodecaedro hacia el cielo / Estaba en 100

    [Header("Traversal Settings")]
    public float traversalSpeed = 5f;

    [SerializeField]
    private List<GameObject> objetos;

    private GameObject currentBallInstance;      // Instancia de la bola del jugador
    private GameObject asientoGO;                // Referencia al asiento/bola

    private Vector3 jugadorRigOriginalWorldScale;
    private bool playerDentro = false;           // Si el jugador está dentro del dodecaedro
    private bool ejecutandoIngresar = false;     // Bandera de corrutina en ejecución
    private bool followBall = false;             // Si el jugador debe seguir la bola

    private float originalFOV;                   // FOV original de la cámara
    private float originalNearClip;              // Near clip plane original
    private float originalFarClip;               // Far clip plane original
    private bool fovReducido = false;            // Flag si FOV fue reducido

    private Vector3 anchorSpawn;                 // Posición del anclaje
    private Vector3 pinSpawn;                    // Posición del primer pin
    private Vector3 rawOffset;                   // Offset calculado

    private Vector3 originalDodecaedroPosition;  // Posición original del dodecaedro
    private Quaternion originalDodecaedroRotation; // Rotación original
    private Vector3 originalDodecaedroScale;     // Escala original

    [Header("Offset Settings")]
    public float offsetDistance = 1.5f;          // Distancia de offset desde los pins



    void FixedUpdate()
    {
        // Mantiene el jugador siguiendo la posición de la bola
        if (followBall && currentBallInstance != null && jugadorRig != null)
        {
            jugadorRig.transform.position = currentBallInstance.transform.position;
        }
    }

    // Ingresa al modo inmersivo sin parentear el jugador
    public void IngresarNoParent()
    {
        // Verifica que haya al menos un pin colocado
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

        // Calcula posiciones y offset desde el primer pin
        anchorSpawn = firstPin.anchor.position;
        pinSpawn = firstPin.pinObject.transform.position;
        rawOffset = (pinSpawn - anchorSpawn) * 2.5f;

        // Guarda estado original del dodecaedro
        originalDodecaedroPosition = dodecaedro.position;
        originalDodecaedroRotation = dodecaedro.rotation;
        originalDodecaedroScale = dodecaedro.localScale;

        // Escala y desplaza el dodecaedro hacia el cielo
        dodecaedro.position += skyOffset;
        dodecaedro.localScale *= dodecaedroScaleFactor;

        // Desactiva sistemas normales y prepara inmersión
        DesactivarCharacterController();
        DesactivarLocomocion();
        ReducirFOV();
        DesactivarObjetos();

        StartCoroutine(IngresarNoParentCoroutine());
    }

    // Corrutina que instancia la bola y comienza el recorrido
    private IEnumerator IngresarNoParentCoroutine()
    {
        // Evita ejecuciones simultáneas
        if (ejecutandoIngresar || playerDentro)
            yield break;

        ejecutandoIngresar = true;

        // Instancia la bola del jugador en el punto de entrada
        if (pelotaPlayerPrefab != null && spawnPoint != null)
        {
            currentBallInstance = Instantiate(
                pelotaPlayerPrefab,
                spawnPoint.position + rawOffset,
                spawnPoint.rotation,
                spawnPoint
            );
            asientoGO = currentBallInstance;

            // Desactiva física y colisiones de la bola para que sea controlada directamente
            var rb = currentBallInstance.GetComponent<Rigidbody>();
            var col = currentBallInstance.GetComponent<Collider>();
            if (rb) rb.isKinematic = true;
            if (col) col.enabled = false;
        }

        // Espera breve antes de iniciar el seguimiento
        yield return new WaitForSeconds(0.3f);

        followBall = true;
        playerDentro = true;
        ejecutandoIngresar = false;

        // Activa el filtro de audio muffled para inmersión
        filtro?.ActivarFiltroMuffled();

        // Inicia el recorrido del camino Hamiltoniano
        StartHamiltonianPathTraversal();
    }

    // Sale del modo inmersivo y restaura los sistemas normales
    public void Salir()
    {
        // 1) Restaura la cámara si fue reducida
        AumentarFOV();

        // 2) Restaura el dodecaedro a su estado original
        dodecaedro.position = originalDodecaedroPosition;
        dodecaedro.rotation = originalDodecaedroRotation;
        dodecaedro.localScale = originalDodecaedroScale;

        // Detiene el seguimiento de la bola
        followBall = false;

        if (jugadorRig != null)
            jugadorRig.transform.SetParent(null);

        // Reactiva los sistemas de locomotion
        ActivarCharacterController();
        ActivarLocomocion();
        filtro?.DesactivarFiltroMuffled();

        // Destruye la bola con un pequeño retraso
        if (currentBallInstance != null)
            Destroy(currentBallInstance, 1f);

        // Reactiva los objetos del mundo
        ActivarObjetos();

        playerDentro = false;

        StartCoroutine(DelayedTeleport());
    }

    // Red de seguridad: si el script se desactiva, restaura la cámara
    void OnDisable()
    {
        if (fovReducido) AumentarFOV();
    }


    // Teletransporta al jugador al suelo principal, con delay para estabilizar transforms
    private IEnumerator DelayedTeleport()
    {
        yield return new WaitForEndOfFrame(); // Permite que los transforms se estabilicen
        yield return new WaitForSeconds(0.1f); // Delay adicional si es necesario

        if (sueloTP != null)
            sueloTP.RequestTeleport();
    }


    // Inicia el recorrido del camino Hamiltoniano a través de los pines del dodecaedro
    public void StartHamiltonianPathTraversal()
    {
        // Verifica que el jugador esté dentro y la bola exista
        if (!playerDentro || currentBallInstance == null)
        {
            Debug.LogWarning("Cannot start path traversal. Player not inside the ball.");
            return;
        }

        // Detiene cualquier corrutina anterior
        StopAllCoroutines();
        // Inicia la nueva corrutina de traversal
        StartCoroutine(TraversePathCoroutine());
    }

    // Corrutina principal que recorre los pines del camino Hamiltoniano
    // Calcula movimiento hacia cada pin, aplica rotación hacia el centro del dodecaedro (gravedad simulada)
    private IEnumerator TraversePathCoroutine()
    {
        // Verifica que existan pines colocados para el recorrido
        if (dodecaedroScript == null || dodecaedroScript.placedPins == null || dodecaedroScript.placedPins.Count < 2)
        {
            Debug.LogWarning("Not enough pins to traverse.");
            yield break;
        }

        // Obtiene el primer pin del camino
        var node = dodecaedroScript.placedPins.First;

        // Itera a través de cada pin en el camino Hamiltoniano
        while (node != null)
        {
            // Calcula posición del pin y dirección desde el dodecaedro
            Vector3 pinPos = node.Value.pinObject.transform.position;
            Vector3 direction = (pinPos - dodecaedro.position).normalized;
            // Calcula posición objetivo con offset respecto al pin
            Vector3 targetPos = pinPos + direction * offsetDistance;

            // Aproximación al pin objetivo
            while (Vector3.Distance(currentBallInstance.transform.position, targetPos) > 3f)
            {
                // Calcula dirección hacia la posición objetivo
                Vector3 moveDir = (targetPos - currentBallInstance.transform.position).normalized;
                // Mueve la bola hacia el objetivo
                currentBallInstance.transform.position += moveDir * traversalSpeed * Time.deltaTime;

                // --- Aplica rotación con gravedad simulada hacia el centro del dodecaedro ---
                if (moveDir != Vector3.zero)
                {
                    // Rotación forward: orienta hacia la dirección del movimiento
                    Quaternion targetForwardRot = Quaternion.LookRotation(moveDir, Vector3.up);

                    // Gravedad simulada: inclinación hacia el centro del dodecaedro
                    Vector3 gravityDir = (dodecaedro.position - jugadorRig.transform.position).normalized;
                    Quaternion gravityRot = Quaternion.FromToRotation(jugadorRig.transform.up, gravityDir);

                    // Combina la rotación de movimiento con la de gravedad
                    Quaternion targetRot = gravityRot * targetForwardRot;

                    // Aplica interpolación suave para la rotación
                    jugadorRig.transform.rotation = Quaternion.Slerp(jugadorRig.transform.rotation, targetRot, 2f * Time.deltaTime);
                }

                yield return null;
            }

            // Snaps a la posición objetivo cuando está lo suficientemente cerca
            currentBallInstance.transform.position = targetPos;
            // Avanza al siguiente pin
            node = node.Next;
            yield return null;
        }

        // Detiene el seguimiento de la bola cuando termina el recorrido
        followBall = false;

        // Restaura el dodecaedro a su posición original
        dodecaedro.position = originalDodecaedroPosition;
        dodecaedro.rotation = originalDodecaedroRotation;
        dodecaedro.localScale = originalDodecaedroScale;

        //filtro?.DesactivarFiltroMuffled();
        playerDentro = false;
        Salir();
    }

    // Reduce el FOV y ajusta los planos de recorte de la cámara para efecto de inmersión
    void ReducirFOV()
    {
        // Si la cámara XR no existe o ya está reducida, no hace nada
        if (xrCamera == null || fovReducido) return;

        // Guarda los valores originales de la cámara
        originalFOV = xrCamera.fieldOfView;
        originalNearClip = xrCamera.nearClipPlane;
        originalFarClip = xrCamera.farClipPlane;

        // Aplica valores reducidos para efecto de zoom/inmersión
        xrCamera.fieldOfView = 45f;
        xrCamera.nearClipPlane = 0.01f;
        xrCamera.farClipPlane = 50f;

        fovReducido = true;
    }

    // Restaura el FOV y los planos de recorte originales de la cámara
    void AumentarFOV()
    {
        // Si no está reducido, no hace nada
        if (xrCamera == null || !fovReducido) return;

        // Restaura los valores guardados
        xrCamera.fieldOfView = originalFOV;
        xrCamera.nearClipPlane = originalNearClip;
        xrCamera.farClipPlane = originalFarClip;

        fovReducido = false;
    }

    // Desactiva el CharacterController del jugador
    void DesactivarCharacterController()
    {
        if (characterController != null && characterController.enabled)
            characterController.enabled = false;
    }

    // Reactiva el CharacterController del jugador
    void ActivarCharacterController()
    {
        if (characterController != null && !characterController.enabled)
            characterController.enabled = true;
    }

    // Desactiva los sistemas de locomotion (movimiento y teletransporte)
    void DesactivarLocomocion()
    {
        if (moveProvider != null)
            moveProvider.enabled = false;
        if (teleportationProvider != null)
            teleportationProvider.enabled = false;
    }

    // Reactiva los sistemas de locomotion
    void ActivarLocomocion()
    {
        // Reactiva el movimiento continuo
        if (moveProvider != null)
            moveProvider.enabled = true;
        // Reactiva el teletransporte
        if (teleportationProvider != null)
            teleportationProvider.enabled = true;
    }

    // Reactiva todos los objetos de la escena que fueron desactivados
    public void ActivarObjetos()
    {
        foreach (GameObject obj in objetos)
            if (obj != null) obj.SetActive(true);
    }

    // Desactiva todos los objetos de la escena para crear efecto de aislamiento inmersivo
    public void DesactivarObjetos()
    {
        foreach (GameObject obj in objetos)
            if (obj != null) obj.SetActive(false);
    }
}
