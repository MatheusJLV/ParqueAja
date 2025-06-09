using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;

namespace Unity.XR.Custom
{
    [AddComponentMenu("XR/Locomotion/Custom Climb Provider", 11)]
    public class CustomClimbProvider : ClimbProvider
    {
        private readonly List<IXRSelectInteractor> grabbingInteractors = new();
        private readonly List<ClimbInteractable> grabbedClimbables = new();

        private Vector3 interactorAnchorWorldPos;
        private Vector3 interactorAnchorLocalPos;

        private float currentClimbMultiplier = 1f;

        public XROriginMovement transformation { get; set; } = new() { forceUnconstrained = true };

        protected override void Awake()
        {
            base.Awake();
        }

        public void StartClimbGrab(ClimbInteractable interactable, IXRSelectInteractor interactor)
        {
            if (interactable is ClimbInteractableWithMultiplier withMultiplier)
                currentClimbMultiplier = withMultiplier.climbForceMultiplier;
            else
                currentClimbMultiplier = 1f;

            var climbTransform = interactable.climbTransform;
            interactorAnchorWorldPos = interactor.transform.position;
            interactorAnchorLocalPos = climbTransform.InverseTransformPoint(interactorAnchorWorldPos);

            grabbingInteractors.Add(interactor);
            grabbedClimbables.Add(interactable);

            TryPrepareLocomotion();
        }

        public void FinishClimbGrab(IXRSelectInteractor interactor)
        {
            var index = grabbingInteractors.IndexOf(interactor);
            if (index < 0) return;

            grabbingInteractors.RemoveAt(index);
            grabbedClimbables.RemoveAt(index);

            if (grabbingInteractors.Count == 0)
                TryEndLocomotion();
            else
            {
                var lastInteractable = grabbedClimbables[^1];
                if (lastInteractable is ClimbInteractableWithMultiplier withMultiplier)
                    currentClimbMultiplier = withMultiplier.climbForceMultiplier;
                else
                    currentClimbMultiplier = 1f;

                var climbTransform = lastInteractable.climbTransform;
                interactorAnchorWorldPos = grabbingInteractors[^1].transform.position;
                interactorAnchorLocalPos = climbTransform.InverseTransformPoint(interactorAnchorWorldPos);
            }
        }

        protected virtual void Update()
        {
            if (!isLocomotionActive)
                return;

            if (grabbingInteractors.Count == 0)
            {
                TryEndLocomotion();
                return;
            }

            if (locomotionState == LocomotionState.Preparing)
                TryStartLocomotionImmediately();

            var interactor = grabbingInteractors[^1];
            var interactable = grabbedClimbables[^1];

            if (interactor == null || interactable == null)
            {
                TryEndLocomotion();
                return;
            }

            StepClimbMovement(interactable, interactor);
        }

        private void StepClimbMovement(ClimbInteractable interactable, IXRSelectInteractor interactor)
        {
            var settings = interactable.climbSettingsOverride?.Value ?? climbSettings?.Value;
            if (settings == null)
                return;

            var climbTransform = interactable.climbTransform;
            var currentInteractorPos = interactor.transform.position;

            Vector3 movement;

            if (settings.allowFreeXMovement && settings.allowFreeYMovement && settings.allowFreeZMovement)
            {
                movement = interactorAnchorWorldPos - currentInteractorPos;
            }
            else
            {
                var localCurrentPos = climbTransform.InverseTransformPoint(currentInteractorPos);
                var delta = interactorAnchorLocalPos - localCurrentPos;

                if (!settings.allowFreeXMovement) delta.x = 0f;
                if (!settings.allowFreeYMovement) delta.y = 0f;
                if (!settings.allowFreeZMovement) delta.z = 0f;

                movement = climbTransform.TransformVector(delta);
            }

            movement *= currentClimbMultiplier;
            transformation.motion = movement;

            TryQueueTransformation(transformation);
        }
    }
}
