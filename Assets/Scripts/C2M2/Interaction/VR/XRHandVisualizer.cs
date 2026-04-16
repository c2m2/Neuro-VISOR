using System.Collections;
using UnityEngine;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Creates controller-visual GameObjects named "hand_left" and "hand_right"
    /// as children of OVRCameraRig's hand anchors, using OVRControllerPrefab.
    ///
    /// VRDeviceManager injects the controller prefab reference via
    /// SetControllerPrefab() before Start() runs. Falls back to simple primitives
    /// when no prefab is available.
    /// </summary>
    public class XRHandVisualizer : MonoBehaviour
    {
        private GameObject _controllerPrefab;

        /// <summary>Called by VRDeviceManager immediately after AddComponent.</summary>
        public void SetControllerPrefab(GameObject prefab) => _controllerPrefab = prefab;

        private void Start()
        {
            OVRCameraRig rig = GetComponentInChildren<OVRCameraRig>();
            if (rig == null) rig = FindFirstObjectByType<OVRCameraRig>();

            if (rig == null)
            {
                Debug.LogError("[XRHandVisualizer] No OVRCameraRig found in scene.");
                return;
            }

            if (_controllerPrefab == null)
                _controllerPrefab = Resources.Load<GameObject>("Prefabs/OVRControllerPrefab");

            if (_controllerPrefab == null)
                Debug.LogWarning("[XRHandVisualizer] OVRControllerPrefab not found – falling back to primitives.");

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

            if (_controllerPrefab != null)
            {
                var ctrl = Instantiate(_controllerPrefab, hand.transform, false);
                ctrl.name = "ControllerModel";
                ctrl.transform.localPosition = Vector3.zero;
                ctrl.transform.localRotation = Quaternion.identity;
                ctrl.transform.localScale    = Vector3.one;

                // Wait two frames: frame 1 for OVRControllerHelper.Awake(),
                // frame 2 for OVRControllerHelper.Start() which hides all sub-models.
                yield return null;
                yield return null;

                var helper = ctrl.GetComponent<OVRControllerHelper>();
                if (helper != null)
                {
                    helper.enabled = false; // stop Update() from overriding visibility
                    ForceControllerModelActive(helper, isLeft);
                }
                else
                {
                    Debug.LogWarning("[XRHandVisualizer] OVRControllerHelper not found on instantiated prefab.");
                }
            }
            else
            {
                BuildPrimitive(hand.transform);
            }
        }

        /// <summary>
        /// Mirrors OVRControllerHelper's headset-detection switch so the correct
        /// physical controller model is shown for whatever headset is connected.
        /// </summary>
        private static void ForceControllerModelActive(OVRControllerHelper helper, bool isLeft)
        {
            OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
            Debug.Log($"[XRHandVisualizer] Headset type: {headset}, isLeft: {isLeft}");

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
                default:
                    model = isLeft ? helper.m_modelOculusTouchQuestAndRiftSLeftController
                                   : helper.m_modelOculusTouchQuestAndRiftSRightController;
                    break;
            }

            if (model != null)
            {
                model.SetActive(true);
                Debug.Log($"[XRHandVisualizer] Activated controller model '{model.name}' for {(isLeft ? "left" : "right")} hand.");
            }
            else
            {
                Debug.LogWarning($"[XRHandVisualizer] No matching controller sub-model found (headset={headset}, isLeft={isLeft}).");
            }
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
