using UnityEngine;

namespace C2M2
{
    public class MouseEventSignaler : RaycastEventSignaler
    {
        protected override void OnAwake() { }
        protected override void OnStart() { }
        /// <summary>
        /// This is returns true if the left mouse button is pressed down
        /// </summary>
        protected override bool BeginRaycastingCondition() => Input.GetMouseButton(0);
        /// <summary>
        /// This builds a ray from the mouse's position, and attempts a raycast using that ray
        /// </summary>
        protected override bool RaycastingMethod(out RaycastHit hit, float maxDistance, LayerMask layerMask)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, maxDistance, layerMask);
        }
        /// <summary>
        /// With mouse raycasting, we only want to press the mouse button to trigger events
        /// </summary>
        protected override bool ChildsPressCondition() => true;
        /// We don't need any extra functionality for our mouse activator,
        /// so these just return immediatley
        protected override void OnPressSub() { }
        protected override void OnHoldPressSub() { }
        protected override void OnEndPressSub() { }
    }
}
