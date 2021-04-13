using UnityEngine;
using UnityEngine.XR;
using C2M2.Utils;
namespace C2M2.Interaction.VR
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
        public GameObject informationOverlay = null;
        public GameObject informationDisplayTV = null;
        private MovingOVRHeadsetEmulator emulator;
        private MouseEventSignaler mouseSignaler;
        private OVRPlayerController playerController;
        private MovementController emulatorMove;

        private readonly KeyCode switchModeKey = KeyCode.Space;
        private readonly OVRInput.Button switchModeButton = OVRInput.Button.Any;

        public bool VRActive { get; set; } = false;
        public bool VRDevicePresent { get { return !VRDevice.Equals(string.Empty); } }
        public string VRDevice { get; private set; }

        private void Awake()
        {
            CheckForVRDevice();

            emulator = GetComponent<MovingOVRHeadsetEmulator>();
            emulatorMove = GetComponent<MovementController>();
            mouseSignaler = GetComponent<MouseEventSignaler>();
            playerController = GetComponent<OVRPlayerController>();

            SwitchState(VRDevicePresent);
        }

        public void Update()
        {
            if (VRActive && Input.GetKey(switchModeKey)) SwitchState(false);
            else if (!VRActive && OVRInput.Get(switchModeButton)) SwitchState(true);
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
            Debug.LogError("Swtich to" + vrActive);
            VRActive = vrActive;

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