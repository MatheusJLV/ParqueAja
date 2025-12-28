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
    /*
     Controla la lógica de escalada personalizada en entornos XR.
     Gestiona interactores que agarran objetos escalables, aplicando
     multiplicadores de fuerza y calculando movimientos basados en
     posiciones relativas y configuraciones de escalada.
    */

    [AddComponentMenu("XR/Locomotion/Custom Climb Provider", 11)]
    public class CustomClimbProvider : ClimbProvider
    {
        // Lista de interactores que están agarrando objetos escalables
        private readonly List<IXRSelectInteractor> grabbingInteractors = new();

        // Lista de objetos escalables que están siendo agarrados
        private readonly List<ClimbInteractable> grabbedClimbables = new();

        // Posición ancla del interactor en el mundo
        private Vector3 interactorAnchorWorldPos;

        // Posición ancla del interactor en coordenadas locales del objeto escalable
        private Vector3 interactorAnchorLocalPos;

        // Multiplicador actual de fuerza de escalada
        private float currentClimbMultiplier = 1f;

        // Transformación de movimiento para la locomoción
        public XROriginMovement transformation { get; set; } = new() { forceUnconstrained = true };

        protected override void Awake()
        {
            base.Awake();
        }

        // Inicia el proceso de agarre para escalada
        public void StartClimbGrab(ClimbInteractable interactable, IXRSelectInteractor interactor)
        {
            Debug.Log("StartClimbGrab");

            // Verifica si el interactable tiene un multiplicador de fuerza personalizado
            if (interactable is ClimbInteractableWithMultiplier withMultiplier)
                currentClimbMultiplier = withMultiplier.climbForceMultiplier;
            else
                currentClimbMultiplier = 1f;

            // Obtiene la transformación del objeto escalable
            var climbTransform = interactable.climbTransform;

            // Guarda la posición actual del interactor como ancla en el mundo
            interactorAnchorWorldPos = interactor.transform.position;

            // Convierte la posición ancla a coordenadas locales del objeto escalable
            interactorAnchorLocalPos = climbTransform.InverseTransformPoint(interactorAnchorWorldPos);

            // Agrega el interactor y el interactable a las listas de agarre
            grabbingInteractors.Add(interactor);
            grabbedClimbables.Add(interactable);

            // Prepara la locomoción para iniciar
            TryPrepareLocomotion();
        }

        // Finaliza el proceso de agarre para escalada
        public void FinishClimbGrab(IXRSelectInteractor interactor)
        {
            Debug.Log("FinishClimbGrab");

            // Encuentra el índice del interactor en la lista
            var index = grabbingInteractors.IndexOf(interactor);
            if (index < 0) return;

            // Remueve el interactor y el interactable de las listas
            grabbingInteractors.RemoveAt(index);
            grabbedClimbables.RemoveAt(index);

            // Si no quedan interactores agarrando, finaliza la locomoción
            if (grabbingInteractors.Count == 0)
            {
                base.FinishLocomotion();
                //TryEndLocomotion();
            }            
            else
            {
                // Si quedan interactores, actualiza el multiplicador con el último interactable
                var lastInteractable = grabbedClimbables[^1];
                if (lastInteractable is ClimbInteractableWithMultiplier withMultiplier)
                    currentClimbMultiplier = withMultiplier.climbForceMultiplier;
                else
                    currentClimbMultiplier = 1f;

                // Actualiza las posiciones ancla con el último interactor
                var climbTransform = lastInteractable.climbTransform;
                interactorAnchorWorldPos = grabbingInteractors[^1].transform.position;
                interactorAnchorLocalPos = climbTransform.InverseTransformPoint(interactorAnchorWorldPos);
            }
        }

        // Actualiza la lógica de escalada en cada frame
        protected virtual void Update()
        {
            // Si la locomoción no está activa, no hace nada
            if (!isLocomotionActive)
                return;

            // Si no hay interactores agarrando, finaliza la locomoción
            if (grabbingInteractors.Count == 0)
            {
                base.FinishLocomotion();
                //TryEndLocomotion();
                return;
            }

            // Si está en estado de preparación, inicia la locomoción inmediatamente
            if (locomotionState == LocomotionState.Preparing)
                TryStartLocomotionImmediately();

            // Obtiene el último interactor y interactable
            var interactor = grabbingInteractors[^1];
            var interactable = grabbedClimbables[^1];

            // Si alguno es nulo, finaliza la locomoción
            if (interactor == null || interactable == null)
            {
                base.FinishLocomotion();
                //TryEndLocomotion();
                return;
            }

            // Calcula el movimiento de escalada
            StepClimbMovement(interactable, interactor);
        }

        // Calcula y aplica el movimiento de escalada basado en la posición del interactor
        private void StepClimbMovement(ClimbInteractable interactable, IXRSelectInteractor interactor)
        {
            Debug.Log(grabbingInteractors.Count);

            // Obtiene las configuraciones de escalada, usando override si existe
            var settings = interactable.climbSettingsOverride?.Value ?? climbSettings?.Value;
            if (settings == null)
                return;

            // Obtiene la transformación del objeto escalable
            var climbTransform = interactable.climbTransform;

            // Posición actual del interactor
            var currentInteractorPos = interactor.transform.position;

            Vector3 movement;

            // Si permite movimiento libre en todas las direcciones, calcula movimiento directo
            if (settings.allowFreeXMovement && settings.allowFreeYMovement && settings.allowFreeZMovement)
            {
                movement = interactorAnchorWorldPos - currentInteractorPos;
            }
            else
            {
                // Calcula el movimiento en coordenadas locales
                var localCurrentPos = climbTransform.InverseTransformPoint(currentInteractorPos);
                var delta = interactorAnchorLocalPos - localCurrentPos;

                // Restringe movimientos según las configuraciones
                if (!settings.allowFreeXMovement) delta.x = 0f;
                if (!settings.allowFreeYMovement) delta.y = 0f;
                if (!settings.allowFreeZMovement) delta.z = 0f;

                // Convierte el delta de vuelta a coordenadas del mundo
                movement = climbTransform.TransformVector(delta);
            }

            // Aplica el multiplicador de fuerza al movimiento
            movement *= currentClimbMultiplier;

            // Asigna el movimiento a la transformación
            transformation.motion = movement;

            // Intenta aplicar la transformación
            TryQueueTransformation(transformation);
        }
    }
}
