using UnityEngine;
using UnityEngine.XR;
using C2M2.Utils;
namespace C2M2.Interaction.VR
{
    using Interaction.Signaling;
    /// <summary>
    /// Handles switching between VR and emulator modes
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
        private OVRManager ovrManager;

        private Transform leftEye;
        private Transform centerEye;
        private Transform rightEye;
        private Transform leftHand;
        private Transform rightHand;

        private Vector3 initialPlayerPositon;
        private Vector3 initialLeftEyePositon;
        private Vector3 initialCenterEyePositon;
        private Vector3 initialRightEyePositon;
        private Vector3 initialLeftHandPositon;
        private Quaternion initialLeftHandRotation;
        private Vector3 initialRightHandPositon;
        private Quaternion  initialRightHandRotation;

        private readonly KeyCode switchModeKey = KeyCode.Space;
        private readonly OVRInput.Button switchModeButton = OVRInput.Button.Any;

        public bool VRActive { get; set; } = false;
        public bool VRDevicePresent { get { return !VRDevice.Equals(string.Empty); } }
        public string VRDevice { get; private set; }

        private void Awake()
        {   
            Camera[] cameras = GetComponentsInChildren<Camera>();
            leftEye = cameras[0].transform;
            centerEye = cameras[1].transform;
            rightEye = cameras[2].transform;
            PublicOVRGrabber[] hands = GetComponentsInChildren<PublicOVRGrabber>();
            leftHand = hands[0].transform;
            rightHand = hands[1].transform;

            initialPlayerPositon = transform.position;
            initialLeftEyePositon = leftEye.position;
            initialCenterEyePositon = centerEye.position;
            initialRightEyePositon = rightEye.position;


            emulator = GetComponent<MovingOVRHeadsetEmulator>();
            emulatorMove = GetComponent<MovementController>();
            mouseSignaler = GetComponent<MouseEventSignaler>();
            playerController = GetComponent<OVRPlayerController>();
            ovrManager = GetComponentInChildren<OVRManager>();

            CheckForVRDevice();

            SwitchState(VRDevicePresent);
        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.Slash)) SwitchState(true); // temp for testing
            if (VRActive && Input.GetKey(switchModeKey)) SwitchState(false);
            else if (!VRActive && OVRInput.Get(switchModeButton)) //Won't work if disabling controllers so need an alternate
            {
                if (!VRDevicePresent) CheckForVRDevice();
                if (VRDevicePresent) SwitchState(true);
                else Debug.LogError("No VR Device Present");
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
            Debug.LogError("Mode switch to" + vrActive);

            VRActive = vrActive;

            XRSettings.enabled = vrActive;

            playerController.enabled = vrActive; //can most likely be permanently set to true?
            emulator.enabled = !vrActive;
            mouseSignaler.enabled = !vrActive;
            emulatorMove.enabled = !vrActive;

            ResetView();

            if (informationDisplayTV != null) informationDisplayTV.SetActive(vrActive);
            if (informationOverlay != null) informationOverlay.SetActive(!vrActive);

            // only enable oculus signalers if VR is enabled
            //OculusEventSignaler[] oculusSignalers = GetComponentsInChildren<OculusEventSignaler>();
            //foreach (OculusEventSignaler o in oculusSignalers)
            //{
                //o.enabled = vrActive;
            //}
        }

        private void ResetView()
        {
            transform.position = initialPlayerPositon;

            //Something is overriding this
            leftEye.position = Vector3.zero;
            leftEye.rotation = Quaternion.Euler(0, 0, 0);
            centerEye.position = Vector3.zero;
            centerEye.rotation = Quaternion.Euler(0, 0, 0);
            rightEye.position = Vector3.zero;
            rightEye.rotation = Quaternion.Euler(0,0,0);


            //leftHand.position = initialLeftHandPositon;
            //leftHand.rotation = initialLeftHandRotation;
            //rightHand.position = initialRightHandPositon;
            //rightHand.rotation = initialRightHandRotation;
            
            ovrManager.headPoseRelativeOffsetRotation = Vector3.zero;
        }
    }
}