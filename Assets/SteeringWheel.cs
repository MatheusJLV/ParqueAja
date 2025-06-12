using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Unity.VRTemplate
{
    public class SteeringWheel : XRBaseInteractable
    {
        [Serializable]
        public class AngleChangeEvent : UnityEvent<float> { }

        [SerializeField]
        Transform m_Handle = null;

        [SerializeField]
        AngleChangeEvent m_OnAngleChange = new AngleChangeEvent();

        [SerializeField]
        float m_MinAngle = -90f;

        [SerializeField]
        float m_MaxAngle = 90f;

        [SerializeField]
        Transform m_LeftHandle;

        [SerializeField]
        Transform m_RightHandle;

        public Transform handle
        {
            get => m_Handle;
            set => m_Handle = value;
        }

        public AngleChangeEvent onAngleChange => m_OnAngleChange;

        private readonly Dictionary<IXRSelectInteractor, Transform> m_InteractorToHandle = new();
        private float m_CurrentAngle = 0.0f;
        private float m_BaseAngle = 0.0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(OnGrab);
            selectExited.AddListener(OnRelease);
        }

        protected override void OnDisable()
        {
            selectEntered.RemoveListener(OnGrab);
            selectExited.RemoveListener(OnRelease);
            base.OnDisable();
        }

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

        void OnRelease(SelectExitEventArgs args)
        {
            if (m_InteractorToHandle.ContainsKey(args.interactorObject))
                m_InteractorToHandle.Remove(args.interactorObject);
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && m_InteractorToHandle.Count > 0)
                UpdateRotation();
        }

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

        public float currentAngle => m_CurrentAngle;
    }
}
