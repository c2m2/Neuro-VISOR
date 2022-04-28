using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace C2M2.Interaction
{
    /// <summary> Attach this script to parent gameObject, create one child with one raycast trigger per child. Switch between triggers by index or switch the active trigger to any custom RaycastTrigger instance. </summary>
    public class RaycastEventManager : MonoBehaviour
    {
        public RaycastPressEvents LRTrigger
        {
            set
            {
                rightTrigger = value;
                leftTrigger = value;
            }
        }
        public RaycastPressEvents rightTrigger = null;
        public RaycastPressEvents leftTrigger = null;
        private RaycastPressEvents emptyTrigger = null;
        private static RaycastHit nullHit = new RaycastHit();
        private void Awake()
        { // If we have no default trigger on the left or right, supply an empty trigger
            emptyTrigger = GameManager.instance.GetComponent<RaycastPressEvents>();
            if (emptyTrigger == null) { emptyTrigger = GameManager.instance.gameObject.AddComponent<RaycastPressEvents>(); }
            if (rightTrigger == null) { rightTrigger = emptyTrigger; }
            if (leftTrigger == null) { leftTrigger = emptyTrigger; }
        }
        public void TriggerChangeRight(RaycastPressEvents trigger) { if (trigger != null) { rightTrigger = trigger; } }
        public void TriggerChangeLeft(RaycastPressEvents trigger) { if (trigger != null) { leftTrigger = trigger; } }
        public void TriggerChangeBoth(RaycastPressEvents trigger)
        {
            TriggerChangeRight(trigger);
            TriggerChangeLeft(trigger);
        }
        public void TriggerEmptyRight() => TriggerChangeRight(emptyTrigger);
        public void TriggerEmptyLeft() => TriggerChangeLeft(emptyTrigger);
        public void TriggerEmptyBoth() => TriggerChangeBoth(emptyTrigger);

        public void HoverEvent(bool rightHand, RaycastHit hit)
        {
            if (rightHand && rightTrigger != null) { rightTrigger.Hover(hit); }
            else if (!rightHand && leftTrigger != null) { leftTrigger.Hover(hit); }
        }
        public void HoverEndEvent(bool rightHand, RaycastHit hit)
        {
            if (rightHand && rightTrigger != null) { rightTrigger.EndHover(hit); }
            else if (!rightHand && leftTrigger != null) { leftTrigger.EndHover(hit); }
        }
        public void PressEvent(bool rightHand, RaycastHit hit)
        {
            if (rightHand && rightTrigger != null) { rightTrigger.Press(hit); }
            else if (!rightHand && leftTrigger != null) { leftTrigger.Press(hit); }
        }
        public void HoldEvent(bool rightHand, RaycastHit hit)
        {
            if (rightHand && rightTrigger != null) { rightTrigger.HoldPress(hit); }
            else if (!rightHand && leftTrigger != null) { leftTrigger.HoldPress(hit); }
        }
        public void EndEvent(bool rightHand, RaycastHit hit)
        {
            if (rightHand && rightTrigger != null) { rightTrigger.EndPress(hit); }
            else if (!rightHand && leftTrigger != null) { leftTrigger.EndPress(hit); }
        }
        public void AllEvents(bool rightHand, RaycastHit hit)
        {
            PressEvent(rightHand, hit);
            HoldEvent(rightHand, hit);
            EndEvent(rightHand, hit);
        }
        public void PressEventNull(bool rightHand) => PressEvent(rightHand, nullHit);
        public void HoldEventNull(bool rightHand) => HoldEvent(rightHand, nullHit);
        public void EndEventNull(bool rightHand) => EndEvent(rightHand, nullHit);
        public void AllEventsNull(bool rightHand) => AllEvents(rightHand, nullHit);
        public override string ToString()
        {
            string s = "RightTrigger:\n\tOnHitEvent:";
            for (int i = 0; i < rightTrigger.OnPress.GetPersistentEventCount(); i++)
            {
                s += "n\t\t" + rightTrigger.OnPress.GetPersistentMethodName(i);

            }
            s += "\n\tOnHoldEvent:";
            for (int i = 0; i < rightTrigger.OnHoldPress.GetPersistentEventCount(); i++)
            {
                s += "n\t\t" + rightTrigger.OnHoldPress.GetPersistentMethodName(i);

            }
            s += "\n\tOnEndEvent:";
            for (int i = 0; i < rightTrigger.OnEndPress.GetPersistentEventCount(); i++)
            {
                s += "n\t\t" + rightTrigger.OnEndPress.GetPersistentMethodName(i);

            }
            s += "\nLeftTrigger:\n\tOnHitEvent:";
            for (int i = 0; i < leftTrigger.OnPress.GetPersistentEventCount(); i++)
            {
                s += "n\t\t" + leftTrigger.OnPress.GetPersistentMethodName(i);

            }
            s += "\n\tOnHoldEvent:";
            for (int i = 0; i < leftTrigger.OnHoldPress.GetPersistentEventCount(); i++)
            {
                s += "n\t\t" + leftTrigger.OnHoldPress.GetPersistentMethodName(i);

            }
            s += "\n\tOnEndEvent:";
            for (int i = 0; i < leftTrigger.OnEndPress.GetPersistentEventCount(); i++)
            {
                s += "n\t\t" + leftTrigger.OnEndPress.GetPersistentMethodName(i);

            }
            return s;
        }
    }
}
