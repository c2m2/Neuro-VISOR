using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Handles switching between VR and emulator modes.
    /// When VR is activated it also bootstraps XRInputBridge (controller input)
    /// and XRHandVisualizer (hand-mesh creation).
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

        private bool xrSystemsInitialized = false;

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

            if (vrActive) EnsureXRSystemsInitialized();
        }

        /// <summary>
        /// One-time setup of XRInputBridge and XRHandVisualizer when VR mode first activates.
        /// </summary>
        private void EnsureXRSystemsInitialized()
        {
            if (xrSystemsInitialized) return;
            xrSystemsInitialized = true;

            // Ensure the input bridge singleton exists.
            // XRInputBridge.Instance auto-creates itself, but touching it here guarantees
            // it's alive before any other component queries it this frame.
            _ = XRInputBridge.Instance;

            // Create hand visuals (hand_left / hand_right) under the OVR anchors.
            // Inject the controller prefab reference directly so the visualizer never
            // depends on Resources.Load succeeding (which requires proper import).
            if (vrController.GetComponentInChildren<XRHandVisualizer>() == null)
            {
                var viz = vrController.AddComponent<XRHandVisualizer>();
#if UNITY_EDITOR
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Oculus/VR/Prefabs/OVRControllerPrefab.prefab");
                viz.SetControllerPrefab(prefab);
#endif
            }
        }
    }
}