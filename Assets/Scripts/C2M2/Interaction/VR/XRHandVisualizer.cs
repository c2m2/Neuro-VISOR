using System.Collections;
using System.Reflection;
using UnityEngine;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Creates phantom-hand GameObjects named "hand_left" and "hand_right"
    /// as children of OVRCameraRig's hand anchors using OVRHandPrefab.
    ///
    /// VRDeviceManager injects the prefab reference via SetHandPrefab() before
    /// Start() runs. Falls back to simple primitives when no prefab is available.
    /// </summary>
    public class XRHandVisualizer : MonoBehaviour
    {
        private GameObject _handPrefab;

        /// <summary>Called by VRDeviceManager immediately after AddComponent.</summary>
        public void SetHandPrefab(GameObject prefab) => _handPrefab = prefab;

        private void Start()
        {
            OVRCameraRig rig = GetComponentInChildren<OVRCameraRig>();
            if (rig == null) rig = FindFirstObjectByType<OVRCameraRig>();

            if (rig == null)
            {
                Debug.LogError("[XRHandVisualizer] No OVRCameraRig found in scene.");
                return;
            }

            if (_handPrefab == null)
                _handPrefab = Resources.Load<GameObject>("Prefabs/OVRHandPrefab");

            if (_handPrefab == null)
                Debug.LogWarning("[XRHandVisualizer] OVRHandPrefab not found – falling back to primitives.");

            StartCoroutine(BuildHandObject(rig.leftHandAnchor,  "hand_left",  isLeft: true));
            StartCoroutine(BuildHandObject(rig.rightHandAnchor, "hand_right", isLeft: false));
        }

        private IEnumerator BuildHandObject(Transform anchor, string handName, bool isLeft)
        {
            if (anchor == null) yield break;
            if (anchor.Find(handName) != null) yield break;

            var hand = new GameObject(handName);
            hand.transform.SetParent(anchor, false);
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localRotation = Quaternion.identity;
            hand.transform.localScale    = Vector3.one;

            if (_handPrefab != null)
            {
                var instance = Instantiate(_handPrefab, hand.transform, false);
                instance.name = "OVRHand";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale    = Vector3.one;

                // Set HandType via reflection — the field is private on OVRHand but
                // OVRSkeleton/OVRMesh read it through the IOVRSkeletonDataProvider /
                // IOVRMeshDataProvider interfaces that OVRHand implements, so one
                // reflection call is sufficient.
                var ovrHand = instance.GetComponent<OVRHand>();
                if (ovrHand != null)
                {
                    var field = typeof(OVRHand).GetField("HandType",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        OVRHand.Hand handType = isLeft ? OVRHand.Hand.HandLeft : OVRHand.Hand.HandRight;
                        field.SetValue(ovrHand, handType);
                        Debug.Log($"[XRHandVisualizer] Set HandType={handType} on {handName}.");
                    }
                    else
                    {
                        Debug.LogWarning("[XRHandVisualizer] Could not find HandType field on OVRHand via reflection.");
                    }
                }
                else
                {
                    Debug.LogWarning("[XRHandVisualizer] OVRHand component not found on instantiated prefab.");
                }

                // Wait one frame for OVRHand.Start() to initialize with the new HandType.
                yield return null;
            }
            else
            {
                BuildPrimitive(hand.transform);
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
