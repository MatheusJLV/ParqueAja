using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.Interaction.Toolkit;

namespace Unity.XR.Custom
{
     /* Extiende ClimbInteractable para permitir un multiplicador
    de fuerza al escalar objetos en VR.*/
    [SelectionBase] // Permite seleccionar el objeto fácilmente en la escena
    [DisallowMultipleComponent]  // Evita agregar múltiples componentes iguales
    [RequireComponent(typeof(Rigidbody))]  // Asegura que haya un Rigidbody
    [AddComponentMenu("XR/Climb Interactable With Multiplier", 11)]
    [MovedFrom("UnityEngine.XR.Interaction.Toolkit")]
    public class ClimbInteractableWithMultiplier : ClimbInteractable
    {
        [Header("Climb Force Multiplier")]
        [Tooltip("Multiplies the climbing force applied when this object is grabbed.")]
        public float climbForceMultiplier = 1f;
        private CustomClimbProvider climbProvider; // Referencia al proveedor de escalada personalizado
        // Awake: llamado al inicializar el componente
        protected override void Awake()
        {
            base.Awake();
            TryFindClimbProvider();// Intenta encontrar un proveedor de escalada en la escena
        }

        // OnSelectEntered: llamado cuando el objeto es agarrado
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            // Si no hay proveedor, lo buscamos nuevamente
            if (climbProvider == null)
                TryFindClimbProvider();
            // Inicia la interacción de escalada personalizada
            climbProvider?.StartClimbGrab(this, args.interactorObject);
        }

        // OnSelectExited: llamado cuando el objeto deja de ser agarrado
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            // Finaliza la interacción de escalada
            climbProvider?.FinishClimbGrab(args.interactorObject);
        }
        
        // Intenta encontrar un proveedor de escalada en la escena
        private void TryFindClimbProvider()
        {
            // Find a ClimbProvider in the scene
            climbProvider = FindAnyObjectByType<CustomClimbProvider>();
        }
    }
}
