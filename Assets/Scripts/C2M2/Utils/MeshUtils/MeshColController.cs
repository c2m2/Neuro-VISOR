using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Utils.MeshUtils
{
    /// <summary>
    /// Set MeshColController instance.mesh to automatically fetch and update MeshCollider meshCol.sharedMesh
    /// </summary>
    /// <remarks>
    /// Provides an interface to more easily update MeshCollider
    /// </remarks>
    public class MeshColController : MonoBehaviour
    {
        public MeshCollider meshCol = null;
        public Mesh mesh
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
                if (meshCol != null) meshCol.sharedMesh = mesh;
            }
        }
    }
}
