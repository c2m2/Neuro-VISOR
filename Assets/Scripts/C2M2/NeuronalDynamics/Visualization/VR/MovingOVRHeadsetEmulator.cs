using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction.UI;
using C2M2.Utils;
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
        private MovementController controls = null;
        private GameObject controlUI;
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
            controls = GetComponent<MovementController>() ?? gameObject.AddComponent<MovementController>();
            controls.speed = speed;
        }

        private void OnEnable()
        {
            InitUI(true);
            StartCoroutine(CheckSpeed());

            if (controls != null) controls.enabled = true;
        }

        private void OnDisable()
        {
            InitUI(false);
            StopCoroutine(CheckSpeed());

            if (controls != null) controls.enabled = false;
        }

        private IEnumerator CheckSpeed()
        {
            while (true)
            {
                controls.speed = SlowMoving ? slowSpeed : speed;
                yield return null;
            }
        }

        private void InitUI(bool enable)
        {
            if (enable) {
                controlUI = Instantiate(Resources.Load("Prefabs/ControlOverlay") as GameObject);
                List<KeyCode> keys = new List<KeyCode>(4);
                keys.AddRange(slowMoveKeys);
                keys.AddRange(activateKeys);
                keys.AddRange(pitchKeys);
                controlUI.GetComponent<ControlOverlay>().SetActivationKeys(keys.ToArray());
            }
            else {
                if (controlUI != null) Destroy(controlUI);
            }
        }
    }
}
