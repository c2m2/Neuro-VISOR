using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Utils.DebugUtils.Actions
{
    public class EditorButtonPause : MonoBehaviour
    {
        public bool allowOculusPause = true;
        public OVRInput.Button oculusPauseButton = OVRInput.Button.PrimaryThumbstick;
        public bool allowKeyboardPause = true;
        public KeyCode keyboardPauseButton = KeyCode.Space;
        // Update is called once per frame
        void Update()
        {
            if (allowOculusPause)
            {
                if (OVRInput.GetDown(oculusPauseButton))
                {
                    Debug.Log("Editor Paused");
                    Debug.Break();
                }
            }
            if (allowKeyboardPause)
            {
                if (Input.GetKeyDown(keyboardPauseButton))
                {
                    if (OVRInput.GetDown(oculusPauseButton))
                    {
                        Debug.Log("Editor Paused");
                        Debug.Break();
                    }
                }
            }
        }
    }
}