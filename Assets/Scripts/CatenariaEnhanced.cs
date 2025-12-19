using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/*
 Controla la simulación de una catenaria compuesta por múltiples cubos.
 Gestiona animación, físicas, reseteos y eventos de interacción XR.
*/
public class CatenariaEnhanced : MonoBehaviour
{
    // Lista de prefabs disponibles para instanciar
    [Header("Prefabs")]
    public List<GameObject> prefabList;

    // Elementos de interfaz
    [Header("UI Elements")]
    public TMP_Dropdown prefabDD;
    public Button animacionBTN;

    // Estado actual en ejecución
    [Header("Runtime")]
    public string currentPrefabST;
    public GameObject currentGO;

    // Piezas físicas de la catenaria
    [Header("Catenaria Pieces")]
    public List<Rigidbody> cubos = new List<Rigidbody>();

    // Objeto auxiliar con bisagra
    public GameObject respaldar;

    // Parámetros de animación
    [Header("Animation Settings")]
    public float liftHeight = 2f;
    public float liftDuration = 1.5f;
    public float rotateZDegrees = 90f;
    public float rotateZDuration = 1f;
    public float rotateYDegrees = 90f;
    public float rotateYDuration = 1f;
    public float dropDelay = 0.5f;

    // Ajustes de físicas
    [Header("Physics Tuning")]
    public float gravityForceModifier = 0.2f;
    public float drag = 0.0f;
    public float angularDrag = 1f;

    // Transformación original del prefab
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Referencia a script externo de exhibición
    public ExhibicionScript exhibicionScript; 

    // Controla si las físicas pueden reactivarse manualmente
    private bool fisicasArtificialesApagables = false;

    // Control de captura de pose original
    private bool originalPoseCaptured = false;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;


    private void Start()
    {
        // Asigna acción al botón de animación
        if (animacionBTN != null)
            animacionBTN.onClick.AddListener(BeginDropSequence);
        // Subscribe to dropdown change event
        /*if (prefabDD != null)
        {
            prefabDD.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        // Optional: Trigger initial prefab
        if (prefabDD != null && prefabDD.options.Count > 0)
        {
            OnDropdownValueChanged(prefabDD.value);
        }*/
        ActualizarCubos();
    }

    // Actualiza la lista de rigidbodies hijos del objeto actual
    public void ActualizarCubos()
    {
        Debug.LogWarning("En metodo: ActualizarCubos");
        cubos.Clear();

        if (currentGO == null)
        {
            Debug.LogWarning("No currentGO assigned. Cannot update cubos list.");
            PostReset();
            return;
        }

        Rigidbody[] rbs = currentGO.GetComponentsInChildren<Rigidbody>(includeInactive: true);

        foreach (var rb in rbs)
        {
            if (rb != null && !cubos.Contains(rb))
                cubos.Add(rb);
        }

        Debug.Log($"Cubos list updated. Total: {cubos.Count} rigidbodies.");
    }

    /*private void OnDropdownValueChanged(int index)
    {
        if (prefabDD == null || index < 0 || index >= prefabDD.options.Count)
            return;

        string selectedName = prefabDD.options[index].text;
        PrefabChange(selectedName);
    }*/

    // Cambia el prefab activo según el nombre
    public void PrefabChange(string newPrefabName)
    {
        currentPrefabST = newPrefabName;

        foreach (GameObject prefab in prefabList)
        {
            if (prefab != null && prefab.name == newPrefabName)
            {
                // Elimina instancia previa
                if (currentGO != null)
                {
                    Destroy(currentGO);
                }

                 // Instancia el nuevo prefab
                currentGO = Instantiate(prefab, transform);

                // Guarda la pose original una sola vez
                if (currentGO != null && !originalPoseCaptured)
                {
                    originalPosition = currentGO.transform.position;        // world
                    originalRotation = currentGO.transform.rotation;
                    originalLocalPosition = currentGO.transform.localPosition;   // local
                    originalLocalRotation = currentGO.transform.localRotation;
                    originalPoseCaptured = true;
                }


                currentGO.name = prefab.name; // Clean name (optional)
                currentGO.transform.localPosition = Vector3.zero; // Optional alignment
                currentGO.transform.localRotation = Quaternion.identity;

                return;
            }
        }

        Debug.LogWarning($"Prefab with name '{newPrefabName}' not found in prefab list.");
    }

