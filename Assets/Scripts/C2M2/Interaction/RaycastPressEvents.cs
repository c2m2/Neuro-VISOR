using UnityEngine;
using System;
using UnityEngine.Events;

namespace C2M2.Interaction
{
    /// <summary> Store custom events that the corresponding RaycastTriggerManager will invoke </summary>
    /// Adapted from: https://answers.unity.com/questions/1335277/how-to-make-a-custom-onclick-event.html
    /// RaycastTrigger events will actually be called by RaycastForward
    public class RaycastPressEvents : MonoBehaviour
    {
        [Serializable]
        public class RaycastHitEvent : UnityEvent<RaycastHit> { }
        // Event for when we hover on an object for the first time
        [SerializeField]
        private RaycastHitEvent onHover = new RaycastHitEvent();
        public RaycastHitEvent OnHover { get { return onHover; } set { onHover = value; } }
        // Event for when we end hovering on an object
        [SerializeField]
        private RaycastHitEvent onHoverEnd = new RaycastHitEvent();
        public RaycastHitEvent OnHoverEnd { get { return onHoverEnd; } set { onHoverEnd = value; } }
        // Event for when we click on an object for the first time
        [SerializeField]
        private RaycastHitEvent onPress = new RaycastHitEvent();
        public RaycastHitEvent OnPress { get { return onPress; } set { onPress = value; } }
        // Event for when we are holding a raycast click on an object
        [SerializeField]
        private RaycastHitEvent onHoldPress = new RaycastHitEvent();
        public RaycastHitEvent OnHoldPress { get { return onHoldPress; } set { onHoldPress = value; } }
        // Event for when we end a raycast click on an object
        [SerializeField]
        private RaycastHitEvent onEndPress = new RaycastHitEvent();
        public RaycastHitEvent OnEndPress { get { return onEndPress; } set { onEndPress = value; } }
        // Calling these methods invokes the corresponding event
        public void Hover(RaycastHit hit) { OnHover.Invoke(hit); }
        public void EndHover(RaycastHit hit) { OnHoverEnd.Invoke(hit); }
        public void Press(RaycastHit hit) { OnPress.Invoke(hit); }
        public void HoldPress(RaycastHit hit) { OnHoldPress.Invoke(hit); }
        public void EndPress(RaycastHit hit) { OnEndPress.Invoke(hit); }
    }
}