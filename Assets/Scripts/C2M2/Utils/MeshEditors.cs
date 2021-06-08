using UnityEngine;
using System.Linq;

namespace C2M2.Utils.MeshUtils
{
    /// <summary>
    /// Utilities used to rescale meshes and reverse their triangle windings
    /// </summary>
    public static class MeshEditors
    {
        /// <summary> Flip a mesh inside out by flipping its triangles around </summary>
        /// <returns> True if flipping succeeded, false if an exception was caught </returns>
        public static void ReverseTriangles(this Mesh mesh)
        {
            mesh.triangles = mesh.triangles.Reverse().ToArray();
            Debug.Log("Reversed the triangles on mesh " + mesh.name);
        }
        /// <summary> Rescale this mesh to targetSize </summary>
        /// <param name="transform"> Transform that the mesh is attached to </param>
        /// <param name="maintainAspectRatio"> If true, scales all axes </param>
        public static void Rescale(this Mesh mesh, Transform transform, Vector3 targetSize, bool maintainAspectRatio, bool recalculateNormals, bool recalculateTangents)
        {
            var bounds = mesh.bounds.size;
            float xScale = 1, yScale = 1, zScale = 1;
            // Calculate how much to scale each axis by
            if (maintainAspectRatio)
            { // Will scale all axes by the same amount
                float[] boundsArray = { bounds.x, bounds.y, bounds.z };
                float max = Mathf.Max(boundsArray);
                float[] targetArray = { targetSize.x, targetSize.y, targetSize.z };
                float min = Mathf.Min(targetArray);
                xScale = min / max;
                yScale = xScale;
                zScale = xScale;
            }
            else
            { // Will scale all axes individually
                xScale = (targetSize.x / bounds.x);
                yScale = (targetSize.y / bounds.y);
                zScale = (targetSize.z / bounds.z);
            }
            // If there is a transform given, just scale it. Otherwise scale mesh verts
            if (transform == null)
            { // Will scale the transform without affecting the actual mesh
                Vector3[] verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++) { verts[i] = new Vector3(verts[i].x * xScale, verts[i].y * yScale, verts[i].z * zScale); }
                mesh.vertices = verts;
                if (recalculateNormals) mesh.RecalculateNormals();
                if (recalculateTangents) mesh.RecalculateTangents();
                mesh.RecalculateBounds();
            }
            else
            { // Will scale the actual mesh vertices
                transform.localScale = new Vector3(xScale, yScale, zScale);
            }
        }
        public static void Rescale(this Mesh mesh, Transform transform, Vector3 targetSize, bool maintainAspectRatio, bool recalculateNormals) => Rescale(mesh, transform, targetSize, maintainAspectRatio, recalculateNormals, false);
        public static void Rescale(this Mesh mesh, Transform transform, Vector3 targetSize, bool maintainAspectRatio) => Rescale(mesh, transform, targetSize, maintainAspectRatio, true, false);
        public static void Rescale(this Mesh mesh, Transform transform, Vector3 targetSize) => Rescale(mesh, transform, targetSize, true, true, false);
        public static void Rescale(this Mesh mesh, Transform transform) => Rescale(mesh, transform, Vector3.one, true, true, false);
        public static void Rescale(this Mesh mesh, Vector3 targetSize) => Rescale(mesh, null, targetSize, true, true, false);
        public static void Rescale(this Mesh mesh) => Rescale(mesh, null, Vector3.one, true, true, false);
    }
}