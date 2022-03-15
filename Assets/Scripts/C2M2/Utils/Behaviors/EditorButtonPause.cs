using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;

namespace C2M2.Utils.DebugUtils.Actions
{
    [System.Serializable]
    public class PrimaryButtonEvent : UnityEvent<bool> { }
    /// <summary>
    /// Allows user to press a button to pause editor play mode from within the application
    /// </summary>
    public class EditorButtonPause : MonoBehaviour
    {
        public bool allowOculusPause = true;
        List<InputDevice> inputDevices = new List<InputDevice>();
        public PrimaryButtonEvent primaryButtonPress;
        private bool lastButtonState = false;
        public bool allowKeyboardPause = true;
        public KeyCode keyboardPauseButton = KeyCode.Space;
        // Update is called once per frame
        private void Start()
        {
            if (primaryButtonPress == null)
            {
                primaryButtonPress = new PrimaryButtonEvent();
            }

            //Create input characteristics to target devices specific to the hand controllers
            InputDeviceCharacteristics controllerCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics ,inputDevices);
        }  

        void Update()
        {
            bool tempState = false;
            foreach (var device in inputDevices)
            {
                bool primaryButtonState = false;
                tempState = device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonState)
                            && primaryButtonState
                            || tempState;
            }
            if (allowOculusPause)
            {
                if(tempState!=lastButtonState)
                {
                    primaryButtonPress.Invoke(tempState);
                    lastButtonState = tempState;
                    Debug.Break();
                    Debug.Log("Editor Paused");
                }
            }
            if (allowKeyboardPause)
            {
                if (tempState != lastButtonState)
                {
                    primaryButtonPress.Invoke(tempState);
                    lastButtonState = tempState;
                    Debug.Break();
                    Debug.Log("Editor Paused");
                }
            }
        }
    }
}