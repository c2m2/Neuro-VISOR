using System.Collections;
using UnityEngine;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Creates controller-visual GameObjects named "hand_left" and "hand_right"
    /// as children of OVRCameraRig's hand anchors.
    ///
    /// Serves two purposes:
    ///   1. Provide a visual controller indicator while the Avatar SDK is absent.
    ///   2. Satisfy OculusEventSignaler.SearchForHand() so the static-hand toggle works.
    ///
    /// Loads OVRControllerPrefab from Resources, instantiates it under each anchor,
    /// and force-activates the correct controller sub-model using the same headset
    /// detection as OVRControllerHelper (Rift / Quest+RiftS / Quest 2).
    /// Falls back to simple primitives if the prefab cannot be loaded.
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

            StartCoroutine(BuildHandObject(rig.leftHandAnchor,  "hand_left",  isLeft: true));
            StartCoroutine(BuildHandObject(rig.rightHandAnchor, "hand_right", isLeft: false));
        }

        private IEnumerator BuildHandObject(Transform anchor, string handName, bool isLeft)
        {
            if (anchor == null) yield break;
            if (anchor.Find(handName) != null) yield break; // already exists

            var hand = new GameObject(handName);
            hand.transform.SetParent(anchor, false);
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localRotation = Quaternion.identity;
            hand.transform.localScale    = Vector3.one;

            var prefab = Resources.Load<GameObject>("Prefabs/OVRControllerPrefab");
            if (prefab != null)
            {
                var ctrl = Instantiate(prefab, hand.transform, false);
                ctrl.name = "ControllerModel";
                ctrl.transform.localPosition = Vector3.zero;
                ctrl.transform.localRotation = Quaternion.identity;
                ctrl.transform.localScale    = Vector3.one;

                // Wait one frame so OVRControllerHelper.Start() runs and hides all sub-models
                yield return null;

                var helper = ctrl.GetComponent<OVRControllerHelper>();
                if (helper != null)
                {
                    helper.enabled = false; // stop Update() from toggling visibility via OVRInput
                    ForceControllerModelActive(helper, isLeft);
                }
            }
            else
            {
                BuildPrimitive(hand.transform);
            }
        }

        /// <summary>
        /// Mirrors OVRControllerHelper's headset-detection logic so the correct
        /// physical controller model is shown for whatever headset is connected.
        /// </summary>
        private static void ForceControllerModelActive(OVRControllerHelper helper, bool isLeft)
        {
            OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();

            GameObject model;
            switch (headset)
            {
                case OVRPlugin.SystemHeadset.Rift_CV1:
                    model = isLeft ? helper.m_modelOculusTouchRiftLeftController
                                   : helper.m_modelOculusTouchRiftRightController;
                    break;
                case OVRPlugin.SystemHeadset.Oculus_Quest_2:
                    model = isLeft ? helper.m_modelOculusTouchQuest2LeftController
                                   : helper.m_modelOculusTouchQuest2RightController;
                    break;
                default: // Quest, Quest Pro, Quest 3, RiftS, and all PC-Link variants
                    model = isLeft ? helper.m_modelOculusTouchQuestAndRiftSLeftController
                                   : helper.m_modelOculusTouchQuestAndRiftSRightController;
                    break;
            }

            if (model != null)
                model.SetActive(true);
            else
                Debug.LogWarning("[XRHandVisualizer] No matching controller sub-model found in OVRControllerPrefab.");
        }

        private static void BuildPrimitive(Transform parent)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "GripBody";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, -0.03f, 0.01f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale    = new Vector3(0.035f, 0.055f, 0.035f);
            Destroy(body.GetComponent<Collider>());

            var guard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            guard.name = "TriggerGuard";
            guard.transform.SetParent(parent, false);
            guard.transform.localPosition = new Vector3(0f, 0.015f, 0.025f);
            guard.transform.localScale    = new Vector3(0.03f, 0.025f, 0.04f);
            Destroy(guard.GetComponent<Collider>());
        }
    }
}
