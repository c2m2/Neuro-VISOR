using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Utils.MeshUtils
{
    /// <summary>
    /// Set this.mesh to quickly look up and update a MeshCollider's mesh
    /// </summary>
    /// <remarks>
    /// Set this.meshCol to directly reference a specific MeshCollider.
    /// Otherwise the script will use the first MeshCollider it finds.
    /// </remarks>
    public class MeshColController : MonoBehaviour
    {
        public MeshCollider meshCol = null;
        private Mesh mesh;
        public Mesh Mesh
        {
            get { return mesh; }
            set
            {
                if (value == null)
                {
                    Debug.LogError("Mesh is null.");
                    return;
                }
                // Store new mesh
                mesh = value;

                // Look for a mesh collider if none was given
                if (meshCol == null) meshCol = GetComponentInChildren<MeshCollider>();

                // Apply new mesh
                if (meshCol != null)
                {
                    meshCol.sharedMesh = mesh;
                    Debug.Log("MeshCollider mesh: " + mesh.name);
                }
                else Debug.LogError("Could not find mesh collider.");
            }
        }
    }
}
