using UnityEngine;

namespace C2M2
{
    namespace Interaction
    {
        using Utilities;
        [RequireComponent(typeof(MeshFilter))]
        public class VRRaycastable : MonoBehaviour
        {
            public GameObject raycastTargetObject { get; private set; } = null;
            private Mesh colliderMesh = null;
            public Mesh ColliderMesh
            {
                get { return colliderMesh; }
                set
                {
                    colliderMesh = value;
                    if (colliderMesh != null)
                    {
                        MeshCollider col = raycastTargetObject.GetComponent<MeshCollider>();
                        col.sharedMesh = colliderMesh;
                    }
                }
            }

            private void Awake()
            {
                // Build raycast target object
                raycastTargetObject = BuildChildObject();

                // Build the Rigidbody
                BuildRigidBody(raycastTargetObject);

                // Check if there is a mesh, then send it to mesh collider   
                Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
                if (mesh == null) throw new MeshNotFoundException();
                BuildMeshCollider(gameObject, raycastTargetObject, mesh);

                raycastTargetObject.AddComponent<TransformResetter>().targetFrame = 2;
            }
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
            /// <summary> Add a mesh collider to the child object, and set its mesh to be the original gameobject's. </summary>
            /// <returns> The MeshCollider that was created. </returns>
            private static MeshCollider BuildMeshCollider(GameObject gameObject, GameObject raycastTargetObject, Mesh mesh)
            {
                MeshCollider meshCollider = raycastTargetObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh ?? throw new MeshNotFoundException("No mesh on " + gameObject);
                return meshCollider;
            }
        }
    }
}
