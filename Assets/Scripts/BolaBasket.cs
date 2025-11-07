using UnityEngine;
/* Este script se encarga de restaurar las propiedades físicas
 de una pelota de baloncesto en Unity.
 Ideal cuando la bola ha sido desactivada o su física pausada,
 y se desea que vuelva a comportarse naturalmente.*/

public class BolaBasket : MonoBehaviour
{

    // Método público que reactiva la física del Rigidbody asociado
    public void RecuperarFisicas()
    {
        // Obtiene el componente Rigidbody del mismo GameObject
        Rigidbody rb = this.GetComponent<Rigidbody>();
        // Solo aplica cambios si el Rigidbody existe
        if (rb != null)
        {
            // Reactiva la gravedad y el movimiento físico
            rb.useGravity = true;
            rb.isKinematic = false;
            
            // Restaura parámetros físicos típicos para una pelota ligera
            rb.mass = 0.2f;  // Masa ligera
            rb.linearDamping = 0.1f;   // Suaviza el movimiento (resistencia lineal)
            rb.angularDamping = 0.05f; // Suaviza la rotación (resistencia angular)
            
            // Mejora la estabilidad de la simulación
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
}
