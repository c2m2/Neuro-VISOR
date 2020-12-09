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
        public GameObject informationOverlay = null;
        public GameObject informationDisplayTV = null;
        private MovingOVRHeadsetEmulator emulator;
        private MouseEventSignaler mouseSignaler;
        private OVRPlayerController playerController;
        private MovementController emulatorMove;

        public bool VrIsActive { get { return !vrDevice.Equals("null"); } }
        public string vrDevice { get; private set; } = "null";

        private void Awake()
        {
            // Get VR device (or lack of one)
            if (UnityEngine.XR.XRDevice.model.Equals(string.Empty)) vrDevice = "null";
            else vrDevice = XRDevice.model;

            emulator = GetComponent<MovingOVRHeadsetEmulator>();
            emulatorMove = GetComponent<MovementController>();
            mouseSignaler = GetComponent<MouseEventSignaler>();
            playerController = GetComponent<OVRPlayerController>();

            SwitchState(VrIsActive);

            if (Application.isPlaying)
            {
                if (VrIsActive) { Debug.Log("Running in VR mode on device [" + vrDevice + "]"); }
                else Debug.Log("Running in emulator mode.");
            }
        }

        private void SwitchState(bool vrActive)
        {
            if (informationDisplayTV != null) informationDisplayTV.SetActive(vrActive);
            playerController.enabled = vrActive;

            emulator.enabled = !vrActive;
            mouseSignaler.enabled = !vrActive;
            emulatorMove.enabled = !vrActive;
            if (informationOverlay != null) informationOverlay.SetActive(!vrActive);


            // only enable oculus signalers if player controller is enabled
            OculusEventSignaler[] oculusSignalers = GetComponentsInChildren<OculusEventSignaler>();
            foreach (OculusEventSignaler o in oculusSignalers)
            {
                o.enabled = vrActive;
            }
        }
    }
}