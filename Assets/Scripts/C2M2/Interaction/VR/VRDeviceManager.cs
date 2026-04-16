using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Handles switching between VR and emulator modes
    /// </summary>
    public class VRDeviceManager : MonoBehaviour
    {
        public GameObject desktopControlScheme = null;
        public GameObject vrControlScheme = null;

        private GameObject vrController;
        private GameObject desktopController;

        private Camera[] vrCameras;
        private Camera desktopCamera;

        private readonly KeyCode switchModeKey = KeyCode.Space;

        public bool VRActive { get; set; } = false;
        public bool VRDevicePresent { get { return !VRDevice.Equals(string.Empty); } }
        public string VRDevice { get; private set; }

        private void Awake()
        {
            vrController = transform.GetChild(0).gameObject;
            desktopController = transform.GetChild(1).gameObject;
            vrCameras = vrController.GetComponentsInChildren<Camera>();
            desktopCamera = desktopController.GetComponent<Camera>();
            StartCoroutine(DelayedVRCheck());
        }

        private IEnumerator DelayedVRCheck()
        {
            // Wait for OpenXR to fully initialize
            yield return new WaitForSeconds(2f);
            CheckForVRDevice();
            SwitchState(VRDevicePresent);
        }

        public void Update()
        {
            if (Input.GetKeyDown(switchModeKey)) {
                if (VRActive) SwitchState(false);
                else SwitchState(true);
            }
        }

        private void CheckForVRDevice()
        {
            // Get VR device (or lack of one) via InputDevices API
            var headDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.Head, headDevices);
            if (headDevices.Count > 0)
            {
                VRDevice = headDevices[0].name;
                Debug.Log("VR Device Name: " + VRDevice);
            }
            else
            {
                VRDevice = string.Empty;
                Debug.Log("No VR device found.");
            }
        }

        private void SwitchState(bool vrActive)
        {
            VRActive = vrActive;

            // XRSettings.enabled is removed in Unity 6; XR is managed by XR Plugin Management

            vrController.SetActive(vrActive);
            desktopController.SetActive(!vrActive);

            // Enable information displays
            if (vrControlScheme != null) vrControlScheme.SetActive(vrActive);
            if (desktopControlScheme != null) desktopControlScheme.SetActive(!vrActive);

            // Enable controllers
            OculusEventSignaler[] oculusSignalers = GetComponentsInChildren<OculusEventSignaler>();
            foreach (OculusEventSignaler o in oculusSignalers)
            {
                o.enabled = vrActive;
            }

            // Enable proper cameras
            foreach (Camera cam in vrCameras)
            {
                cam.enabled = vrActive;
            }
            desktopCamera.enabled = !vrActive;
        }
    }
}