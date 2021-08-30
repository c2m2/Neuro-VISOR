using UnityEngine;
using UnityEngine.XR;

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
            // Get VR device (or lack of one)
            // Note: in Unity 2019.4 XRDevice.model is obsolete but still works.
            InputDevice inputDevice = new InputDevice();
            Debug.Log("VR Device Name: " + inputDevice.name);
            VRDevice = XRDevice.model;
        }

        private void SwitchState(bool vrActive)
        {
            VRActive = vrActive;

            XRSettings.enabled = vrActive;

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