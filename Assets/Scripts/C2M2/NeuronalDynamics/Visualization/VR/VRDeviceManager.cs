using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using C2M2.Interaction;
namespace C2M2.Visualization.VR
{
    using Interaction.Signaling;
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
        public GameObject fpsOverlay = null;
        public GameObject fpsTVScreen = null;
        private MovingOVRHeadsetEmulator emulator;
        private MouseEventSignaler signaler;
        private OVRPlayerController playerController;
        private MovementController emulatorMove;

        private void Update()
        {
            if (Application.isPlaying) Destroy(this);

            emulator = GetComponent<MovingOVRHeadsetEmulator>();
            emulatorMove = GetComponent<MovementController>();
            signaler = GetComponent<MouseEventSignaler>();
            playerController = GetComponent<OVRPlayerController>();
            

            emulator.enabled = !playerController.enabled;
            signaler.enabled = emulator.enabled;
            emulatorMove.enabled = emulator.enabled;
            if (fpsOverlay != null) fpsOverlay.SetActive(emulator.enabled);
            if (fpsTVScreen != null) fpsTVScreen.SetActive(playerController.enabled);
        }
    }
}