using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR;
using UnityEngine.Events;

namespace C2M2.Utils.DebugUtils
{
    [System.Serializable]
    public class SecondaryButtonEvent : UnityEvent<bool> { }
#if (UNITY_EDITOR)
    public class PrintMaterialAssetPaths : MonoBehaviour
    {
        public Material mat;
        public SecondaryButtonEvent secondaryButtonPress;

        private bool lastSecondaryButtonState = false;
        private List<InputDevice> handControllers = new List<InputDevice>();

        private void Awake()
        {
            InputDeviceCharacteristics desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, handControllers);
        }

        private void Update()
        {
            bool tempState = false;
            foreach (var device in handControllers)
            {
                bool secondaryButtonState = false;
                tempState = device.TryGetFeatureValue(CommonUsages.primaryButton, out secondaryButtonState) // did get a value
                            && secondaryButtonState // the value we got
                            || tempState; // cumulative result from other controllers
            }
            bool isPressed = tempState!=lastSecondaryButtonState;
            if (isPressed) // Button state changed since last frame
            {
                secondaryButtonPress.Invoke(tempState);
                lastSecondaryButtonState = tempState;
                Debug.Log("Spacebar was pressed");
                Debug.Log("Name: " + AssetDatabase.GetAssetPath(mat));
                Debug.Log("Path: " + AssetDatabase.GetAssetPath(mat));
            }
        }
    }
#endif
}