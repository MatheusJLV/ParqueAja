using UnityEngine;

public class BolaBasket : MonoBehaviour
{

    public void RecuperarFisicas()
    {
        Rigidbody rb = this.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;

            rb.mass = 0.2f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
}
