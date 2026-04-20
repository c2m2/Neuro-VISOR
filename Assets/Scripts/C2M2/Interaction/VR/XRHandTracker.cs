using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Drives a hand GameObject's position and rotation from an XR controller pose.
    /// Replaces the Oculus Avatar SDK hand visual (OvrAvatarHand) which required an App ID.
    ///
    /// This script is intentionally engine-agnostic: it uses Unity's XR InputDevices API,
    /// so it works with any active XR backend (OpenXR, Oculus XR Plugin, etc.) without
    /// requiring an Oculus App ID or Avatar SDK initialization.
    ///
    /// The GameObject this is attached to is renamed to "hand_left" or "hand_right" on
    /// Awake so that existing GameObject.Find("hand_left"/"hand_right") calls keep working
    /// (see OculusEventSignaler.SearchForHand).
    /// </summary>
    public class XRHandTracker : MonoBehaviour
    {
        [Tooltip("Which hand this tracker represents. Controls both XR node selection and GameObject naming.")]
        public bool isLeftHand = false;

        [Tooltip("Optional: a visual model (mesh) to parent under this hand. If null, no visual is created and whatever children already exist are used.")]
        public GameObject handModelPrefab;

        [Tooltip("Local position offset applied to the instantiated hand model.")]
        public Vector3 modelPositionOffset = Vector3.zero;

        [Tooltip("Local rotation offset (Euler, degrees) applied to the instantiated hand model.")]
        public Vector3 modelRotationOffset = Vector3.zero;

        [Tooltip("If true, the hand's renderers are disabled until a valid tracking pose is received. Prevents the hand flashing at origin on start. Default is false so the hand stays visible regardless of transient tracking gaps (Meta Quest Link sometimes drops the pose for a frame).")]
        public bool hideWhenUntracked = false;

        private XRNode HandNode => isLeftHand ? XRNode.LeftHand : XRNode.RightHand;
        private string HandName => isLeftHand ? "hand_left" : "hand_right";

        private readonly List<InputDevice> deviceBuffer = new List<InputDevice>();
        private GameObject spawnedModel;
        private Renderer[] modelRenderers;
        private bool hasPose;

        private void Awake()
        {
            // Rename for legacy GameObject.Find() lookups (OculusEventSignaler).
            if (gameObject.name != HandName) gameObject.name = HandName;
        }

        private void Start()
        {
            if (handModelPrefab != null)
            {
                spawnedModel = Instantiate(handModelPrefab, transform);
                spawnedModel.transform.localPosition = modelPositionOffset;
                spawnedModel.transform.localEulerAngles = modelRotationOffset;
                // Match the parent hand GameObject's layer so the camera doesn't cull the model.
                // hand_left / hand_right are placed on the "Player" layer in the Controller prefab.
                ApplyLayerRecursively(spawnedModel, gameObject.layer);
                modelRenderers = spawnedModel.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                modelRenderers = GetComponentsInChildren<Renderer>(true);
            }

            // SkinnedMeshRenderer bounds go stale when the parent GameObject is toggled
            // inactive/active (as OculusEventSignaler does when entering/leaving raycast mode),
            // which can leave the hand culled after reactivation. updateWhenOffscreen forces
            // per-frame bounds recomputation from the current pose so it renders correctly.
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }

            if (hideWhenUntracked) SetVisible(false);
            else SetVisible(true);
        }

        private static void ApplyLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) ApplyLayerRecursively(child.gameObject, layer);
        }

        private void OnEnable()
        {
            // Re-assert visibility on every activation. OculusEventSignaler.StaticHandSetActive
            // toggles this GameObject active/inactive each time raycast mode is entered or
            // exited; without this, a stale hasPose flag (or hideWhenUntracked=true) can leave
            // the hand invisible until the next tracking update.
            if (!hideWhenUntracked) SetVisible(true);
        }

        private void Update()
        {
            InputDevices.GetDevicesAtXRNode(HandNode, deviceBuffer);
            if (deviceBuffer.Count == 0)
            {
                if (hasPose && hideWhenUntracked) SetVisible(false);
                hasPose = false;
                return;
            }

            var device = deviceBuffer[0];
            bool gotPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
            bool gotRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);
            if (!gotPosition && !gotRotation)
            {
                if (hasPose && hideWhenUntracked) SetVisible(false);
                hasPose = false;
                return;
            }

            // Poses from XR InputDevices are reported in the tracking space. Using localPosition/Rotation
            // keeps the hand correct regardless of how the tracking origin (e.g. OVRCameraRig/TrackingSpace)
            // is positioned in world space.
            if (gotPosition) transform.localPosition = pos;
            if (gotRotation) transform.localRotation = rot;

            if (!hasPose && hideWhenUntracked) SetVisible(true);
            hasPose = true;
        }

        private void SetVisible(bool visible)
        {
            if (modelRenderers == null) return;
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null) modelRenderers[i].enabled = visible;
            }
        }
    }
}
