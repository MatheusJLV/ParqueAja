using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CatenariaBaseController : MonoBehaviour
{
    private Rigidbody rb;
    private HingeJoint hingeJoint;
    private bool isControlledByScript = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private float animationDuration = 1f; // Duration of the animation in seconds

    private Rigidbody childRb;
    private XRGrabInteractable childGrabInteractable;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hingeJoint = GetComponent<HingeJoint>();
        initialRotation = transform.rotation;
        targetRotation = Quaternion.Euler(90, 0, 0) * initialRotation;

        // Get the child object's Rigidbody and XRGrabInteractable components
        Transform childTransform = transform.GetChild(0);
        childRb = childTransform.GetComponent<Rigidbody>();
        childGrabInteractable = childTransform.GetComponent<XRGrabInteractable>();
    }

    // Method to take control of the physics
    public void TakeControl()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            isControlledByScript = true;
        }

        if (childRb != null)
        {
            childRb.isKinematic = true;
        }

        if (childGrabInteractable != null)
        {
            childGrabInteractable.enabled = false;
        }
    }

    // Method to revert control of the physics
    public void RevertControl()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            isControlledByScript = false;
        }

        if (childRb != null)
        {
            childRb.isKinematic = false;
        }

        if (childGrabInteractable != null)
        {
            childGrabInteractable.enabled = true;
        }
    }

    // Method to animate the object by rotating it 90 degrees upwards
    public void AnimateRotationUpwards()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateRotation(targetRotation));
    }

    // Method to revert the animation
    public void RevertAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateRotation(initialRotation));
    }

    // Coroutine to handle the rotation animation
    private IEnumerator AnimateRotation(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRotation;
    }
}
