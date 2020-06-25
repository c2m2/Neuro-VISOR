using UnityEngine;

namespace C2M2.Interaction.Signaling
{
    using Utils;
    public abstract class RaycastEventSignaler : PressEventSignaler
    {
        public bool rightHand = true;
        private LayerMask layerMask;
        public float maxRaycastDistance = 10f;
        private RaycastEventManager activeEvent = null;
        private RaycastHit lastHit;

        protected abstract void OnAwake();
        protected void Awake()
        {
            layerMask = LayerMask.GetMask(new string[]{ "Raycast" });
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
            { /* This is the 'A' button on Oculus */
                if (RaycastingMethod(out lastHit, maxRaycastDistance, layerMask))
                { /* If we hit a raycastable target, try to find a trigger target */
                    activeEvent = FindRaycastTrigger(lastHit);
                }
                else { activeEvent = null; }
                // Check if the next button is pressed, and then try to activate relevant events
                Pressed = ChildsPressCondition();
            }
            else { Pressed = false; }
        }
        /// <summary> Try to find the RaycastTriggerManager on the hit object </summary>
        private static RaycastEventManager FindRaycastTrigger(RaycastHit hit)
        {
            return hit.collider.GetComponentInParent<RaycastEventManager>();

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
        /// <summary> Signal children that a press is beginning </summary>
        protected abstract void OnPressSub();
        /// <summary> Signal children that a press is being held </summary>
        protected abstract void OnHoldPressSub();
        /// <summary> Signal children that a press is ending </summary>
        protected abstract void OnEndPressSub();
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