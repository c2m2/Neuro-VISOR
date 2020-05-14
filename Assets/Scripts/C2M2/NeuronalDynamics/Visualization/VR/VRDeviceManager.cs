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
    [RequireComponent(typeof(OVRHeadsetEmulator))]
    [RequireComponent(typeof(MouseEventSignaler))]
    [RequireComponent(typeof(OVRPlayerController))]
    public class VRDeviceManager : MonoBehaviour
    {
        private void Awake()
        {
            ResolveDeviceState();
            Destroy(this);
        }
        private void ResolveDeviceState()
        {
            // If there is no VR device loaded, enable emulator
            bool emulatorEnabled = false;
            if (XRSettings.loadedDeviceName == "") emulatorEnabled = true;
            GetComponent<OVRHeadsetEmulator>().enabled = emulatorEnabled;
            GetComponent<MouseEventSignaler>().enabled = emulatorEnabled;
            GetComponent<OVRPlayerController>().enabled = !emulatorEnabled;
        }
    }
}