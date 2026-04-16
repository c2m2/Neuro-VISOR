using UnityEngine;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Creates simple controller-visual GameObjects named "hand_left" and "hand_right"
    /// as children of OVRCameraRig's hand anchors.
    ///
    /// These objects serve two purposes:
    ///   1. Provide a visual indicator of controller position while the Avatar SDK is absent.
    ///   2. Satisfy the OculusEventSignaler coroutine that searches for "hand_left"/"hand_right"
    ///      by name, so the static-hand / raycast-mode toggle works correctly.
    ///
    /// The visuals are intentionally minimal (small sphere + capsule). Replace the child
    /// meshes with proper controller models (e.g. via OVRControllerHelper) when available.
    /// </summary>
    public class XRHandVisualizer : MonoBehaviour
    {
        private void Start()
        {
            OVRCameraRig rig = GetComponentInChildren<OVRCameraRig>();
            if (rig == null) rig = FindFirstObjectByType<OVRCameraRig>();

            if (rig == null)
            {
                Debug.LogError("[XRHandVisualizer] No OVRCameraRig found in scene.");
                return;
            }

            BuildHandObject(rig.leftHandAnchor,  "hand_left");
            BuildHandObject(rig.rightHandAnchor, "hand_right");
        }

        private static void BuildHandObject(Transform anchor, string handName)
        {
            if (anchor == null) return;
            if (anchor.Find(handName) != null) return; // already exists

            var hand = new GameObject(handName);
            hand.transform.SetParent(anchor, false);
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localRotation = Quaternion.identity;
            hand.transform.localScale    = Vector3.one;

            // ── Grip body (capsule) ──────────────────────────────────────────────
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "GripBody";
            body.transform.SetParent(hand.transform, false);
            body.transform.localPosition = new Vector3(0f, -0.03f, 0.01f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale    = new Vector3(0.035f, 0.055f, 0.035f);
            Destroy(body.GetComponent<Collider>());

            // ── Trigger guard (sphere) ───────────────────────────────────────────
            var guard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            guard.name = "TriggerGuard";
            guard.transform.SetParent(hand.transform, false);
            guard.transform.localPosition = new Vector3(0f, 0.015f, 0.025f);
            guard.transform.localScale    = new Vector3(0.03f, 0.025f, 0.04f);
            Destroy(guard.GetComponent<Collider>());
        }
    }
}
