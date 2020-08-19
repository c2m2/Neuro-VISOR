using UnityEngine;
using System.Collections;

namespace C2M2.Interaction.Signaling
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
        protected override bool BeginRaycastingCondition()
        { 
            bool rURaycasting = OVRInput.Get(beginRaycastingButton, controller);

            if (grabber != null && grabber.grabbedObject != null)
                rURaycasting = false;

            StaticHandSetActive(rURaycasting);
            LineRendererSetActive(rURaycasting);
            return rURaycasting;
        }
        private bool distancePressed = false;
        /// <returns> True if the specified controller button is pressed OR if we are near enough to the raycast target </returns>
        protected override bool ChildsPressCondition() => (OVRInput.Get(triggerEventsButton, controller) || distancePressed);
        // At the start of a click change the line renderer color to pressed color
        protected override void OnPressSub() => lineRend.SetEndpointColors(pressedColor);
        // We don't need any functionaliy in the hold case
        protected override void OnHoldPressSub() { }
        // After a click return the line renderer color to default
        protected override void OnEndPressSub() => lineRend.SetEndpointColors(unpressedColor);
        /// <summary> Raycast using fingertip position/direction, handle fingertip line renderer </summary>
        protected override bool RaycastingMethod(out RaycastHit hit, float maxDistance, LayerMask layerMask)
        {
            // Turn the local forward into global forward to find the raycast direction
            Vector3 globalForward = transform.TransformDirection(Vector3.forward);
            // Try to raycast onto a valid target in the global forward direction
            bool didHit = Physics.Raycast(transform.position, globalForward, out hit, maxDistance, layerMask);
            if (didHit)
            { // If we hit a valid target, render the linerender there. Othewise don't render it
                distancePressed = CheckPressDistance(hit);
                lineRend.SetEndpointPositions(transform.position, hit.point);
            }
            else lineRend.SetEndpointPositions(Vector3.zero, Vector3.zero);

            return didHit;
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
        public float clickDistance = 0.01f;
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
            // If the raycast hit was within click distance, initiate a click
            bool inClickDistance = hit.distance < clickDistance;
            // If we are close enough to click, we are close enough to hold
            if (inClickDistance) return true;
            // If we have already clicked. and we are still within holding distance, keep holding
            else if (distancePressed && hit.distance < clickDistance * 3) return true;
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