    // Inicia la secuencia de animación y caída
    public void BeginDropSequence()
    {
        Debug.LogWarning("En metodo: BeginDropSequence");
        animacionBTN.interactable = false; // Disable button to prevent multiple clicks
        StopAllCoroutines();
        if (currentGO != null)
            StartCoroutine(DropSequenceRoutine(currentGO));
    }

    // Secuencia principal de animación y físicas
    private IEnumerator DropSequenceRoutine(GameObject currentGO)
{
    Debug.LogWarning("En corutina: DropSequenceRoutine");

    // 0) Guard null BEFORE touching currentGO.transform
    if (currentGO == null)
        yield break;

    // Keep a consistent reference used by ResetPositionRoutine()
    this.currentGO = currentGO;

    // 1) Capture origin ONCE per instantiation (world + local)
    if (!originalPoseCaptured)
    {
        originalPosition      = currentGO.transform.position;       // world
        originalRotation      = currentGO.transform.rotation;
        originalLocalPosition = currentGO.transform.localPosition;  // local
        originalLocalRotation = currentGO.transform.localRotation;
        originalPoseCaptured  = true;
    }

    // 2) Cache child rigidbodies (fall back to children if list is empty)
    Rigidbody[] pieceRBs = cubos != null && cubos.Count > 0
        ? cubos.ToArray()
        : currentGO.GetComponentsInChildren<Rigidbody>(includeInactive: false);

    // 3) Set all to kinematic
    foreach (var rb in pieceRBs)
    {
        if (!rb) continue;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    // 4) Raise the group
    Vector3 startPos = currentGO.transform.position; // use current (we already captured "original" once)
    Vector3 endPos   = startPos + Vector3.up * liftHeight;
    yield return MoveOverTime(currentGO.transform, startPos, endPos, liftDuration);

    // 5) Rotate in Z
    yield return RotateOverTime(currentGO.transform, Vector3.forward, rotateZDegrees, rotateZDuration);

    // 6) Rotate in Y
    yield return RotateOverTime(currentGO.transform, Vector3.down, rotateYDegrees, rotateYDuration);

    // 7) Wait before drop
    yield return new WaitForSeconds(dropDelay);

    // 8) Set pieces to dynamic with adjusted physics
    foreach (var rb in pieceRBs)
    {
        if (!rb) continue;
        rb.isKinematic = false;
        rb.AddForce(Physics.gravity * gravityForceModifier * 2f, ForceMode.Acceleration);
        rb.linearDamping  = drag;
        rb.angularDamping = angularDrag;
    }

    // 9) Reset position (delayed)
    yield return ResetPositionRoutine();
}

    private IEnumerator ResetPositionRoutine()
    {
        Debug.LogWarning("En corutina: ResetPositionRoutine");
        Debug.Log($"Resetting position. Current count: {cubos.Count} cubos.");
        yield return new WaitForSeconds(1.5f);

        // Step 1: Detach all rigidbodies (cubos) from parent
        List<Transform> detachedTransforms = new List<Transform>();
        foreach (var rb in cubos)
        {
            if (rb != null && rb.transform.parent == currentGO.transform)
            {
                detachedTransforms.Add(rb.transform);
                rb.transform.SetParent(null, true); // Detach from currentGO
            }
        }

        yield return null; // Let hierarchy settle

        // Step 2: Reset parent transform
        yield return new WaitForSeconds(7f);
        currentGO.transform.SetPositionAndRotation(originalPosition, originalRotation);

        yield return null;

        // Step 3: Reparent and freeze physics
        foreach (var t in detachedTransforms)
        {
            var rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            t.SetParent(currentGO.transform, true);
            Debug.Log($"Reparented: {t.name} at {t.position}");
        }

        yield return null;

        // Step 4: Unfreeze and apply artificial gravity
        foreach (var rb in cubos)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                StartCoroutine(ApplyCustomGravity(rb, 60f, 0.05f));
            }
        }

        yield return new WaitForSeconds(1.5f);

        animacionBTN.interactable = true;
        fisicasArtificialesApagables = true;

