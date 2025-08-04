using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CatenariaEnhanced : MonoBehaviour
{
    [Header("Prefabs")]
    public List<GameObject> prefabList;

    [Header("UI Elements")]
    public TMP_Dropdown prefabDD;
    public Button animacionBTN;


    [Header("Runtime")]
    public string currentPrefabST;
    public GameObject currentGO;

    [Header("Catenaria Pieces")]
    public List<Rigidbody> cubos = new List<Rigidbody>();

    public GameObject respaldar;

    [Header("Animation Settings")]
    public float liftHeight = 2f;
    public float liftDuration = 1.5f;

    public float rotateZDegrees = 90f;
    public float rotateZDuration = 1f;

    public float rotateYDegrees = 90f;
    public float rotateYDuration = 1f;

    public float dropDelay = 0.5f;

    [Header("Physics Tuning")]
    public float gravityForceModifier = 0.2f;
    public float drag = 0.0f;
    public float angularDrag = 1f;


    private Vector3 originalPosition;
    private Quaternion originalRotation;

    public ExhibicionScript exhibicionScript; // Reference to the exhibition script

    private bool fisicasArtificialesApagables = false;

    private void Start()
    {
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

    public void PrefabChange(string newPrefabName)
    {
        currentPrefabST = newPrefabName;

        foreach (GameObject prefab in prefabList)
        {
            if (prefab != null && prefab.name == newPrefabName)
            {
                // Destroy current instance if any
                if (currentGO != null)
                {
                    Destroy(currentGO);
                }

                // Instantiate new prefab as child
                currentGO = Instantiate(prefab, transform);
                currentGO.name = prefab.name; // Clean name (optional)
                currentGO.transform.localPosition = Vector3.zero; // Optional alignment
                currentGO.transform.localRotation = Quaternion.identity;

                return;
            }
        }

        Debug.LogWarning($"Prefab with name '{newPrefabName}' not found in prefab list.");
    }



    public void BeginDropSequence()
    {
        Debug.LogWarning("En metodo: BeginDropSequence");
        animacionBTN.interactable = false; // Disable button to prevent multiple clicks
        if (currentGO != null)
            StartCoroutine(DropSequenceRoutine(currentGO));
    }

    private IEnumerator DropSequenceRoutine(GameObject currentGO)
    {
        Debug.LogWarning("En corutina: DropSequenceRoutine");
        originalPosition = currentGO.transform.position;
        originalRotation = currentGO.transform.rotation;

        if (currentGO == null)
            yield break;

        this.currentGO = currentGO; // Ensure reference is consistent inside resetPosition()

        // 1. Cache child rigidbodies
        Rigidbody[] pieceRBs = cubos.ToArray();

        // 2. Set all to kinematic
        foreach (var rb in pieceRBs)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // 3. Save original transform
        originalPosition = currentGO.transform.position;
        originalRotation = currentGO.transform.rotation;

        // 4. Raise the group
        Vector3 endPos = originalPosition + Vector3.up * liftHeight;
        yield return MoveOverTime(currentGO.transform, originalPosition, endPos, liftDuration);

        // 5. Rotate in Z
        yield return RotateOverTime(currentGO.transform, Vector3.forward, rotateZDegrees, rotateZDuration);

        // 6. Rotate in Y
        yield return RotateOverTime(currentGO.transform, Vector3.down, rotateYDegrees, rotateYDuration);

        // 7. Wait before drop
        yield return new WaitForSeconds(dropDelay);

        // 8. Set pieces to dynamic with adjusted physics
        foreach (var rb in pieceRBs)
        {
            rb.isKinematic = false;
            //rb.useGravity = true;
            rb.AddForce(Physics.gravity * gravityForceModifier*2, ForceMode.Acceleration); 
            rb.linearDamping = drag;            
            rb.angularDamping = angularDrag;

            // Note: DO NOT set Physics.gravity globally here � it affects all objects.
            // Instead simulate gravity scaling with custom forces if needed.
            // Example: rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
        }

        // 9. Reset position (delayed)
        yield return ResetPositionRoutine();
    }

    /*private IEnumerator ResetPositionRoutine()
    {
        // 1. Wait for pieces to settle
        Debug.Log($"currentGO has {currentGO.transform.childCount} children.");

        yield return new WaitForSeconds(1.5f);

        // 2. Detach children temporarily
        // Step 1: Copy all children BEFORE modifying parent
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < currentGO.transform.childCount; i++)
        {
            children.Add(currentGO.transform.GetChild(i));
        }

        // Step 2: Detach safely
        foreach (Transform child in children)
        {
            child.SetParent(null, true);
        }

        Debug.Log($"currentGO has {currentGO.transform.childCount} children.");

        yield return null; // Let transform hierarchy settle

        // 3. Wait more before resetting parent
        yield return new WaitForSeconds(7f);
        currentGO.transform.SetPositionAndRotation(originalPosition, originalRotation);
        Debug.Log($"currentGO has {currentGO.transform.childCount} children.");

        yield return null; // Let reset apply

        // 4. Reparent and freeze physics to avoid instant drift
        foreach (Transform child in children)
        {
            var rb = child.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
            }
            child.SetParent(currentGO.transform, true);
            Debug.Log($"currentGO has {currentGO.transform.childCount} children.");

            Debug.Log($"Child {child.name} reparented at {child.position}");

        }

        yield return null; // Let parenting apply

        // 5. Unfreeze physics now that transform hierarchy is stable
        foreach (Transform child in currentGO.transform)
        {
            var rb = child.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.linearDamping = 0f;             // This is the correct property, not linearDamping
                rb.angularDamping = 0.05f;
                //rb.useGravity = true;
                StartCoroutine(ApplyCustomGravity(rb, 60f, 0.05f)); // Apply for 60 seconds

            }
        }
        Debug.Log($"currentGO has {currentGO.transform.childCount} children.");

        yield return new WaitForSeconds(1.5f);

        animacionBTN.interactable = true;
        fisicasArtificialesApagables = true;
    }*/

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


    /*public void ReactivatePhysics()
    {
        if (currentGO == null) return;

        if (!fisicasArtificialesApagables) return;

        foreach (Transform child in currentGO.transform)
        {
            var rb = child.GetComponent<Rigidbody>();
            if (rb)
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

        Debug.Log("Physics reactivated for all child rigidbodies.");

        fisicasArtificialesApagables = false;
    }*/

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

        // Optionally switch to built-in gravity
        //rb.useGravity = true;
    }

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

    public void PostReset()
    {
        Transform found = transform.Find("Cubos catenaria(Clone)");

        if (found != null)
        {
            currentGO = found.gameObject;
            Debug.Log("PostReset: "+ found.name+" assigned to currentGO.");
            ActualizarCubos();
            Debug.Log("Se actualizaron los cubos, ahora se traran los listeners");
            // Add XRGrabInteractable listener to call ReactivatePhysics on grab
            foreach (Rigidbody rb in cubos)
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
            }
        }
        else
        {
            Debug.LogWarning("PostReset: 'Cubos catenaria' not found under this object.");
            currentGO = null;
            cubos.Clear();
        }
    }
    /*public void ResetCatenaria()
    {
        StopAllCoroutines();
        StartCoroutine(ResetCatenariaRoutine());
    }*/

    /*private IEnumerator ResetCatenariaRoutine()
    {
        Debug.LogWarning("ResetCatenariaRoutine: Starting reset...");

        if (animacionBTN != null)
            animacionBTN.interactable = true;

        fisicasArtificialesApagables = false;

        // Clean up detached cubes explicitly
        foreach (var rb in cubos)
        {
            if (rb != null)
                Destroy(rb.gameObject);
        }
        cubos.Clear();
        currentGO = null;

        // Reset the exhibition
        if (exhibicionScript != null)
            exhibicionScript.ResetExhibicion();

        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.1f);

        PostReset();
    }*/
    
    public void ResetCatenaria()
    {
        StopAllCoroutines();
        StartCoroutine(ResetCatenariaRoutine());
    }

    /*private IEnumerator ResetCatenariaRoutine()
    {
        Debug.LogWarning("ResetCatenariaRoutine: Starting reset...");

        // Step 0: Prep
        if (animacionBTN != null)
            animacionBTN.interactable = true;

        fisicasArtificialesApagables = false;

        // Step 1: Apply upward force
        foreach (var rb in cubos)
        {
            if (rb != null)
            {
                Vector3 randomDirection = Vector3.up * 1.0f; // Base upward force

                // Add slight randomness in X and Z directions
                float randomX = Random.Range(-0.5f, 0.5f);
                float randomZ = Random.Range(-0.5f, 0.5f);

                randomDirection += new Vector3(randomX, 0f, randomZ); // Combined vector
                randomDirection.Normalize(); // Optional: keep magnitude consistent

                float forceMagnitude = Random.Range(1f, 3f); // Playful variable strength
                rb.AddTorque(Random.onUnitSphere * Random.Range(0.5f, 2f), ForceMode.Impulse);

                rb.AddForce(randomDirection * forceMagnitude, ForceMode.Impulse);
            }
        }


        yield return new WaitForSeconds(0.5f); // Let them fly a bit

        // Step 2: Rotate respaldar (use hinge spring)
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

        // Step 3: Destroy old cubes (they're detached and flying now)
        foreach (var rb in cubos)
        {
            if (rb != null)
            {
                Destroy(rb.gameObject);
            }
        }
        cubos.Clear();
        currentGO = null;

        yield return new WaitForSeconds(0.1f); // Let Unity process Destroy()

        // Step 4: Reset all objects from exhibition
        if (exhibicionScript != null)
        {
            Debug.Log("Calling ResetExhibicion");
            exhibicionScript.ResetExhibicion();
        }

        // Step 5: Let new objects instantiate
        yield return new WaitForSeconds(0.2f); // Tune this if instantiation takes longer

        // Step 6: Finalize with PostReset
        PostReset();
    }*/

    private IEnumerator ResetCatenariaRoutine()
    {
        Debug.LogWarning("ResetCatenariaRoutine: Starting reset...");

        if (animacionBTN != null)
            animacionBTN.interactable = true;

        fisicasArtificialesApagables = false;

        // Make sure cubes are non-kinematic so they can fly
        foreach (var rb in cubos)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                // Apply playful force: up + random direction
                Vector3 playfulDirection = (Vector3.up * 5f) + new Vector3(
                    Random.Range(-1f, 1f),
                    0f,
                    Random.Range(-1f, 1f)
                ) * 2f;
                rb.AddForce(playfulDirection, ForceMode.Impulse);
            }
        }

        // Wait a short time to allow the cubes to "fly"
        yield return new WaitForSeconds(0.5f);

        // Animate the respaldar going down
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

        // Destroy old cubes
        foreach (var rb in cubos)
        {
            if (rb != null)
                Destroy(rb.gameObject);
        }
        cubos.Clear();
        currentGO = null;

        // Reset the exhibition (which reinstantiates new objects)
        if (exhibicionScript != null)
            exhibicionScript.ResetExhibicion();

        // Wait a couple frames + small delay to ensure stability
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.1f);

        PostReset();
    }

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

        // Wait until it gets close enough
        while (Mathf.Abs(hinge.angle - targetAngle) > 1f)
        {
            yield return null;
        }

        // Optional: Wait a short buffer to ensure it's settled
        yield return new WaitForSeconds(0.3f);

        // 🔧 Turn off spring to allow free movement
        hinge.useSpring = false;

        Debug.Log("Respaldar reached target and spring disabled.");
    }


}
