using UnityEngine;

namespace C2M2.Interaction.Signaling
{
    using Utils;
    /// <summary>
    /// Abstract class 
    /// </summary>
    public abstract class RaycastEventSignaler : PressEventSignaler
    {
        public bool rightHand = true;
        public LayerMask layerMask;
        public float maxRaycastDistance = 10f;
        private RaycastEventManager activeEvent = null;
        private RaycastHit lastHit;
        private GameObject prevObj; // Used to track if raycasted object has changed

        protected abstract void OnAwake();
        protected void Awake()
        {
            if (layerMask == default(LayerMask) 
                || layerMask.Equals(LayerMask.GetMask(new string[] { "Nothing" })))
            {
                layerMask = LayerMask.GetMask(new string[] { "Raycast", "RaycastCollider" });
            }
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
            if (BeginRaycastingCondition())
            { /* This is the 'A' button on Oculus, mouse always raycasts */
                bool raycastHit = RaycastingMethod(out lastHit, maxRaycastDistance, layerMask);
                if (raycastHit)
                { /* If we hit a raycastable target, try to find a trigger target */
                    // If the hit object has changed
                    if(prevObj != lastHit.collider.gameObject)
                    {
                        OnHoverEnd();
                        prevObj = lastHit.collider.gameObject;
                    }

                    activeEvent = FindRaycastTrigger(lastHit);
                }
                else
                {
                    OnHoverEnd();
                    activeEvent = null;
                }
                // Check if the next button is pressed, and then try to activate relevant events
                // TODO: If you hover over a different raycastable object immediately, this will not end the hover on the old object
                //      This needs to track if the raycastHit object changes
                Pressed = ChildsPressCondition();
                if(raycastHit && !Pressed)
                {
                    OnHover();
                }
                else
                { // If we didn't hit a valid target or a press is happening, we are no longer hovering
                    OnHoverEnd();
                }
            }
            else { Pressed = false; }
        }
        /// <summary> Try to find the RaycastTriggerManager on the hit object </summary>
        private static RaycastEventManager FindRaycastTrigger(RaycastHit hit)
        {
            return hit.collider.GetComponentInParent<RaycastEventManager>();

        }

        private void OnObjectChange()
        {
            OnHoverEnd();
        }
        protected sealed override void OnHover()
        {
            OnHoverSub();
            if (activeEvent != null) activeEvent.HoverEvent(rightHand, lastHit);
        }
        protected sealed override void OnHoverEnd()
        {
            OnHoverEndSub();
            if (activeEvent != null) activeEvent.HoverEndEvent(rightHand, lastHit);
        }
        sealed protected override void OnPress()
        {
            OnPressSub();
            if (activeEvent != null) activeEvent.PressEvent(rightHand, lastHit);
        }
        sealed protected override void OnHoldPress()
        {
            OnHoldPressSub();
            if (activeEvent != null) activeEvent.HoldEvent(rightHand, lastHit);
        }
        sealed protected override void OnEndPress()
        {
            OnEndPressSub();
            if (activeEvent != null) activeEvent.EndEvent(rightHand, lastHit);
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
        protected abstract bool BeginRaycastingCondition();
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
        protected abstract bool ChildsPressCondition();
    }
}