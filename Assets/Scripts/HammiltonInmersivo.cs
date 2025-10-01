using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using System.Collections;
using System.Collections.Generic;

public class HammiltonInmersivo : MonoBehaviour
{
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
    public Vector3 skyOffset = new Vector3(0f, 200f, 0f); // estaba en 100

    [Header("Traversal Settings")]
    public float traversalSpeed = 5f;

    [SerializeField]
    private List<GameObject> objetos;

    private GameObject currentBallInstance;
    private GameObject asientoGO;

    private Vector3 jugadorRigOriginalWorldScale;
    private bool playerDentro = false;
    private bool ejecutandoIngresar = false;
    private bool followBall = false;

    private float originalFOV;
    private float originalNearClip;
    private float originalFarClip;
    private bool fovReducido = false;

    private Vector3 anchorSpawn;
    private Vector3 pinSpawn;
    private Vector3 rawOffset;

    private Vector3 originalDodecaedroPosition;
    private Quaternion originalDodecaedroRotation;
    private Vector3 originalDodecaedroScale;

    [Header("Offset Settings")]
    public float offsetDistance = 1.5f; // Puedes ajustar este valor desde el Inspector



    void FixedUpdate()
    {
        if (followBall && currentBallInstance != null && jugadorRig != null)
        {
            jugadorRig.transform.position = currentBallInstance.transform.position;
        }
    }

    public void IngresarNoParent()
    {
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

        anchorSpawn = firstPin.anchor.position;
        pinSpawn = firstPin.pinObject.transform.position;
        rawOffset = (pinSpawn - anchorSpawn) * 2.5f;

        originalDodecaedroPosition = dodecaedro.position;
        originalDodecaedroRotation = dodecaedro.rotation;
        originalDodecaedroScale = dodecaedro.localScale;

        dodecaedro.position += skyOffset;
        dodecaedro.localScale *= dodecaedroScaleFactor;

        DesactivarCharacterController();
        DesactivarLocomocion();
        ReducirFOV();
        DesactivarObjetos();

        StartCoroutine(IngresarNoParentCoroutine());
    }

    private IEnumerator IngresarNoParentCoroutine()
    {
        if (ejecutandoIngresar || playerDentro)
            yield break;

        ejecutandoIngresar = true;

        if (pelotaPlayerPrefab != null && spawnPoint != null)
        {
            currentBallInstance = Instantiate(
                pelotaPlayerPrefab,
                spawnPoint.position + rawOffset,
                spawnPoint.rotation,
                spawnPoint
            );
            asientoGO = currentBallInstance;

            var rb = currentBallInstance.GetComponent<Rigidbody>();
            var col = currentBallInstance.GetComponent<Collider>();
            if (rb) rb.isKinematic = true;
            if (col) col.enabled = false;
        }

        yield return new WaitForSeconds(0.3f);

        followBall = true;
        playerDentro = true;
        ejecutandoIngresar = false;

        filtro?.ActivarFiltroMuffled();

        StartHamiltonianPathTraversal();
    }

    public void Salir()
    {
        // 1) Restaurar cámara si estaba reducida
        AumentarFOV();

        // 2) Reset del dodecaedro
        dodecaedro.position = originalDodecaedroPosition;
        dodecaedro.rotation = originalDodecaedroRotation;
        dodecaedro.localScale = originalDodecaedroScale;

        followBall = false;

        if (jugadorRig != null)
            jugadorRig.transform.SetParent(null);

        ActivarCharacterController();
        ActivarLocomocion();
        filtro?.DesactivarFiltroMuffled();

        if (currentBallInstance != null)
            Destroy(currentBallInstance, 1f);

        ActivarObjetos();

        playerDentro = false;

        StartCoroutine(DelayedTeleport());
    }

    // Red de seguridad: si el script se desactiva en mitad del ride, devolver cámara
    void OnDisable()
    {
        if (fovReducido) AumentarFOV();
    }


    private IEnumerator DelayedTeleport()
    {
        yield return new WaitForEndOfFrame(); // Let transforms settle
        yield return new WaitForSeconds(0.1f); // Extra delay if needed

        if (sueloTP != null)
            sueloTP.RequestTeleport();
    }


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

            while (Vector3.Distance(currentBallInstance.transform.position, targetPos) > 3f)
            {
                Vector3 moveDir = (targetPos - currentBallInstance.transform.position).normalized;
                currentBallInstance.transform.position += moveDir * traversalSpeed * Time.deltaTime;

                // --- Rotación con gravedad simulada ---
                if (moveDir != Vector3.zero)
                {
                    // Forward: dirección del movimiento
                    Quaternion targetForwardRot = Quaternion.LookRotation(moveDir, Vector3.up);

                    // Gravedad: inclinación hacia el centro del dodecaedro
                    Vector3 gravityDir = (dodecaedro.position - jugadorRig.transform.position).normalized;
                    Quaternion gravityRot = Quaternion.FromToRotation(jugadorRig.transform.up, gravityDir);

                    // Combina ambas y aplica suavemente
                    Quaternion targetRot = gravityRot * targetForwardRot;

                    jugadorRig.transform.rotation = Quaternion.Slerp(jugadorRig.transform.rotation, targetRot, 2f * Time.deltaTime);
                }

                yield return null;
            }

            currentBallInstance.transform.position = targetPos;
            node = node.Next;
            yield return null;
        }


        followBall = false;

        dodecaedro.position = originalDodecaedroPosition;
        dodecaedro.rotation = originalDodecaedroRotation;
        dodecaedro.localScale = originalDodecaedroScale;

        //filtro?.DesactivarFiltroMuffled();
        playerDentro = false;
        Salir();
    }


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

    void AumentarFOV()
    {
        if (xrCamera == null || !fovReducido) return;

        xrCamera.fieldOfView = originalFOV;
        xrCamera.nearClipPlane = originalNearClip;
        xrCamera.farClipPlane = originalFarClip;

        fovReducido = false;
    }

    void DesactivarCharacterController()
    {
        if (characterController != null && characterController.enabled)
            characterController.enabled = false;
    }

    void ActivarCharacterController()
    {
        if (characterController != null && !characterController.enabled)
            characterController.enabled = true;
    }

    void DesactivarLocomocion()
    {
        if (moveProvider != null)
            moveProvider.enabled = false;
        if (teleportationProvider != null)
            teleportationProvider.enabled = false;
    }

    void ActivarLocomocion()
    {
        if (moveProvider != null)
            moveProvider.enabled = true;
        if (teleportationProvider != null)
            teleportationProvider.enabled = true;
    }

    public void ActivarObjetos()
    {
        foreach (GameObject obj in objetos)
            if (obj != null) obj.SetActive(true);
    }

    public void DesactivarObjetos()
    {
        foreach (GameObject obj in objetos)
            if (obj != null) obj.SetActive(false);
    }
}
