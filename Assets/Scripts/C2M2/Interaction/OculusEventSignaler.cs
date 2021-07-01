using UnityEngine;
using System.Collections;

namespace C2M2.Interaction
{
    using Utils;

    /// <summary>
    /// Activate raycast triggers using oculus controller buttons and fingertip distance
    /// </summary>
    public class OculusEventSignaler : RaycastEventSignaler
    {
        [Tooltip("The Oculus controller being raycasted from")]
        public OVRInput.Controller controller = OVRInput.Controller.RTouch;
        [Tooltip("Button to activate raycasting mode")]
        public OVRInput.Button beginRaycastingButton = OVRInput.Button.One;
        [Tooltip("If Toggle Mode is enabled, pressing Begin Raycasting Button will toggle raycasting mode on. Otherwise Begin Raycasting Button needs to be held down to enter raycasting mode.")]
        public bool toggleMode = true;
        [Tooltip("Button to invoke hit/hold events from a distance")]
        public OVRInput.Button triggerEventsButton = OVRInput.Button.PrimaryIndexTrigger;
        public OVRGrabber grabber = null;
        [Tooltip("Line renderer for visually mimicking raycast vector")]
        public LineRenderer lineRend;
        [Tooltip("Line renderer default color")]
        public Color unpressedColor = Color.cyan;
        [Tooltip("Line renderer color when holding a click")]
        public Color pressedColor = new Color(1f, 0.6f, 0f);

        public Transform localAvatar;
        public bool isLeftHand = false;

        private bool toggled = false;
        private bool Toggled
        {
            get
            {
                // If the raycasting button was pressed for the first time this frame, enable/disable raycasting
                if (OVRInput.GetDown(beginRaycastingButton, controller))
                {
                    toggled = !toggled;
                }
                return toggled;
            }
        }

        protected override void OnAwake()
        {
            lineRend = gameObject.GetComponentInChildren<LineRenderer>();
            if (lineRend == null) { Debug.LogWarning("Couldn't find line renderer in RaycastForward"); }

            if(grabber == null)
            {
                grabber = GetComponentInParent<OVRGrabber>();
            }
        }
        protected override void OnStart()
        {
            // WARNING: Don't call this method in Awake( )
            lineRend.SetEndpointColors(unpressedColor);

            StartCoroutine(SearchForHand(100));
        }
        protected override bool RaycastRequested()
        {
            // If we are in toggle mode, is raycasting mode toggled on?
            // Otherwise, is the Begin Raycasting Button currently being pressed down?
            bool rURaycasting = toggleMode ? Toggled : OVRInput.Get(beginRaycastingButton, controller);

            // If an object is being actively grabbed, don't raycast
            if (grabber != null && grabber.grabbedObject != null)
                rURaycasting = false;

            StaticHandSetActive(rURaycasting);
            LineRendererSetActive(rURaycasting);

            return rURaycasting;
        }
        private bool distancePressed = false;
        /// <returns> True if the specified controller button is pressed OR if we are near enough to the raycast target </returns>
        protected override bool PressCondition() => (OVRInput.Get(triggerEventsButton, controller) || distancePressed);

        // At the start of a click change the line renderer color to pressed color
        protected override void OnPressBegin()
        {
            lineRend.SetEndpointColors(pressedColor);
            base.OnPressBegin();
        }

        // After a press return the line renderer color to default
        protected override void OnPressEnd()
        {
            lineRend.SetEndpointColors(unpressedColor);
            base.OnPressEnd();
        }

        /// <summary> Raycast using fingertip position/direction, handle fingertip line renderer </summary>
        protected override bool RaycastingMethod(out RaycastHit hit, float maxDistance, LayerMask layerMask)
        {
            // Turn the local forward into global forward to find the raycast direction
            Vector3 globalForward = transform.TransformDirection(Vector3.forward);
            // Try to raycast onto a valid target in the global forward direction
            bool raycastHit = Physics.Raycast(transform.position, globalForward, out hit, maxDistance, layerMask);

            if (raycastHit)
            {
                distancePressed = CheckPressDistance(hit);
                // Draw linerenderer to hit object
                lineRend.SetEndpointPositions(transform.position, hit.point);
            }
            else lineRend.SetEndpointPositions(Vector3.zero, Vector3.zero);

            return raycastHit;
        }

        [Tooltip("The renderer component for the static pointed hand ")]
        public MeshRenderer staticHand;
        [Tooltip("This is the default Oculus hand object, usually hand_right or hand_left")]
        private UnityEngine.GameObject defaultHand = null;
        /// <summary>
        /// Enable/disable static raycasting hand model and default hand
        /// </summary>
        /// <param name="active"> True to enable static hand, false to enable regular OVR hand</param>
        private void StaticHandSetActive(bool active)
        {
            if (defaultHand != null && staticHand != null)
            {
                staticHand.enabled = active;
                defaultHand.SetActive(!active);
            }
        }
        /// <summary>
        /// Enable/Disable line renderer depending on if raycasting mode is enabled
        /// </summary>
        /// <param name="active"> True to enable line renderer, false to disable </param>
        private void LineRendererSetActive(bool active)
        {
            lineRend.enabled = active;
        }
        [Tooltip("Minimum distance to trigger raycast triggers")]
        public float pressDistance = 0.01f;
        /// <summary>
        /// Figure out if we should trigger a press based on distance
        /// </summary>
        /// <returns>
        /// True if we have instigated a click event by coming within click distance,
        /// True if we are holding a press by remaining within hysteresis distance (3 * click distance) after clicking,
        /// False otherwise
        /// </returns>
        private bool CheckPressDistance(RaycastHit hit)
        {
            // If we are close enough to press, 
            // or we have already pressed and we are close enough to hold a press
            if((hit.distance < pressDistance) || 
                (distancePressed && hit.distance < (pressDistance * 3))) return true;

            // We either haven't clicked yet, or we're too far. Don't trigger a hold
            else return false;
        }

        private IEnumerator SearchForHand(int waitFrames)
        {
            int maxFrames = 100;
            string handName = isLeftHand ? "hand_left" : "hand_right";

            while(defaultHand == null)
            {
                defaultHand = GameObject.Find(handName);
                maxFrames--;
                if (maxFrames == 0) break;
                yield return null;
            }

            if (defaultHand == null) Debug.LogError("No hand found!");
        }
    }
}