        Debug.Log($"Reset complete. {cubos.Count} cubos handled.");
    }
    
    // Reactiva físicas tras interacción
    public void ReactivatePhysics()
    {
        Debug.LogWarning("En metodo: ReactivatePhysics");
        if (currentGO == null || !fisicasArtificialesApagables) return;

        foreach (var rb in cubos)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                rb.useGravity = true;

                StartCoroutine(ApplyCustomGravity(rb, 5f, 0.05f));
            }
        }

        Debug.Log($"Physics reactivated for {cubos.Count} rigidbodies.");

        fisicasArtificialesApagables = false;
    }

    // Aplica gravedad personalizada por tiempo limitado
    IEnumerator ApplyCustomGravity(Rigidbody rb, float duration, float intensity = 1f)
    {
        Debug.LogWarning("En corutina: ApplyCustomGravity");
        float timer = 0f;
        while (timer < duration)
        {
            rb.AddForce(Physics.gravity * intensity, ForceMode.Acceleration);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    // Movimiento interpolado
    private IEnumerator MoveOverTime(Transform t, Vector3 from, Vector3 to, float duration)
    {
        Debug.LogWarning("En corutina: MoveOverTime");
        float elapsed = 0f;
        while (elapsed < duration)
        {
            t.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.position = to;
    }

    // Rotación interpolada
    private IEnumerator RotateOverTime(Transform t, Vector3 axis, float angle, float duration)
    {
        Debug.LogWarning("En corutina: RotateOverTime");
        float elapsed = 0f;
        float currentAngle = 0f;

        while (elapsed < duration)
        {
            float step = (angle / duration) * Time.deltaTime;
            currentAngle += step;
            Quaternion initialRot = t.rotation;
            Quaternion targetRot = t.rotation * Quaternion.AngleAxis(angle, axis);

            while (elapsed < duration)
            {
                t.rotation = Quaternion.Slerp(initialRot, targetRot, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.rotation = targetRot;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Reasigna el objeto tras un reset externo
    public void PostReset()
    {
        Transform found = transform.Find("Cubos catenaria(Clone)");

        if (found != null)
        {
            currentGO = found.gameObject;

            // 
            if (currentGO != null && !originalPoseCaptured)
            {
                originalPosition = currentGO.transform.position;        // world
                originalRotation = currentGO.transform.rotation;
                originalLocalPosition = currentGO.transform.localPosition;   // local
                originalLocalRotation = currentGO.transform.localRotation;
                originalPoseCaptured = true;
            }

            Debug.Log("PostReset: "+ found.name+" assigned to currentGO.");
            ActualizarCubos();
            Debug.Log("Se actualizaron los cubos, ahora se traran los listeners");
            // Add XRGrabInteractable listener to call ReactivatePhysics on grab
            /*foreach (Rigidbody rb in cubos)
            {
                if (rb == null) continue;

                XRGrabInteractable grab = rb.GetComponent<XRGrabInteractable>();
                if (grab != null)
                {
                    grab.selectEntered.RemoveAllListeners(); // optional safety
                    grab.selectEntered.AddListener(_ => ReactivatePhysics());
                    Debug.Log($"Listener added to: {grab.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"GrabInteractable missing on: {rb.gameObject.name}");
                }
            }*/
            SetListenersCubos();
        }
        else
        {
            Debug.LogWarning("PostReset: 'Cubos catenaria' not found under this object.");
            currentGO = null;
            cubos.Clear();
        }
        
    }
    
    // Inicia un reseteo completo de la catenaria
    public void ResetCatenaria()
    {
        animacionBTN.interactable = false;      // Bloquea el botón
        StopAllCoroutines();                    // Detiene animaciones activas
        StartCoroutine(ResetCatenariaRoutine());
    }

    // Rutina que destruye y reconstruye la catenaria
    private IEnumerator ResetCatenariaRoutine()
    {
        Debug.LogWarning("ResetCatenariaRoutine: Starting reset...");

        if (animacionBTN != null)
            animacionBTN.interactable = true;

        fisicasArtificialesApagables = false;

        // Libera los cubos para que salgan disparados
        foreach (var rb in cubos)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                // Fuerza hacia arriba con dispersión aleatoria
                Vector3 playfulDirection = (Vector3.up * 5f) + new Vector3(
                    Random.Range(-1f, 1f),
                    0f,
                    Random.Range(-1f, 1f)
                ) * 2f;
                rb.AddForce(playfulDirection, ForceMode.Impulse);
            }
        }

        // Tiempo para permitir el movimiento de los cubos
        yield return new WaitForSeconds(0.5f);

        // Baja el respaldar usando su bisagra
        if (respaldar != null)
        {
            HingeJoint hinge = respaldar.GetComponent<HingeJoint>();
            if (hinge != null)
            {
                Debug.Log("Rotating respaldar using hinge...");
                yield return LayDownRespaldar(hinge, targetAngle: 0f);  // 0 = lay down
            }
            else
            {
                Debug.LogWarning("Respaldar has no hinge!");
            }
        }

        // Elimina los cubos anteriores
        foreach (var rb in cubos)
        {
            if (rb != null)
                Destroy(rb.gameObject);
        }
        cubos.Clear();
        currentGO = null;

        // Reinicia la exhibición para recrear objetos
        if (exhibicionScript != null)
            exhibicionScript.ResetExhibicion();

        // Espera breve para estabilidad
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.1f);

        // Reasigna el nuevo objeto instanciado
        PostReset();

        yield return new WaitForSeconds(2f);
        animacionBTN.interactable = true;
    }

    // Baja el respaldar aplicando un resorte en la bisagra
    private IEnumerator LayDownRespaldar(HingeJoint hinge, float targetAngle = 0f, float speed = 500f)
    {
        if (hinge == null) yield break;

        JointSpring spring = hinge.spring;
        spring.spring = speed;
        spring.damper = 10f;
        spring.targetPosition = targetAngle;
        hinge.spring = spring;
        hinge.useSpring = true;

        Debug.Log("Applying spring to move respaldar toward " + targetAngle);

        // Espera hasta alcanzar el ángulo deseado
        while (Mathf.Abs(hinge.angle - targetAngle) > 1f)
        {
            yield return null;
        }

        // Pequeña pausa de estabilización
        yield return new WaitForSeconds(0.3f);

        // Desactiva el resorte
        hinge.useSpring = false;

        Debug.Log("Respaldar reached target and spring disabled.");
    }

    // Fuerza un reseteo inmediato de físicas y jerarquía
    public void ForceResetPhysics()
    {
        Debug.LogWarning("ForceResetPhysics: Aborting all coroutines and restoring default physics");

        StopAllCoroutines();

        if (animacionBTN != null)
            animacionBTN.interactable = true;

        fisicasArtificialesApagables = false;

        if (currentGO == null)
        {
            Debug.LogWarning("ForceResetPhysics: currentGO is null. Cannot reparent or reset.");
            return;
        }

        // Desacopla cubos del objeto padre
        List<Transform> detachedTransforms = new List<Transform>();
        foreach (var rb in cubos)
        {
            if (rb != null && rb.transform.parent == currentGO.transform)
            {
                detachedTransforms.Add(rb.transform);
                rb.transform.SetParent(null, true); // Detach from currentGO
            }
        }

        // Restaura la transformación original del padre
        currentGO.transform.SetPositionAndRotation(originalPosition, originalRotation);
       
       // Reasigna cubos y restablece físicas
        foreach (var rb in cubos)
        {
            if (rb == null) continue;

            // Ensure it's a child of currentGO
            if (rb.transform.parent != currentGO.transform)
            {
                Debug.LogWarning($"ForceResetPhysics: Reparenting {rb.gameObject.name}");
                rb.transform.SetParent(currentGO.transform, true);
            }

            // Reset physics
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
        }

        Debug.Log($"ForceResetPhysics: Completed for {cubos.Count} rigidbodies.");
    }

    // Asigna listeners XR a los cubos
    public void SetListenersCubos()
    {
        foreach (Rigidbody rb in cubos)
        {
            if (rb == null) continue;

            XRGrabInteractable grab = rb.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                // Optional: Remove previous listeners to avoid duplicates
                grab.selectExited.RemoveAllListeners();

                // Add listener to call ForceResetPhysics when grab is released
                grab.selectExited.AddListener(_ => ForceResetPhysics());

                Debug.Log($"Listener added to selectExited of: {grab.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"XRGrabInteractable missing on: {rb.gameObject.name}");
            }
        }
        
    }

}
