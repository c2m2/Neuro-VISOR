using UnityEngine;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Ensures "hand_left" and "hand_right" GameObjects exist under the tracking space and
    /// are driven by XRHandTracker. Replaces the Oculus Avatar SDK's runtime spawning of
    /// hand_left/hand_right children (which required an Oculus App ID).
    ///
    /// Attach this component to the transform that should parent the hands. Typically this
    /// is the OVRCameraRig's TrackingSpace (or the TrackingSpace's equivalent for your rig).
    /// The XR InputDevices API reports controller poses in tracking space, so placing the
    /// hands under the same transform keeps positions correct regardless of rig placement.
    ///
    /// If hand_left / hand_right already exist as children (e.g. from an existing prefab),
    /// this component reuses them and just ensures they have an XRHandTracker with the
    /// correct isLeftHand flag.
    /// </summary>
    public class XRHandBootstrapper : MonoBehaviour
    {
        [Tooltip("Optional visual prefab for the left hand. If null, whatever exists under hand_left is used as the visual.")]
        public GameObject leftHandModelPrefab;

        [Tooltip("Optional visual prefab for the right hand.")]
        public GameObject rightHandModelPrefab;

        [Tooltip("Model pose offset applied to the left hand visual.")]
        public Vector3 leftModelPositionOffset = Vector3.zero;
        public Vector3 leftModelRotationOffset = Vector3.zero;

        [Tooltip("Model pose offset applied to the right hand visual.")]
        public Vector3 rightModelPositionOffset = Vector3.zero;
        public Vector3 rightModelRotationOffset = Vector3.zero;

        [Tooltip("If true, hides the hand visuals until a tracking pose is received.")]
        public bool hideWhenUntracked = true;

        private void Awake()
        {
            EnsureHand(isLeft: true, leftHandModelPrefab, leftModelPositionOffset, leftModelRotationOffset);
            EnsureHand(isLeft: false, rightHandModelPrefab, rightModelPositionOffset, rightModelRotationOffset);
        }

        private void EnsureHand(bool isLeft, GameObject modelPrefab, Vector3 posOffset, Vector3 rotOffset)
        {
            string handName = isLeft ? "hand_left" : "hand_right";
            Transform existing = transform.Find(handName);

            GameObject handGO;
            if (existing != null)
            {
                handGO = existing.gameObject;
            }
            else
            {
                handGO = new GameObject(handName);
                handGO.transform.SetParent(transform, false);
            }

            var tracker = handGO.GetComponent<XRHandTracker>();
            if (tracker == null) tracker = handGO.AddComponent<XRHandTracker>();

            tracker.isLeftHand = isLeft;
            tracker.hideWhenUntracked = hideWhenUntracked;

            // Only set prefab if the tracker doesn't already have one wired in the editor.
            // This lets editor-assigned values take precedence when the bootstrapper is
            // added to a scene that already has configured trackers.
            if (tracker.handModelPrefab == null && modelPrefab != null)
            {
                tracker.handModelPrefab = modelPrefab;
                tracker.modelPositionOffset = posOffset;
                tracker.modelRotationOffset = rotOffset;
            }

            // Ensure the GameObject starts active so XRHandTracker.Update runs.
            if (!handGO.activeSelf) handGO.SetActive(true);
        }
    }
}
