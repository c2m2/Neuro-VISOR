using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Drives the attached transform from an XR controller pose using Unity's XR InputDevices
    /// API. Intended for use on LeftHandAnchor / RightHandAnchor under an OVRCameraRig when
    /// the legacy Oculus OVRPlugin stack is non-functional (e.g. Unity 6 + OpenXR with the
    /// Oculus Integration SDK installed but OVRPlugin.dll failing to load).
    ///
    /// Poses are written to localPosition/localRotation, matching how OVRCameraRig updates
    /// its anchors relative to TrackingSpace. Downstream code that reads the anchor transform
    /// (OVRGrabber, raycast signalers, grab volumes) then sees correct poses without any
    /// further changes.
    /// </summary>
    public class XRControllerPoseDriver : MonoBehaviour
    {
        public enum Hand { Left, Right }

        [Tooltip("Which XR node to sample the pose from.")]
        public Hand hand = Hand.Right;

        [Tooltip("Euler rotation applied after the controller rotation. Used to convert the OpenXR grip pose to the aim/pointer convention the existing hand/pointer models expect. For a Meta Touch controller under OpenXR in Unity 6, (45, 0, 0) pitches the grip pose forward ~45 degrees to align the static raycast hand with the held-controller pointer direction.")]
        public Vector3 rotationOffset = new Vector3(45f, 0f, 0f);

        private XRNode Node => hand == Hand.Left ? XRNode.LeftHand : XRNode.RightHand;
        private readonly List<InputDevice> deviceBuffer = new List<InputDevice>();

        private void Update()
        {
            InputDevices.GetDevicesAtXRNode(Node, deviceBuffer);
            if (deviceBuffer.Count == 0) return;

            var device = deviceBuffer[0];
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                transform.localPosition = pos;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                transform.localRotation = rot * Quaternion.Euler(rotationOffset);
        }
    }
}
