using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using C2M2.Interaction;
namespace C2M2.Interaction.VR
{
    using Interaction.Signaling;
    using Visualization.VR;
    /// <summary>
    /// Make sure that a VR device is loaded before using OVRPlayerController.
    /// If none is loaded, add VR emulation tools
    /// </summary>
    [RequireComponent(typeof(MovingOVRHeadsetEmulator))]
    [RequireComponent(typeof(MouseEventSignaler))]
    [RequireComponent(typeof(OVRPlayerController))]
    [RequireComponent(typeof(MovementController))]
    [ExecuteInEditMode]
    public class VRDeviceManager : MonoBehaviour
    {
        public bool vrIsActive { get { return playerController.enabled; } }
        public GameObject informationOverlay = null;
        public GameObject informationDisplayTV = null;
        private MovingOVRHeadsetEmulator emulator;
        private MouseEventSignaler mouseSignaler;
        private OVRPlayerController playerController;
        private MovementController emulatorMove;
        private bool prev;

        private void Awake()
        {
            emulator = GetComponent<MovingOVRHeadsetEmulator>();
            emulatorMove = GetComponent<MovementController>();
            mouseSignaler = GetComponent<MouseEventSignaler>();
            playerController = GetComponent<OVRPlayerController>();
            prev = vrIsActive;
            if (Application.isPlaying)
            {
                if (vrIsActive) { Debug.Log("Running in VR mode."); }
                else Debug.Log("Running in emulator mode.");
            }
        }
        private void Update()
        {
            if (prev != vrIsActive && !Application.isPlaying)
            {
                emulator.enabled = !vrIsActive;
                mouseSignaler.enabled = !vrIsActive;
                emulatorMove.enabled = !vrIsActive;
                if (informationOverlay != null) informationOverlay.SetActive(!vrIsActive);

                if (informationDisplayTV != null) informationDisplayTV.SetActive(vrIsActive);

                // only enable oculus signalers if player controller is enabled
                OculusEventSignaler[] oculusSignalers = GetComponentsInChildren<OculusEventSignaler>();
                foreach (OculusEventSignaler o in oculusSignalers)
                {
                    o.enabled = vrIsActive;
                }
                prev = vrIsActive;
            }
        }
    }
}