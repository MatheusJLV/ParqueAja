using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace Unity.XR.Custom
{
    [AddComponentMenu("XR/Locomotion/Custom Climb Provider", 11)]
    // Proveedor de escalada personalizado: gestiona agarres y movimiento.
    public class CustomClimbProvider : ClimbProvider
    {
        // Interactores agarrando (último activo)
        private readonly List<IXRSelectInteractor> grabbingInteractors = new();
        // Interactables agarrados (paralelo)
        private readonly List<ClimbInteractable> grabbedClimbables = new();

        // Ancla en mundo
        private Vector3 interactorAnchorWorldPos;
        // Ancla en local (climbTransform)
        private Vector3 interactorAnchorLocalPos;

        // Multiplicador de escalada
        private float currentClimbMultiplier = 1f;

        // Movimiento a aplicar al origen
        public XROriginMovement transformation { get; set; } = new() { forceUnconstrained = true };

        // Awake base
        protected override void Awake()
        {
            base.Awake();
        }

        // Inicia agarre
        public void StartClimbGrab(ClimbInteractable interactable, IXRSelectInteractor interactor)
        {
            Debug.Log("StartClimbGrab");
            // Ajusta multiplicador si el interactable lo define
            if (interactable is ClimbInteractableWithMultiplier withMultiplier)
                currentClimbMultiplier = withMultiplier.climbForceMultiplier;
            else
                currentClimbMultiplier = 1f;

            // Captura anclas
            var climbTransform = interactable.climbTransform;
            interactorAnchorWorldPos = interactor.transform.position;
            interactorAnchorLocalPos = climbTransform.InverseTransformPoint(interactorAnchorWorldPos);

            // Guarda referencias
            grabbingInteractors.Add(interactor);
            grabbedClimbables.Add(interactable);

            // Prepara la locomoción
            TryPrepareLocomotion();
        }

        // Finaliza agarre
        public void FinishClimbGrab(IXRSelectInteractor interactor)
        {
            Debug.Log("FinishClimbGrab");
            // Localiza índice
            var index = grabbingInteractors.IndexOf(interactor);
            if (index < 0) return;

            // Elimina referencias
            grabbingInteractors.RemoveAt(index);
            grabbedClimbables.RemoveAt(index);

            // Si no quedan agarres, termina locomoción
            if (grabbingInteractors.Count == 0)
            {
                base.FinishLocomotion();
                //TryEndLocomotion();
            }            
            else
            {
                // Actualiza multiplicador y anclas al último agarre
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

        // Update breve
        protected virtual void Update()
        {

            // Si locomoción inactiva => salir
            if (!isLocomotionActive)
                return;

            // Sin agarres -> terminar
            if (grabbingInteractors.Count == 0)
            {
                base.FinishLocomotion();
                //TryEndLocomotion();
                return;
            }

            if (locomotionState == LocomotionState.Preparing)
                TryStartLocomotionImmediately();

            var interactor = grabbingInteractors[^1];
            var interactable = grabbedClimbables[^1];

            // Verifica referencias
            if (interactor == null || interactable == null)
            {
                base.FinishLocomotion();
                //TryEndLocomotion();
                return;
            }

            // Realiza paso de movimiento
            StepClimbMovement(interactable, interactor);
        }

        // Calcula movimiento desde el ancla y encola la transformación al sistema
        private void StepClimbMovement(ClimbInteractable interactable, IXRSelectInteractor interactor)
        {
            Debug.Log(grabbingInteractors.Count);
            // Lee settings del interactable o usa los globales
            var settings = interactable.climbSettingsOverride?.Value ?? climbSettings?.Value;
            if (settings == null)
                return;

            var climbTransform = interactable.climbTransform;
            // Posición actual del interactor
            var currentInteractorPos = interactor.transform.position;

            Vector3 movement;

            // Si libre en X/Y/Z: diferencia en mundo
            if (settings.allowFreeXMovement && settings.allowFreeYMovement && settings.allowFreeZMovement)
            {
                movement = interactorAnchorWorldPos - currentInteractorPos;
            }
            else
            {
                // Si hay restricciones, calcula delta en local y aplica ejes permitidos
                var localCurrentPos = climbTransform.InverseTransformPoint(currentInteractorPos);
                var delta = interactorAnchorLocalPos - localCurrentPos;

                if (!settings.allowFreeXMovement) delta.x = 0f;
                if (!settings.allowFreeYMovement) delta.y = 0f;
                if (!settings.allowFreeZMovement) delta.z = 0f;

                // Transforma delta local a mundo
                movement = climbTransform.TransformVector(delta);
            }

            // Aplica factor del interactable
            movement *= currentClimbMultiplier;
            transformation.motion = movement;

            // Encola el movimiento para aplicarse al origen XR
            TryQueueTransformation(transformation);
        }
    }
}
