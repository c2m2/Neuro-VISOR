#pragma warning disable CS0618

using UnityEngine;
using System;
namespace C2M2.Interaction
{
    [Obsolete("Replace by OculusEventSignaler")]
    public class RaycastForward : MonoBehaviour
    {
        #region Event_Utilities
        public bool rightHand = true;      // Does this script belong to the right, or the left controller?
        #region Private_Members
        private RaycastEventManager activeTriggerManager;         // Holds the RaycastTriggers that hold our active hit, hold, and end (HHE) events
        private int activeObjectID = -1;    // Identifying InstanceID of the current valid raycast target

        private bool valid = false;         // Are we raycasting to an object that has a RaycastTriggerManager and any valid HHE events to invoke?
        private bool clicked = false;       // Are we holding triggerButton on a valid target?
        private bool touched = false;       // Have we entered minHitDist on a valid target, or are we still within hysteresisDist?
        private bool hitTriggered, holdTriggered, endTriggered;     // Have we triggered any HHE events already this frame?
        private bool validHitEvent, validHoldEvent, validEndEvent;  // Does our active raycast target have any valid HHE events to invoke?
        #endregion
        #endregion
        #region Raycasting_Utilities
        #region Public_Members
        [Header("Raycasting Information")]
        [Tooltip("The Oculus controller being raycasted from")]
        public OVRInput.Controller raycastController = OVRInput.Controller.RTouch;
        [Tooltip("Button to activate raycasting mode")]
        public OVRInput.Button raycastButton = OVRInput.Button.One;
        [Tooltip("Button to invoke hit/hold events from a distance")]
        public OVRInput.Button triggerButton = OVRInput.Button.PrimaryIndexTrigger;
        [Tooltip("Layers that raycast pays attention to")]
        public LayerMask layerMask;
        [Tooltip("The renderer component for the static pointed hand ")]
        public MeshRenderer staticIndexRenderer;
        public GameObject staticHandObject; // TODO: Remove
        [Tooltip("This is the default Oculus hand object, usually hand_right or hand_left")]
        public GameObject defaultHandObject;
        [Tooltip("Min distance to trigger hit events")]
        public float minHitDist = 0.01f;
        [Tooltip("Min distance to retrigger hit events or trigger hold events if hit event already triggered")]
        public float hysteresisDist = 0.03f;
        #endregion
        private Vector3 relativeForward = new Vector3(0, 0, 1);     // Direction relative to fingertip to project raycast, default local z-axis
        #endregion
        #region Line_Renderer
        #region Public_Members
        [Header("Line Renderer Information")]
        [Tooltip("Line renderer default color")]
        public Color nullCol = Color.cyan;
        [Tooltip("Line renderer color when holding a click")]
        public Color holdCol = new Color(1f, 0.6f, 0f);
        [Tooltip("Line renderer color when hit lands")]
        public Color hitCol = Color.red;
        [Tooltip("Line renderer color when ending a hold/click")]
        public Color endCol = Color.yellow;
        #endregion
        #region Private_Members
        private LineRenderer lineRend;      // Mimick raycast hit vector with a visible line
        private bool invisible = false;     // Is our line renderer currently rendering?
        #endregion
        #endregion
        #region Haptics
        #region Public_Members
        [Header("Haptic Response Information")]
        [Tooltip("The frequency of the haptic response to activating a haptic trigger")]
        public float hapFreq = 0.5f;
        [Tooltip("Default intensity of our controller (0 strongly reccommended)")]
        public float nullAmp = 0f;
        [Tooltip("The intensity of the haptic response to invoking a hit event")]
        public float hitAmp = 0.9f;
        [Tooltip("The intensity of the haptic response to invoking a hold event")]
        public float holdAmp = 0.3f;
        [Tooltip("The intensity of the haptic response to invoking an end event (0 reccommended)")]
        public float endAmp = 0f;
        #endregion
        #endregion
        private enum StateCode : sbyte { NULL, HIT, HOLD, END };     // Define the possible states of our raycaster
        private StateCode currentState = StateCode.NULL;            // Track of the current state of our raycaster
        public bool mouseMode = false;
        private void Start()
        {
            // If this is the non-VR camera, then we don't need to find hand objects because we don't have hands
            mouseMode = GameManager.instance.nonVRCamera == gameObject;
            if (!mouseMode)
            {
                // Resolve which controller we are using. Left hand will be default
                rightHand = (raycastController.Equals(OVRInput.Controller.RTouch) || gameObject.name.Contains("right")) ? true : false;
                // Get the renderer of the static pointed finger and disable it initially
                // staticIndexRenderer = staticHandObject.GetComponent<MeshRenderer>();        
                // Disable the static hand if we can find it
                if (staticIndexRenderer != null) { staticIndexRenderer.enabled = false; }
                else { Debug.LogWarning("No Static hand model found."); }
                // Try to find the line renderer
                lineRend = gameObject.GetComponentInChildren<LineRenderer>();
                if (lineRend == null) { Debug.LogWarning("Couldn't find line renderer in RaycastForward"); }
            }
        }
        void FixedUpdate()
        {
            TryRaycastHit();
        }
        public void ChangeStaticHandColor(Color color)
        {
            staticIndexRenderer = staticHandObject.GetComponent<MeshRenderer>();
            staticIndexRenderer.material.SetColor("_BaseColor", color);
            //staticIndexRenderer.material.color = color;
        }
        /// <summary> If raycastButton is pressed, attempt to raycast to valid target and activate valid raycastTrigger events on the target </summary>
        private void TryRaycastHit()
        {
            // If the user enables "raycast mode" by VR controller or by mouse
            bool raycastActive = OVRInput.Get(raycastButton, raycastController) || (mouseMode && Input.GetMouseButton(0));
            if (raycastActive)
            {
                // Resolve raycast hit info for VR or mouse controller
                RaycastHit hit;
                bool rayHit = false;
                if (mouseMode)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    rayHit = Physics.Raycast(ray, out hit, 10f, layerMask);
                }
                else
                {
                    RequestHandRenderState(true);       // Enable raycasting hand
                    Vector3 globalForward = transform.TransformDirection(relativeForward);      // Find the direction of the raycast
                    rayHit = Physics.Raycast(transform.position, globalForward, out hit, 10f, layerMask);
                }
                if (rayHit)
                { // If our raycast hits a raycastable target,        
                  // Are we raycasting to a new object, and does that object have any valid raycasting events?
                    valid = ResolveCurrentTarget(hit);
                    if (valid)
                    { // If we have a valid target to hit,  
                        hitTriggered = false;
                        holdTriggered = false;
                        endTriggered = false;
                        // Don't allow calling more than one hit, hold, or end event in a single frame                 
                        DipointLinerendSetPositions(transform.position, hit.point);                     // Visualize our raycast as long as we have a valid target
                        ResolveClickEvents(hit);                                                           // Call events according to trigger button system
                        ResolveProximityEvents(hit);                                                       // Call events according to proximity distance activation system  
                                                                                                           // If we actually activated any triggers this frame, change hand asthetics to reflect it
                        if (holdTriggered) { RequestNewState(StateCode.HOLD); }
                        else if (hitTriggered) { RequestNewState(StateCode.HIT); }
                        else if (endTriggered) { RequestNewState(StateCode.END); }
                        else { RequestNewState(StateCode.NULL); }                               // Othwerwise we are inactive
                    }
                    else { DontDrawLinerend(); }    // If we don't have a valid target, don't draw the linerender
                }
                else
                {
                    DontDrawLinerend();
                }    // If our raycast did NOT hit a raycastable target, don't draw the linerender
            }
            else
            { // If we aren't in "raycast mode"
                RequestHandRenderState(false);      // Return to default hand
                                                    // Turn off controller vibration & don't draw the raycast
                RequestNewState(StateCode.NULL);
                DontDrawLinerend();
                if (touched) { touched = false; }       // Allow proximity touches again
                if (clicked) { clicked = false; }       // Allow trigger button activation again
            }

