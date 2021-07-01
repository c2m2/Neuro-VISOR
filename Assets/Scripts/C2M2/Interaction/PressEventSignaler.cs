using UnityEngine;

namespace C2M2.Interaction
{
    /// <summary>
    /// Send press, holdPress, and endPress events based on set values of Pressed and RaycastTriggerManager
    /// </summary>
    /// <remarks>
    /// Make a class inherit from this class. The child class should monitor for input, and then set press
    /// whenever input is received. This calss will handle checking for whether the input is a press, a hold,
    /// or an end press
    /// </remarks>
    public abstract class PressEventSignaler : MonoBehaviour
    {
        private bool pressing = false;
        /// <summary>
        /// Was the input source pressed in the previous frame?
        /// </summary>
        protected bool Pressing
        {
            get { return pressing; }
            set
            {
                // If we weren't pressing before,
                if (pressing == false)
                { 
                    // And we are pressing now, we have a first press instance
                    if(value == true) OnPressBegin();
                    // Otherwise we aren't pressing now nor before, so nothing is happening
                }
                // If we were pressing before,
                else
                {
                    // and we are still pressing now, we are holding a press
                    if(value == true) OnPressHold();
                    // and now we aren't pressing, the end of a press has occurred
                    else OnPressEnd();
                }
                pressing = value;
                /*
                if (pressing == false && value == false)
                { // If we weren't pressing before, and aren't pressing now, nothing is happening

                }
                else if (pressing == false && value == true)
                { // If we weren't pressing before, and we are now, we have a first press instance 
                    OnPress();
                    pressing = value;
                }
                else if (pressing == true && value == true)
                { // If we were pressing before, and we are pressing now, we are holding press
                    OnHoldPress();
                }
                else if (pressing == true && value == false)
                { // If we were pressing before, and now we aren't pressing, we have the end of a press
                    OnEndPress();
                    // Store new value
                    pressing = value;
                }
                */
            }
        }
        private bool hovering = false;
        protected bool Hovering
        {
            get { return hovering; }
            set
            {
                // If we were not hovering before,
                if (hovering == false)
                {
                    // and we are hovering now, we have the beginning of a hover
                    if(value == true) OnHoverBegin();
                    // Otherwise no hover was happening nor is happening now, so nothing is happening
                }
                // If we were hovering before,
                else
                {
                    // And we are still hovering now, a hover is being held
                    if(value == true) OnHoverHold();
                    // And we are not hovering now, a hover is ending
                    else OnHoverEnd();
                }
                hovering = value;
            }
        }
        protected virtual void OnHoverBegin() { }
        protected virtual void OnHoverHold() { }
        protected virtual void OnHoverEnd() { }
        protected virtual void OnPressBegin() { }
        protected virtual void OnPressHold() { }
        protected virtual void OnPressEnd() { }
    }
}