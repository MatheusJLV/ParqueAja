using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Unity.VRTemplate
{
    // Sistema de volante de dirección interactivo para VR que permite agarrar y rotar un volante usando XR Interaction Toolkit.
    // Soporta agarres con ambas manos y calcula el ángulo de rotación basado en la posición de los interactores.
    public class SteeringWheel : XRBaseInteractable
    {
        // Evento personalizado que se invoca cuando el ángulo del volante cambia
        [Serializable]
        public class AngleChangeEvent : UnityEvent<float> { }

        [SerializeField]
        Transform m_Handle = null;           // Transform del volante que se rota

        [SerializeField]
        AngleChangeEvent m_OnAngleChange = new AngleChangeEvent();  // Evento que se invoca cuando cambia el ángulo

        [SerializeField]
        float m_MinAngle = -90f;             // Ángulo mínimo de rotación del volante (grados)

        [SerializeField]
        float m_MaxAngle = 90f;              // Ángulo máximo de rotación del volante (grados)

        [SerializeField]
        Transform m_LeftHandle;              // Transform del agarre izquierdo del volante

        [SerializeField]
        Transform m_RightHandle;             // Transform del agarre derecho del volante             // Transform del agarre derecho del volante

        // Propiedad para acceder o establecer el transform del volante
        public Transform handle
        {
            get => m_Handle;
            set => m_Handle = value;
        }

        // Propiedad para acceder al evento de cambio de ángulo
        public AngleChangeEvent onAngleChange => m_OnAngleChange;

        private readonly Dictionary<IXRSelectInteractor, Transform> m_InteractorToHandle = new();  // Mapea cada interactor a su handle correspondiente
        private float m_CurrentAngle = 0.0f;     // Ángulo actual del volante
        private float m_BaseAngle = 0.0f;        // Ángulo base usado para calcular el delta de rotación        // Ángulo base usado para calcular el delta de rotación

        // Suscribe los listeners de eventos cuando el componente se habilita
        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(OnGrab);
            selectExited.AddListener(OnRelease);
        }

        // Desuscribe los listeners de eventos cuando el componente se deshabilita
        protected override void OnDisable()
        {
            selectEntered.RemoveListener(OnGrab);
            selectExited.RemoveListener(OnRelease);
            base.OnDisable();
        }

        // Maneja el evento cuando un interactor agarra el volante, determinando qué handle fue agarrado
        void OnGrab(SelectEnterEventArgs args)
        {
            var interactor = args.interactorObject;
            var attach = interactor.GetAttachTransform(this);

            if (!m_InteractorToHandle.ContainsKey(interactor))
            {
                if (attach == m_LeftHandle || attach.IsChildOf(m_LeftHandle))
                    m_InteractorToHandle.Add(interactor, m_LeftHandle);
                else if (attach == m_RightHandle || attach.IsChildOf(m_RightHandle))
                    m_InteractorToHandle.Add(interactor, m_RightHandle);
            }

            UpdateBaseAngle();
        }

        // Maneja el evento cuando un interactor suelta el volante, removiendo el interactor del diccionario
        void OnRelease(SelectExitEventArgs args)
        {
            if (m_InteractorToHandle.ContainsKey(args.interactorObject))
                m_InteractorToHandle.Remove(args.interactorObject);
        }

        // Procesa la actualización del interactable, llamando a UpdateRotation durante la fase dinámica
        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && m_InteractorToHandle.Count > 0)
                UpdateRotation();
        }

        // Calcula y aplica la rotación del volante basada en las posiciones promedio de los interactors activos
        void UpdateRotation()
        {
            if (m_Handle == null) return;

            Vector3 avgDirection = Vector3.zero;
            foreach (var pair in m_InteractorToHandle)
            {
                var interactorTransform = pair.Key.GetAttachTransform(this);
                Vector3 localOffset = transform.InverseTransformVector(interactorTransform.position - m_Handle.position);
                localOffset.y = 0.0f;
                avgDirection += localOffset.normalized;
            }

            avgDirection.Normalize();
            float angle = Mathf.Atan2(avgDirection.z, avgDirection.x) * Mathf.Rad2Deg;
            float deltaAngle = Mathf.DeltaAngle(m_BaseAngle, angle);

            m_CurrentAngle += deltaAngle;
            m_CurrentAngle = Mathf.Clamp(m_CurrentAngle, m_MinAngle, m_MaxAngle);
            m_Handle.localEulerAngles = new Vector3(0.0f, m_CurrentAngle, 0.0f);

            m_BaseAngle = angle;
            m_OnAngleChange.Invoke(m_CurrentAngle);
        }

        // Actualiza el ángulo base calculando el promedio de las posiciones de los interactors
        void UpdateBaseAngle()
        {
            if (m_InteractorToHandle.Count == 0) return;

            Vector3 avgDirection = Vector3.zero;
            foreach (var pair in m_InteractorToHandle)
            {
                var interactorTransform = pair.Key.GetAttachTransform(this);
                Vector3 localOffset = transform.InverseTransformVector(interactorTransform.position - m_Handle.position);
                localOffset.y = 0.0f;
                avgDirection += localOffset.normalized;
            }

            avgDirection.Normalize();
            m_BaseAngle = Mathf.Atan2(avgDirection.z, avgDirection.x) * Mathf.Rad2Deg;
        }

        // Propiedad de solo lectura que devuelve el ángulo actual del volante
        public float currentAngle => m_CurrentAngle;
    }
}
