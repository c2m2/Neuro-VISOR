using UnityEngine;

namespace C2M2.Interaction.Signaling
{
    using Utils;
    /// <summary>
    /// Abstract class 
    /// </summary>
    /// TODOs, known issues:
    ///     This script doesn't know how to handle quick transitions between raycastable objects.
    ///     If the user holds a press on one raycastable target, and then moves immediately to another, 
    ///     EndPress will not be called correctly on the previous raycast target.
    public abstract class RaycastEventSignaler : PressEventSignaler
    {       
        public bool rightHand = true;
        public LayerMask layerMask;
        public float maxRaycastDistance = 10f;
        private RaycastEventManager curEvent = null;
        private RaycastHit hit;
        private GameObject curObj; // Used to track if raycasted object has changed
        
        protected abstract void OnAwake();
        protected void Awake()
        {
            layerMask = LayerMask.GetMask(new string[] { "Raycast" });
            OnAwake();
        }
        protected abstract void OnStart();
        private void Start()
        {
            OnStart();
        }
        // FixedUpdate is called once per phsyics frame
        private void FixedUpdate()
        {          
            // If a raycast is requested (button one on Oculus, constant for mouse)
            if (RaycastRequested())
            {
                // Perform the raycast and store the result
                bool raycastHit = RaycastingMethod(out hit, maxRaycastDistance, layerMask);

                // If the raycast hit,
                if (raycastHit)
                {
                    // See if the hit object has changed
                    if(hit.collider.gameObject != curObj)
                    {
                        // Clean up hover, press events on previous object
                        OnHoverEnd();
                        OnPressEnd();
                        
                        // Update object
                        curObj = hit.collider.gameObject;

                        // Find the event manager to trigger on the new object
                        curEvent = FindRaycastTrigger(hit);
                    }

                    // Get user press state
                    Pressing = PressCondition();
                    // If the user is not pressing, we are hovering over the object
                    Hovering = !Pressing;
                }
                else
                {
                    // If we didn't hit a valid target, clean up hover, press events
                    Hovering = false;
                    Pressing = false;

                    curEvent = null;
                    curObj = null;
                }

                // TODO: If you hover over a different raycastable object immediately, this will not end the hover on the old object
                //      This needs to track if the raycastHit object changes
            }
            else { Pressing = false; }
        }
        /// <summary> Try to find the RaycastTriggerManager on the hit object </summary>
        private static RaycastEventManager FindRaycastTrigger(RaycastHit hit)
        {
            return hit.collider.GetComponentInParent<RaycastEventManager>();

        }
        // TODO: remove sealed and OnSub methods, call base.OnHover in children
        protected sealed override void OnHoverHold()
        {
            if (curEvent != null) curEvent.HoverEvent(rightHand, hit);
        }
        protected sealed override void OnHoverEnd()
        {
            OnHoverEndSub();
            if (curEvent != null) curEvent.HoverEndEvent(rightHand, hit);
        }
        sealed protected override void OnPressBegin()
        {
            OnPressSub();
            if (curEvent != null) curEvent.PressEvent(rightHand, hit);
        }
        sealed protected override void OnPressHold()
        {
            OnHoldPressSub();
            if (curEvent != null) curEvent.HoldEvent(rightHand, hit);
        }
        sealed protected override void OnPressEnd()
        {
            OnEndPressSub();
            if (curEvent != null) curEvent.EndEvent(rightHand, hit);
        }
        /// <summary> Signal children that a hover is happening </summary>
        protected virtual void OnHoverSub() { }
        /// <summary> Signal children that a hover is not happening </summary>
        protected virtual void OnHoverEndSub() { }
        /// <summary> Signal children that a press is beginning </summary>
        protected virtual void OnPressSub() { }
        /// <summary> Signal children that a press is being held </summary>
        protected virtual void OnHoldPressSub() { }
        /// <summary> Signal children that a press is ending </summary>
        protected virtual void OnEndPressSub() { }
        /// <summary> Let the input type choose their own raycasting method </summary>
        /// <param name="hit"> Resulting hit of the raycast </param>
        /// <param name="layerMask"> Which layer(s) should the raycast paya attention to? </param>
        /// <returns> Whether or not the raycast hit a valid target </returns>
        protected abstract bool RaycastingMethod(out RaycastHit hit, float maxDistance, LayerMask layerMask);
        /// <summary>
        /// Rrequire some condition before raycast requests are made.
        /// </summary>
        /// <remarks> 
        /// You can require a button be pressed before raycasting.
        /// In Oculus, this returns true if button "One" is pressed down.
        /// With mouse raycasting, this returns true if the mouse button is pressed down
        /// </remarks>
        protected abstract bool RaycastRequested();
        /// <summary>
        /// Require a second condition before raycast events are triggered
        /// </summary>
        /// <remarks>
        /// In Oculus, this is the trigger button. The user must hold "One" to begin raycasting,
        /// and then the trigger once they want to see events triggered. This way, raycasts can
        /// be visualized in VR before events are triggered with the raycast.
        /// With mouse raycasting, this simply returns "true", since we don't need to visualize raycasts
        /// before triggering events. We can just skip right to triggering events
        /// </remarks>
        protected abstract bool PressCondition();
    }
}