using UnityEngine;
using C2M2.Utils.Exceptions;

namespace C2M2.Interaction.VR
{
    public class VRRaycastableMesh : VRRaycastable<Mesh>
    {
        public GameObject raycastTargetObj { get; private set; } = null;
        // protected Mesh source;

        public override Mesh GetSource()
        {
            return source;
        }

        public override void SetSource(Mesh source)
        {
            if(source != null)
            {
                this.source = source;
                MeshCollider col = raycastTargetObj.GetComponent<MeshCollider>();
                if (col == null) col = raycastTargetObj.AddComponent<MeshCollider>();
                col.sharedMesh = this.source;
            }
        }

        protected override void OnAwake()
        {
            // Build raycast target object
            raycastTargetObj = BuildChildObject(transform);

            // Build the Rigidbody
            //BuildRigidBody(raycastTargetObj);

            // Check if there is a mesh, then send it to mesh collider   
            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
            if (mesh == null) throw new MeshNotFoundException();

            BuildMeshCollider(gameObject, raycastTargetObj, mesh);
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
