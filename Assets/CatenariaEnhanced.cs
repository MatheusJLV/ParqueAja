using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

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
        animacionBTN.interactable = false; // Disable button to prevent multiple clicks
        if (currentGO != null)
            StartCoroutine(DropSequenceRoutine(currentGO));
    }

    private IEnumerator DropSequenceRoutine(GameObject currentGO)
    {
        originalPosition = currentGO.transform.position;
        originalRotation = currentGO.transform.rotation;

        if (currentGO == null)
            yield break;

        this.currentGO = currentGO; // Ensure reference is consistent inside resetPosition()

        // 1. Cache child rigidbodies
        Rigidbody[] pieceRBs = currentGO.GetComponentsInChildren<Rigidbody>();

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
            rb.AddForce(Physics.gravity * gravityForceModifier, ForceMode.Acceleration); 
            rb.linearDamping = drag;            
            rb.angularDamping = angularDrag;

            // Note: DO NOT set Physics.gravity globally here — it affects all objects.
            // Instead simulate gravity scaling with custom forces if needed.
            // Example: rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
        }

        // 9. Reset position (delayed)
        yield return ResetPositionRoutine();
    }

    private IEnumerator ResetPositionRoutine()
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
                StartCoroutine(ApplyCustomGravity(rb, 5f, 0.05f)); // Apply for 5 seconds

            }
        }
        Debug.Log($"currentGO has {currentGO.transform.childCount} children.");

        yield return new WaitForSeconds(1.5f);

        animacionBTN.interactable = true;
    }

    IEnumerator ApplyCustomGravity(Rigidbody rb, float duration, float intensity = 1f)
    {
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


}
