using UnityEngine;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Extends OVRGrabber with a public grab-volume interface and replaces the broken
    /// OVRInput.Axis1D grip detection with XRInputBridge (Unity Input System) so that
    /// grip grabbing works with the OpenXR backend (Meta Quest Link, no App ID required).
    /// </summary>
    public class PublicOVRGrabber : OVRGrabber
    {
        public Collider[] M_GrabVolumes { get { return m_grabVolumes; } set { m_grabVolumes = value; } }
        public OVRInput.Controller Controller { get { return m_controller; } }

        private bool IsLeftHand => m_controller == OVRInput.Controller.LTouch;

        private OVRCameraRig cachedRig = null;

        protected override void Awake()
        {
            // Replicate what base.Awake() does (store anchor offsets, mark whether we have a rig),
            // but do NOT register OVRGrabber's broken OVRInput handler on UpdatedAnchors.
            m_anchorOffsetPosition = transform.localPosition;
            m_anchorOffsetRotation = transform.localRotation;

            if (!m_moveHandPosition)
            {
                cachedRig = transform.GetComponentInParent<OVRCameraRig>();
                if (cachedRig != null)
                {
                    cachedRig.UpdatedAnchors += OnXRIUpdatedAnchors;
                    m_operatingWithoutOVRCameraRig = false;
                }
            }
            // NOTE: base.Awake() intentionally NOT called — it would register
            // OVRGrabber's OVRInput.Axis1D.PrimaryHandTrigger handler which is
            // unreliable with the OpenXR backend on Meta Quest Link.
        }

        protected virtual void OnDestroy()
        {
            if (m_grabbedObj != null)
                GrabEnd();
            if (cachedRig != null)
                cachedRig.UpdatedAnchors -= OnXRIUpdatedAnchors;
        }

        private void OnXRIUpdatedAnchors(OVRCameraRig rig)
        {
            if (m_parentTransform == null) return; // Start() not yet called

            Vector3 destPos = m_parentTransform.TransformPoint(m_anchorOffsetPosition);
            Quaternion destRot = m_parentTransform.rotation * m_anchorOffsetRotation;

            if (m_moveHandPosition)
            {
                var rb = GetComponent<Rigidbody>();
                rb.MovePosition(destPos);
                rb.MoveRotation(destRot);
            }

            if (!m_parentHeldObject)
                MoveGrabbedObject(destPos, destRot);

            m_lastPos = transform.position;
            m_lastRot = transform.rotation;

            float prevFlex = m_prevFlex;
            XRInputBridge xri = XRInputBridge.Instance;
            m_prevFlex = (xri != null && xri.GetGrip(IsLeftHand)) ? 1f : 0f;

            CheckForGrabOrRelease(prevFlex);
        }
    }
}