            #region Local_Functions
            // Are we raycasting to a new object, and does that object have any valid raycasting events?
            bool ResolveCurrentTarget(RaycastHit hit)
            {
                int newID = hit.collider.gameObject.GetInstanceID();
                if (newID != activeObjectID)
                { // If we have found a new object,
                    RaycastEventManager newTriggerManager = hit.collider.GetComponentInParent<RaycastEventManager>();
                    if (newTriggerManager != null)
                    { // If it has a valid trigger,
                      // Find whether or not there are valid hit, hold, and end events based on handedness
                        if (rightHand)
                        {
                            validHitEvent = (newTriggerManager.rightTrigger.OnPress.GetPersistentEventCount() > 0) ? true : false;
                            validHoldEvent = (newTriggerManager.rightTrigger.OnHoldPress.GetPersistentEventCount() > 0) ? true : false;
                            validEndEvent = (newTriggerManager.rightTrigger.OnEndPress.GetPersistentEventCount() > 0) ? true : false;
                        }
                        else
                        {
                            validHitEvent = (newTriggerManager.leftTrigger.OnPress.GetPersistentEventCount() > 0) ? true : false;
                            validHoldEvent = (newTriggerManager.leftTrigger.OnHoldPress.GetPersistentEventCount() > 0) ? true : false;
                            validEndEvent = (newTriggerManager.leftTrigger.OnEndPress.GetPersistentEventCount() > 0) ? true : false;
                        }
                        if (validHitEvent || validHoldEvent || validEndEvent)
                        { // If we have found some active event to activate, save it and mark this target as valid
                            activeTriggerManager = newTriggerManager;
                            activeObjectID = newID;
                            return true;
                        }
                    }
                    // If the object doesn't have a RaycastTriggerManager, then we don't have a valid object  
                    return false;
                }
                // Otherwise we have a previously valid object
                else return false;
            }
            // Enable/disable static raycasting hand depending on raycast state
            void RequestHandRenderState(bool rURaycasting)
            {
                if (!mouseMode)
                {
                    if (rURaycasting)
                    { // If we're trying to use raycasting hands,
                        if (defaultHandObject.activeSelf || !staticIndexRenderer.enabled)
                        { // If our hand object isn't in raycasting mode yet,
                            defaultHandObject.SetActive(false);     // Disable the default Oculus hand
                            staticIndexRenderer.enabled = true;     // Enable the static hand mesh
                        }
                    }
                    else
                    {
                        if (!defaultHandObject.activeSelf || staticIndexRenderer.enabled)
                        { // If our hand object is still in raycasting mode,
                            defaultHandObject.SetActive(true);      // Enable default Oculus hand
                            staticIndexRenderer.enabled = false;    // Disable static hand mesh
                        }
                    }
                }
            }
            // Functionality for invoking trigger events with triggerButton
            void ResolveClickEvents(RaycastHit hit)
            {
                bool buttonPressed = false;
                if (mouseMode) buttonPressed = true;
                else if (OVRInput.Get(triggerButton, raycastController)) buttonPressed = true;
                if (buttonPressed)
                { // If we are holding down the relevant button
                    if (!clicked)
                    { // FIRST CLICK CASE: We have just pressed the index trigger all the way down on our controller for the first time recently.      
                        AttemptHitEvent(hit);
                        clicked = true;
                    }
                    // We should attempt to call a hit event every frame that the button is held
                    AttemptHoldEvent(hit);
                }
                else
                { // If we are not holding down the trigger
                    if (clicked)
                    { // If we were just clicking, and now we aren't, reset clicked and call the onEndEvent
                        AttemptEndEvent(hit);
                        clicked = false;
                    }
                }
            }
            // Functionality for invoking trigger events for close-proximity raycast hits
            void ResolveProximityEvents(RaycastHit hit)
            {
                if (hit.distance < minHitDist)
                { // If we are within close enough to activate our trigger,
                    if (!touched)
                    { // & if we didn't just activate that trigger by being within distance already,           
                        touched = true;     // Don't reactivate any triggers until we move far enough away.
                        AttemptHitEvent(hit);
                    }
                    else { AttemptHoldEvent(hit); }    // If we're still close, but have already trigegred the hit event, then we're holding
                }
                else if (hit.distance < hysteresisDist)
                { // If we are still within hysteresis range, don't allow further touches but continue calling hold events
                    if (touched) { AttemptHoldEvent(hit); } // If we've already hit the object, then we can hold it
                }
                else
                { // If we are far enough away, then we are done with our proximity touch/hold
                    if (touched)
                    {
                        touched = false;        // Allow proximity touches again
                        AttemptEndEvent(hit);
                    }
                }
            }
            // Invoke valid hit, hold, or end events
            void AttemptHitEvent(RaycastHit hit)
            {
                if (validHitEvent)
                { // If the active object has a valid hit or hold event, activate the trigger and produce active aesthetics
                    if (!hitTriggered)
                    { // If we haven't already activated it, do that
                        if (rightHand) { activeTriggerManager.rightTrigger.Press(hit); }
                        else { activeTriggerManager.leftTrigger.Press(hit); }
                        hitTriggered = true;
                    }
                }
            }
            void AttemptHoldEvent(RaycastHit hit)
            {
                if (validHoldEvent)
                { // If we have an active hold event,
                    if (!holdTriggered)
                    { // If we haven't already activated it this frame,
                        if (rightHand) { activeTriggerManager.rightTrigger.HoldPress(hit); }
                        else { activeTriggerManager.leftTrigger.HoldPress(hit); }
                        holdTriggered = true;
                    }
                }
            }
            void AttemptEndEvent(RaycastHit hit)
            {
                if (validEndEvent)
                {
                    if (!endTriggered)
                    {
                        if (rightHand) { activeTriggerManager.rightTrigger.EndPress(hit); }
                        else { activeTriggerManager.leftTrigger.EndPress(hit); }
                        endTriggered = true;
                    }
                }
            }
            // Carry out changes for the current trigger state
            void RequestNewState(StateCode newState)
            {
                if (currentState != newState)
                { // If we have new changes to reflect,
                    currentState = newState;    // Store the new state,
                    if (!mouseMode)
                    {
                        switch (currentState)
                        { // Set the appropriate color and controller vibration
                            case StateCode.NULL:
                                LineRendSetEndpointColors(nullCol);
                                OVRInput.SetControllerVibration(hapFreq, nullAmp, raycastController);
                                break;
                            case StateCode.HOLD:
                                LineRendSetEndpointColors(holdCol);
                                OVRInput.SetControllerVibration(hapFreq, holdAmp, raycastController);
                                break;
                            case StateCode.HIT:
                                LineRendSetEndpointColors(hitCol);
                                OVRInput.SetControllerVibration(hapFreq, hitAmp, raycastController);
                                break;
                            case StateCode.END:
                                LineRendSetEndpointColors(endCol);
                                OVRInput.SetControllerVibration(hapFreq, endAmp, raycastController);
                                break;
                        }
                    }
                }
            }
            // Sets both ends of a 2-point line renderer to be the same color
            void LineRendSetEndpointColors(Color newColor)
            {
                if (lineRend != null)
                {
                    lineRend.startColor = newColor;
                    lineRend.endColor = newColor;
                }
            }
            // Sets the position of both ends of a 2-point line renderer
            void DipointLinerendSetPositions(Vector3 position0, Vector3 position1)
            {
                if (lineRend != null)
                {
                    lineRend.SetPositions(new Vector3[2] { position0, position1 });
                    if (invisible) { invisible = false; }   // Tell DontDrawLineRend() that there is a visible line render
                }
            }
            /// Don't draw the line renderer
            void DontDrawLinerend()
            {
                if (lineRend != null)
                {
                    if (!invisible)
                    { // If there is a visible line render, 
                        lineRend.SetPositions(new Vector3[2] { Vector3.zero, Vector3.zero });   // Draw both points to the same position
                        invisible = true;                                                       // Don't bother again until we have a visible line render
                    }
                }
            }
            #endregion
        }
    }
}
