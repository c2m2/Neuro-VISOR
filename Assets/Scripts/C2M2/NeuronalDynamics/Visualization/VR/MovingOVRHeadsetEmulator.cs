using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction.UI;
namespace C2M2.Interaction.VR
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary> Add movement functionality to Oculus HMD headset emulator,
    ///           add some additional small functionality like disabling hands in emulator mode,
    ///           allow slow movement </summary>
    ///
    /// <remarks>   Jacob Wells, 4/30/2020. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    [RequireComponent(typeof(MovementController))]
    public class MovingOVRHeadsetEmulator : OVRHeadsetEmulator
    {
        public KeyCode[] slowMoveKeys = new KeyCode[] { KeyCode.LeftShift, KeyCode.RightShift };

        public float speed = 0.1f;
        public float slowSpeed = 0.025f;
        private MovementController controller = null;
        private bool SlowMoving
        {
            get
            {
                bool slowMoving = false;
                foreach (KeyCode key in slowMoveKeys)
                {
                    if (Input.GetKey(key))
                        slowMoving = true;
                }
                return slowMoving;
            }
        }

        private void Awake()
        {
            controller = GetComponent<MovementController>() ?? gameObject.AddComponent<MovementController>();
            controller.speed = speed;
        }

        private void Start()
        {
            DisableAvatar();
            InitUI();
            StartCoroutine(CheckSpeed());
        }
        private IEnumerator CheckSpeed()
        {
            while (true)
            {
                controller.speed = SlowMoving ? slowSpeed : speed;
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
        private void OnEnable()
        {
            if(controller != null)
                controller.enabled = true;
        }
        private void OnDisable()
        {
            if (controller != null)
                controller.enabled = false;
        }
    }
}
