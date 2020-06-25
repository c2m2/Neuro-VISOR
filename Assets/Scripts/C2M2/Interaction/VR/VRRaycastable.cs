using UnityEngine;

namespace C2M2.Interaction.VR
{
    using Utils;
    [RequireComponent(typeof(MeshFilter))]
    public abstract class VRRaycastable<ColliderSourceT> : MonoBehaviour
    {
        public GameObject raycastTargetObj { get; private set; } = null;
        protected ColliderSourceT source;
        public abstract ColliderSourceT GetSource();
        public abstract void SetSource(ColliderSourceT source);

        private void Awake()
        {
            // Build raycast target object
            raycastTargetObj = BuildChildObject();

            // Build the Rigidbody
            BuildRigidBody(raycastTargetObj);

            // Let children initialize
            OnAwake();
        }
        protected abstract void OnAwake();

        /// <summary> Instantiate child object & set its layer to "Raycast" </summary>
        /// <returns> The child object that was created. </returns>
        private GameObject BuildChildObject()
        {
            GameObject childObject = new GameObject("MeshCollider");
            childObject.layer = LayerMask.NameToLayer("Raycast");
            childObject.transform.parent = transform;
            childObject.transform.position = Vector3.zero;
            childObject.transform.eulerAngles = Vector3.zero;
            childObject.transform.localScale = Vector3.one;

            return childObject;
        }
        /// <summary> Add a rigidbody to the raycast child object and change its settings </summary>
        /// <returns> The Rigidbody that was created. </returns>
        private Rigidbody BuildRigidBody(GameObject raycastTargetObject)
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
