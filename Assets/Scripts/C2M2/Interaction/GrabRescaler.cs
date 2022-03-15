using UnityEngine;
using C2M2.Utils;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.Events;

namespace C2M2.Interaction
{
    [System.Serializable]
    public class PrimaryThumbstickEvent: UnityEvent<bool> { }
    /// <summary>
    /// Controls the scaling of a transform
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GrabRescaler : MonoBehaviour
    {
        private XRGrabInteractable grabbable = null;
        private Vector3 origScale;
        private Vector3 minScale;
        private Vector3 maxScale;
        public float scaler = 0.2f;
        public float minPercentage = 0;
        public float maxPercentage = float.PositiveInfinity;
        public bool xScale = true;
        public bool yScale = true;
        public bool zScale = true;
        public List<InputDevice> controllers = new List<InputDevice>();
        public PrimaryThumbstickEvent primaryThumbstickPress;
        private bool lastThumbstickState = false;
        public KeyCode incKey = KeyCode.UpArrow;
        public KeyCode decKey = KeyCode.DownArrow;
        public KeyCode resetKey = KeyCode.R;
        public Transform target = null;

        private float ChangeScaler
        {
            get
            {
                float yTotal = 0;
                Vector2 thumbstickDirection = new Vector2();
                foreach (var device in controllers)
                {
                    device.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstickDirection);
                    yTotal += thumbstickDirection.y;
                }
                ///<returns>A float between -1 and 1, where -1 means the thumbstick y axis is completely down and 1 implies it is all the way up</returns>
                if (GameManager.instance.vrDeviceManager.VRActive) return yTotal;
                else if (Input.GetKey(incKey) && !Input.GetKey(decKey)) return .2f;
                else if (Input.GetKey(decKey) && !Input.GetKey(incKey)) return -.2f;
                return 0;
            }
        }


        ///<returns>A boolean of whether the joystick is pressed</returns>
        private bool ResetPressed
        {
            get
            {
                bool tempState = false;
                foreach (var device in controllers)
                {
                    bool primaryThumbstickState = false;
                    tempState = device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryThumbstickState) // did get a value
                                && primaryThumbstickState // the value we got
                                || tempState; // cumulative result from other controllers
                }
                bool isPressed = tempState != lastThumbstickState;
                if (isPressed) // Button state changed since last frame
                {
                    primaryThumbstickPress.Invoke(tempState);
                    lastThumbstickState = tempState;
                }
                if (GameManager.instance.vrDeviceManager.VRActive) return isPressed;
                else return Input.GetKey(resetKey);
            }
        }

        private void Awake()
        {
            InputDeviceCharacteristics controllerCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, controllers);
        }

        private void Start()
        {
            if (target == null) target = transform;

            grabbable = GetComponent<XRGrabInteractable>();

            // Use this to determine how to scale at runtime
            origScale = target.localScale;
            minScale = minPercentage * origScale;
            if (maxPercentage == float.PositiveInfinity) maxScale = Vector3.positiveInfinity;
            else maxScale = maxPercentage * origScale;
        }

        void Update()
        {
            // RaycastEventHandler handles calling rescale for Desktop mode, TODO this is bad and should be changed
            if (!GameManager.instance.vrDeviceManager.VRActive) return;

            if (grabbable.isSelected) Rescale();
        }

        public void Rescale()
        {
            if (ResetPressed)
            {
                target.localScale = origScale;
            }
            else if(ChangeScaler != 0)
            {
                Vector3 scaleValue = scaler * ChangeScaler * origScale;
                Vector3 newLocalScale = target.localScale + scaleValue;

                // Makes sure the new scale is within the determined range
                newLocalScale = Math.Clamp(newLocalScale, minScale, maxScale);

                // Only scales the proper dimensions
                if (!xScale) newLocalScale.x = target.localScale.x;
                if (!yScale) newLocalScale.y = target.localScale.y;
                if (!zScale) newLocalScale.z = target.localScale.z;

                target.localScale = newLocalScale;
            }
        }
    }
}
