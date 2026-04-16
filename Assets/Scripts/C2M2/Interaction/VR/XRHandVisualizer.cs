using System.Collections;
using UnityEngine;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Spawns skeletal hand-mesh GameObjects ("hand_left" / "hand_right") under
    /// OVRCameraRig's hand anchors.  Visual assets are injected by VRDeviceManager
    /// via SetHandAssets(); falls back to simple primitives when assets are absent.
    ///
    /// Using raw FBX model GameObjects (l_hand_skeletal_lowres / r_hand_skeletal_lowres)
    /// with HandMaterial gives the standard phantom-white-hand appearance without any
    /// dependency on OVRInput or OVRPlugin hand-tracking state.
    /// </summary>
    public class XRHandVisualizer : MonoBehaviour
    {
        private GameObject _leftModel;
        private GameObject _rightModel;
        private Material   _handMaterial;

        /// <summary>Called by VRDeviceManager immediately after AddComponent.</summary>
        public void SetHandAssets(GameObject leftModel, GameObject rightModel, Material handMaterial)
        {
            _leftModel    = leftModel;
            _rightModel   = rightModel;
            _handMaterial = handMaterial;
        }

        private void Start()
        {
            OVRCameraRig rig = GetComponentInChildren<OVRCameraRig>();
            if (rig == null) rig = FindFirstObjectByType<OVRCameraRig>();

            if (rig == null)
            {
                Debug.LogError("[XRHandVisualizer] No OVRCameraRig found in scene.");
                return;
            }

            // Resources fallback paths (require files to be copied into Assets/Resources/).
            if (_leftModel == null)
                _leftModel = Resources.Load<GameObject>("Models/l_hand_skeletal_lowres");
            if (_rightModel == null)
                _rightModel = Resources.Load<GameObject>("Models/r_hand_skeletal_lowres");
            if (_handMaterial == null)
                _handMaterial = Resources.Load<Material>("Materials/HandMaterial");

            if (_leftModel == null || _rightModel == null)
                Debug.LogWarning("[XRHandVisualizer] Hand models not found – falling back to primitives.");

            StartCoroutine(BuildHandObject(rig.leftHandAnchor,  "hand_left",  _leftModel,  isLeft: true));
            StartCoroutine(BuildHandObject(rig.rightHandAnchor, "hand_right", _rightModel, isLeft: false));
        }

        private IEnumerator BuildHandObject(Transform anchor, string handName, GameObject model, bool isLeft)
        {
            if (anchor == null) yield break;
            if (anchor.Find(handName) != null) yield break;

            var hand = new GameObject(handName);
            hand.transform.SetParent(anchor, false);
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localRotation = Quaternion.identity;
            hand.transform.localScale    = Vector3.one;

            if (model != null)
            {
                var instance = Instantiate(model, hand.transform, false);
                instance.name = "HandModel";
                instance.transform.localPosition = Vector3.zero;
                // Match the rotation used by the original CustomHandLeft/Right prefab roots:
                // +90° Z for left hand, -90° Z for right hand.
                instance.transform.localRotation = Quaternion.Euler(0f, 0f, isLeft ? 90f : -90f);
                instance.transform.localScale    = Vector3.one;

                if (_handMaterial != null)
                {
                    foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        var mats = new Material[smr.sharedMaterials.Length];
                        for (int i = 0; i < mats.Length; i++)
                            mats[i] = _handMaterial;
                        smr.sharedMaterials = mats;
                    }
                }

                Debug.Log($"[XRHandVisualizer] Built '{handName}' from model '{model.name}'.");
            }
            else
            {
                BuildPrimitive(hand.transform);
                Debug.Log($"[XRHandVisualizer] Built '{handName}' as primitive fallback.");
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
