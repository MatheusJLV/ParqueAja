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
    public Transform spawnPoint=null; // Where the ball spawns (e.g., anchor or visual location)

    [Header("Scale Settings")]
    public float playerScaleFactor = 100f;

    [Header("Teleport Targets")]
    public TeleportationAnchor sueloTP;

    private GameObject currentBallInstance;
    private TeleportationAnchor asientoTP;
    private GameObject asientoGO;

    private Vector3 jugadorRigOriginalWorldScale;
    private bool playerDentro = false;
    private bool ejecutandoIngresar = false;

    private float originalFOV;
    private float originalNearClip;
    private float originalFarClip;
    private bool fovReducido = false;

    private Vector3 anchorSpawn;
    private Vector3 pinSpawn;

    private Vector3 traversalOffset = Vector3.zero;

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
        filtro.DesactivarFiltroMuffled(); // Activar filtro de audio
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

    public void Ingresar()
    {
        anchorSpawn= (Vector3)(dodecaedroScript.placedPins.First?.Value.anchor.position); // Get the first pin's anchor position
        pinSpawn= (Vector3)(dodecaedroScript.placedPins.First?.Value.pinObject.transform.position); // Get the first pin's pin position
        spawnPoint = dodecaedroScript.placedPins.First?.Value.anchor; // Get the first pin's anchor as spawn point
        if (spawnPoint == null)
        {
            Debug.LogError("No spawn point set. Please place a pin first.");
            return;
        }
        ReducirFOV();
        DesactivarLocomocion();
        DesactivarCharacterController();
        DesactivarObjetos();
        StartCoroutine(IngresarCoroutine());
    }

    private IEnumerator IngresarCoroutine()
    {
        if (ejecutandoIngresar || playerDentro) yield break;

        ejecutandoIngresar = true;

        // Instantiate the ball
        if (pelotaPlayerPrefab != null && spawnPoint != null)
        {
            traversalOffset = (pinSpawn - anchorSpawn)*1.5f;
            currentBallInstance = Instantiate(pelotaPlayerPrefab, spawnPoint.position + 1.5f* traversalOffset, spawnPoint.rotation, spawnPoint);
            asientoGO = currentBallInstance;

            var rb = currentBallInstance.GetComponent<Rigidbody>();
            var col = currentBallInstance.GetComponent<Collider>();
            if (rb) rb.isKinematic = true;
            if (col) col.enabled = false;

            asientoTP = currentBallInstance.GetComponentInChildren<TeleportationAnchor>();
        }

        yield return new WaitForSeconds(0.1f);

        if (asientoTP != null)
            asientoTP.RequestTeleport();

        yield return new WaitForSeconds(0.1f);
        yield return new WaitForEndOfFrame();

        // Parent and scale the player
        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRigOriginalWorldScale = jugadorRig.transform.lossyScale;
            jugadorRig.transform.SetParent(asientoGO.transform);
            jugadorRig.transform.localPosition = Vector3.zero;
            jugadorRig.transform.localRotation = Quaternion.identity;
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale / playerScaleFactor);
        }

        yield return new WaitForSeconds(0.3f); // let hierarchy settle

        var rbFinal = currentBallInstance?.GetComponent<Rigidbody>();
        var colFinal = currentBallInstance?.GetComponent<Collider>();
        if (rbFinal) rbFinal.isKinematic = false;
        if (colFinal) colFinal.enabled = true;

        playerDentro = true;
        ejecutandoIngresar = false;
        filtro?.ActivarFiltroMuffled();
        StartHamiltonianPathTraversal();
    }

    public void Salir()
    {
        ActivarCharacterController();
        ActivarLocomocion();
        filtro?.DesactivarFiltroMuffled();

        if (sueloTP != null)
            sueloTP.RequestTeleport();

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale);
        }

        if (currentBallInstance != null)
            Destroy(currentBallInstance, 1f);

        AumentarFOV();
        ActivarObjetos();
        playerDentro = false;
    }

    // ---------- Utility Methods ----------

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

    public void StartHamiltonianPathTraversal()
    {
        if (!playerDentro || currentBallInstance == null)
        {
            Debug.LogWarning("Cannot start path traversal. Player not inside the ball.");
            return;
        }

        StopAllCoroutines(); // In case anything else is running
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
            Vector3 targetPos = node.Value.anchor.position + traversalOffset;

            while (Vector3.Distance(currentBallInstance.transform.position, targetPos) > 0.01f)
            {
                Vector3 direction = (targetPos - currentBallInstance.transform.position).normalized;
                float speed = 0.1f;
                currentBallInstance.transform.position += direction * speed * Time.deltaTime;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
                    currentBallInstance.transform.rotation = Quaternion.Slerp(currentBallInstance.transform.rotation, targetRot, 5f * Time.deltaTime);
                }

                yield return null;
            }

            // Snap exactly
            currentBallInstance.transform.position = targetPos;


            node = node.Next;

            yield return null;
        }

        Debug.Log("Traversal complete.");
    }



}
