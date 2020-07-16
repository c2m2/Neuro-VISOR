using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction.UI;
namespace C2M2.Visualization.VR
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary> Add movement functionality to Oculus HMD headset emulator,
    ///           add some additional small functionality like disabling hands in emulator mode,
    ///           allow slow movement </summary>
    ///
    /// <remarks>   Jacob Wells, 4/30/2020. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class MovingOVRHeadsetEmulator : OVRHeadsetEmulator
    {
        public KeyCode[] slowMoveKeys = new KeyCode[] { KeyCode.LeftShift, KeyCode.RightShift };

        public float moveSpeed = 0.5f;
        public float slowMoveSpeed = 0.05f;
        public float rotationSensitivity = 1.5f;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> Finds out if the user is pressing any of the slow movement activation keys </summary>
        /// <value> True if this is moving slow, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool IsMovingSlow
        {
            get
            {
                foreach (KeyCode key in slowMoveKeys)
                {
                    if (Input.GetKey(key)) return true;
                }
                return false;
            }
        }

        private void Awake()
        {

        }
        private void Start()
        {
            DisableAvatar();
            StartCoroutine(ResolveMovement());
            InitUI();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   
        ///     Coroutine to apply normal or slow movement speed to the obejct this script is attached to.
        /// </summary>
        ///
        /// <returns>  
        ///     yield return null makes this coroutine run once per game frame. 
        /// </returns>
        /// 
        /// <remarks>  
        ///     See https://docs.unity3d.com/Manual/ExecutionOrder.html for information about
        ///     order of execution for event functions in Unity.  
        /// </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public IEnumerator ResolveMovement()
        {
            // Original controls result in inversion
            while (true)
            {
                // Get frame movement speed
                float speed = IsMovingSlow ? slowMoveSpeed : moveSpeed;

                // Get user movement key input, apply speed and move player
                transform.Translate(
                    new Vector3(Input.GetAxis("Horizontal") * speed, 0.0f, Input.GetAxis("Vertical") * moveSpeed),
                    Camera.main.transform);

                // Wait until next frame
                yield return null;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Disables OVRAvatar body, head, and hand rendering </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DisableAvatar()
        {
            OvrAvatar avatar = GetComponentInChildren<OvrAvatar>();
            if (avatar != null)
            {
                avatar.EnableHands = false;
                avatar.EnableBody = false;
                avatar.EnableBase = false;
                avatar.EnableExpressive = false;
            }
        }
        private void InitUI()
        {
            GameObject controlUI = Instantiate(Resources.Load("Prefabs/ControlOverlay") as GameObject);
            List<KeyCode> keys = new List<KeyCode>(4);
            keys.AddRange(slowMoveKeys);
            keys.AddRange(activateKeys);
            keys.AddRange(pitchKeys);
            controlUI.GetComponent<ControlOverlay>().SetActivationKeys(keys.ToArray());
        }
    }
}
