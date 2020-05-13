using UnityEngine;

namespace C2M2.Interaction.Signaling
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
        private bool pressedPrevFrame = false;
        /// <summary>
        /// Was the input source pressed in the previous frame?
        /// </summary>
        protected bool Pressed
        {
            get { return pressedPrevFrame; }
            set
            {
                if (pressedPrevFrame == false && value == false)
                { /* If both are already false, no press is happening or ending */

                }
                else if (pressedPrevFrame == false && value == true)
                { /* If input WASN'T pressed in the previous frame, and now it is, we have a first press */
                    OnPress();
                    // Store new value
                    pressedPrevFrame = value;
                }
                else if (pressedPrevFrame == true && value == true)
                { /* If input WAS pressed in the previous frame, and now it still is, we have a hold press */
                    OnHoldPress();
                }
                else if (pressedPrevFrame == true && value == false)
                { /* If input WAS pressed in the previous frame, and now it is NOT, we have the end of a press */
                    OnEndPress();
                    // Store new value
                    pressedPrevFrame = value;
                }
            }
        }
        protected abstract void OnPress();
        protected abstract void OnHoldPress();
        protected abstract void OnEndPress();
    }
}