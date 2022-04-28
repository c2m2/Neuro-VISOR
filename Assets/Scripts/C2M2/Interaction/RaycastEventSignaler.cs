using UnityEngine;

namespace C2M2.Interaction
{
    /// <summary>
    /// Abstract class for signalers that use raycasting to trigger events on objects.
    /// </summary>
    /// <remarks>
    /// Manages event timing and shared logic.
    /// </remarks>
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

        private void Update()
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
                        if (Pressing) OnPressEnd();
                        
                        // Update object
                        curObj = hit.collider.gameObject;

                        // Find the event manager to trigger on the new object
                        curEvent = FindRaycastTrigger(hit);
                    }

                    // Get user press state
                    Pressing = PressCondition();

                    Hovering = true;
                }
                else
                {
                    // If we didn't hit a valid target, clean up hover, press events
                    Hovering = false;
                    Pressing = false;

                    curEvent = null;
                    curObj = null;
                }
            }
            else
            {
                Pressing = false;
                Hovering = false;
            }
        }
        /// <summary> Try to find the RaycastTriggerManager on the hit object </summary>
        private static RaycastEventManager FindRaycastTrigger(RaycastHit hit)
        {
            return hit.collider.GetComponentInParent<RaycastEventManager>();

        }
        // TODO: remove sealed and OnSub methods, call base.OnHover in children
        protected override void OnHoverHold()
        {
            if (curEvent != null) curEvent.HoverEvent(rightHand, hit);
        }
        protected override void OnHoverEnd()
        {
            if (curEvent != null) curEvent.HoverEndEvent(rightHand, hit);
        }
        protected override void OnPressBegin()
        {
            if (curEvent != null) curEvent.PressEvent(rightHand, hit);
        }
        protected override void OnPressHold()
        {
            if (curEvent != null) curEvent.HoldEvent(rightHand, hit);
        }
        protected override void OnPressEnd()
        {
            if (curEvent != null) curEvent.EndEvent(rightHand, hit);
        }

        /// <summary> Let the input type choose their own raycasting method </summary>
        /// <param name="hit"> Resulting hit of the raycast </param>
        /// <param name="layerMask"> Which layer(s) should the raycast paya attention to? </param>
        /// <returns> Whether or not the raycast hit a valid target </returns>
        protected abstract bool RaycastingMethod(out RaycastHit hit, float maxDistance, LayerMask layerMask);
        /// <summary>
        /// Require some condition before raycast requests are made.
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