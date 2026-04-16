using UnityEngine;
using System.Collections;
using C2M2.Interaction.VR;

namespace C2M2.Interaction
{
    using Utils;

    /// <summary>
    /// Activate raycast triggers using XR controller buttons and fingertip distance.
    /// Replaces the legacy OVRInput calls with XRInputBridge so no Oculus App ID is needed.
    /// </summary>
    public class OculusEventSignaler : RaycastEventSignaler
    {
        [Tooltip("Is this the left-hand controller? Determines which Input System bindings are used.")]
        public bool isLeftHand = false;

        [Tooltip("If Toggle Mode is enabled, pressing the primary button (A/X) will toggle raycasting mode on. " +
                 "Otherwise the button needs to be held.")]
        public bool toggleMode = true;

        public OVRGrabber grabber = null;

        [Tooltip("Line renderer for visually mimicking the raycast vector")]
        public LineRenderer lineRend;
        [Tooltip("Line renderer default color")]
        public Color unpressedColor = Color.cyan;
        [Tooltip("Line renderer color when holding a click")]
        public Color pressedColor = new Color(1f, 0.6f, 0f);

        private bool toggled = false;
        private bool Toggled
        {
            get
            {
                XRInputBridge xri = XRInputBridge.Instance;
                if (xri != null && xri.GetPrimaryButtonDown(isLeftHand))
                    toggled = !toggled;
                return toggled;
            }
        }

        protected override void OnAwake()
        {
            lineRend = gameObject.GetComponentInChildren<LineRenderer>();
            if (lineRend == null) Debug.LogWarning("Couldn't find LineRenderer in OculusEventSignaler");

            if (grabber == null)
                grabber = GetComponentInParent<OVRGrabber>();
        }

        protected override void OnStart()
        {
            lineRend.SetEndpointColors(unpressedColor);
            StartCoroutine(SearchForHand());
        }

        protected override bool RaycastRequested()
        {
            XRInputBridge xri = XRInputBridge.Instance;
            bool raycasting;
            if (xri != null)
                raycasting = toggleMode ? Toggled : xri.GetPrimaryButton(isLeftHand);
            else
                raycasting = false;

            // Suppress raycasting while an object is being grabbed
            if (grabber != null && grabber.grabbedObject != null)
                raycasting = false;

            StaticHandSetActive(raycasting);
            LineRendererSetActive(raycasting);
            return raycasting;
        }

        private bool distancePressed = false;

        protected override bool PressCondition()
        {
            XRInputBridge xri = XRInputBridge.Instance;
            return (xri != null && xri.GetTrigger(isLeftHand)) || distancePressed;
        }

        protected override void OnPressBegin()
        {
            lineRend.SetEndpointColors(pressedColor);
            base.OnPressBegin();
        }

        protected override void OnPressEnd()
        {
            lineRend.SetEndpointColors(unpressedColor);
            base.OnPressEnd();
        }

        protected override bool RaycastingMethod(out RaycastHit hit, float maxDistance, LayerMask layerMask)
        {
            Vector3 globalForward = transform.TransformDirection(Vector3.forward);
            bool raycastHit = Physics.Raycast(transform.position, globalForward, out hit, maxDistance, layerMask);

            if (raycastHit)
            {
                distancePressed = CheckPressDistance(hit);
                lineRend.SetEndpointPositions(transform.position, hit.point);
            }
            else
            {
                lineRend.SetEndpointPositions(Vector3.zero, Vector3.zero);
            }

            return raycastHit;
        }

        // ── Static / default hand toggle ─────────────────────────────────────

        [Tooltip("The MeshRenderer for the static pointed-hand model shown during raycasting")]
        public MeshRenderer staticHand;
        private GameObject defaultHand = null;

        private void StaticHandSetActive(bool active)
        {
            if (defaultHand != null && staticHand != null)
            {
                staticHand.enabled = active;
                defaultHand.SetActive(!active);
            }
        }

        private void LineRendererSetActive(bool active)
        {
            lineRend.enabled = active;
        }

        // ── Fingertip proximity press ─────────────────────────────────────────

        [Tooltip("Distance at which fingertip proximity triggers a press (metres)")]
        public float pressDistance = 0.01f;

        private bool CheckPressDistance(RaycastHit hit)
        {
            if (hit.distance < pressDistance) return true;
            if (distancePressed && hit.distance < pressDistance * 3f) return true;
            return false;
        }

        // ── Default-hand search ───────────────────────────────────────────────

        /// <summary>
        /// Waits for XRHandVisualizer to create the hand object, then caches the reference.
        /// Falls back gracefully if the object never appears (hand toggle is a non-critical feature).
        /// </summary>
        private IEnumerator SearchForHand()
        {
            string handName = isLeftHand ? "hand_left" : "hand_right";
            int maxFrames = 150;

            while (defaultHand == null && maxFrames-- > 0)
            {
                defaultHand = GameObject.Find(handName);
                yield return null;
            }

            if (defaultHand == null)
                Debug.LogWarning($"[OculusEventSignaler] '{handName}' not found – hand-toggle disabled.");
        }
    }
}
