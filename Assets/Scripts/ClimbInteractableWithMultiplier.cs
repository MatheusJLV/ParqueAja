using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.Interaction.Toolkit;

namespace Unity.XR.Custom
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("XR/Climb Interactable With Multiplier", 11)]
    [MovedFrom("UnityEngine.XR.Interaction.Toolkit")]
    public class ClimbInteractableWithMultiplier : ClimbInteractable
    {
        [Header("Climb Force Multiplier")]
        [Tooltip("Multiplies the climbing force applied when this object is grabbed.")]
        public float climbForceMultiplier = 1f;

        private CustomClimbProvider climbProvider;

        protected override void Awake()
        {
            base.Awake();
            TryFindClimbProvider();
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (climbProvider == null)
                TryFindClimbProvider();

            climbProvider?.StartClimbGrab(this, args.interactorObject);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            climbProvider?.FinishClimbGrab(args.interactorObject);
        }

        private void TryFindClimbProvider()
        {
            // Find a ClimbProvider in the scene
            climbProvider = FindAnyObjectByType<CustomClimbProvider>();
        }
    }
}
