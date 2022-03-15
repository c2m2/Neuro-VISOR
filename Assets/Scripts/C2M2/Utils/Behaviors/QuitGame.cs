using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace C2M2.Utils
{
    public class QuitGame : MonoBehaviour
    {
        public KeyCode quitKey = KeyCode.Escape;
        public List<InputDevice> devicesWithMenuBtn = new List<InputDevice>();
        public MenuButtonEvent menuButtonPress;
        private bool lastButtonState = false;
        private bool OculusRequested
        {
            get
            {
                bool tempState = false;
                foreach (var device in devicesWithMenuBtn)
                {
                    bool menuButtonState = false;
                    tempState = device.TryGetFeatureValue(CommonUsages.menuButton, out menuButtonState)
                                        && menuButtonState
                                        || tempState;
                }
                bool isPress = tempState != lastButtonState;
                if (isPress)
                {
                    menuButtonPress.Invoke(tempState);
                    lastButtonState = tempState;
                }
                return isPress;
            }
        }

        private void Awake()
        {
            if (menuButtonPress == null)
            {
                menuButtonPress = new MenuButtonEvent();
            }

            InputDeviceCharacteristics controllerCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devicesWithMenuBtn);
        }

        private bool QuitRequested
        {
            get
            {
                return GameManager.instance.vrDeviceManager.VRActive ?
                    (OculusRequested || Input.GetKey(quitKey))
                    : Input.GetKey(quitKey);
            }
        }

        [Tooltip("If true, game will quit after X frames.")]
        public bool QuitAfterX = false;
        [Tooltip("Number of frames to quit after.")]
        public int xFrames = 300;

        // Update is called once per frame
        void Update()
        {
            // Quit if the user requests or when the user requests
            if ((QuitAfterX && Time.frameCount >= xFrames) || QuitRequested)
            {
                Quit();
            }
        }

        private void Quit()
        {
#if UNITY_STANDALONE
            Application.Quit();
#endif
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}