using UnityEngine;
using System;
using C2M2.Utils.Exceptions;

namespace C2M2.Utils.MeshUtils
{
    /// <summary>
    /// Monobehaviour for reversing the winding of Mesh triangles from the editor
    /// </summary>
    /// <remarks>
    /// Uses MeshUtils.MeshEditors
    /// </remarks>
    [ExecuteInEditMode]
    public class MonoMeshReverser : MonoBehaviour
    {
        private void Update()
        {
            FlipInsideOut();
        }
        public void FlipInsideOut()
        {
            try
            {
                MeshFilter mf = GetComponent<MeshFilter>() ?? throw new MeshFilterNotFoundException();
                Mesh mesh = mf.sharedMesh ?? throw new MeshNotFoundException();
                mesh.ReverseTriangles();
                mf.sharedMesh = mesh;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            DestroyImmediate(this);
        }
    }
}
namespace C2M2.Utils.Exceptions
{
    public class MeshFilterNotFoundException : Exception
    {
        public MeshFilterNotFoundException() { }
        public MeshFilterNotFoundException(string message) : base(message) { }
        public MeshFilterNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
