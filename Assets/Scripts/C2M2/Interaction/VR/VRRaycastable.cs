using UnityEngine;

namespace C2M2.Interaction.VR
{
    using Utils;
    [RequireComponent(typeof(MeshFilter))]
    public abstract class VRRaycastable<ColliderSourceT> : MonoBehaviour
    {
        protected ColliderSourceT source;
        public abstract ColliderSourceT GetSource();
        public abstract void SetSource(ColliderSourceT source);

        private void Awake()
        {
            // Let children initialize
            OnAwake();
        }
        protected abstract void OnAwake();

        /// <summary> Instantiate child object & set its layer to "Raycast" </summary>
        /// <returns> The child object that was created. </returns>
        protected GameObject BuildChildObject(Transform parent, string name = "RaycastTarget", string layer = "Raycast")
        {
            GameObject childObject = new GameObject(name);
            childObject.layer = LayerMask.NameToLayer(layer);
            childObject.transform.parent = parent;
            childObject.transform.position = Vector3.zero;
            childObject.transform.eulerAngles = Vector3.zero;
            childObject.transform.localScale = Vector3.one;

            return childObject;
        }
        /// <summary> Add a rigidbody to the raycast child object and change its settings </summary>
        /// <returns> The Rigidbody that was created. </returns>
        protected Rigidbody BuildRigidBody(GameObject raycastTargetObject)
        {
            Rigidbody rb = raycastTargetObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.angularDrag = Mathf.Infinity;
            rb.drag = Mathf.Infinity;
            return rb;
        }

    }
}
