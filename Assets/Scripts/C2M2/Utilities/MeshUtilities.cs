using UnityEngine;
using System.Text;
using System.Linq;

namespace C2M2
{
    namespace Utilities
    {
        using static Math;
        /// <summary> Debugging helpers for meshes </summary>
        public static partial class MeshUtilities
        {
            /// <summary> Print first 'n' triangles of this mesh </summary>
            /// <param name="n"> Number of triangles to print </param>
            public static string PrintTriangles(in Mesh mesh, int n)
            {
                int finalIndex = n * 3;
                finalIndex = Min(finalIndex, mesh.triangles.Length);
                // Size estimate
                string header = "mesh \"" + mesh.name + "\", " + n + "/" + (mesh.triangles.Length / 3) + " triangles:\n";
                StringBuilder sb = new StringBuilder(header, 24 * ((mesh.triangles.Length / 3)) + 1);
                if ((finalIndex % 3) != 0) finalIndex -= (n % 3);
                for (int i = 0; i < finalIndex; i += 3)
                {
                    sb.AppendFormat("{0}: {{{1}, {2}, {3}}}\n", (i / 3), mesh.triangles[i], mesh.triangles[i + 1], mesh.triangles[i + 2]);
                }
                return sb.ToString();
            }
            /// <summary> Print all triangles of this mesh </summary>
            public static string PrintTriangles(in Mesh mesh) => PrintTriangles(mesh, (mesh.triangles.Length / 3));

            /// <summary> Print the vertices of this mesh to any degree of accuracy </summary>
            public static string PrintVertices(in Mesh mesh, int precision)
            {
                // 1 <= precision <= 8
                precision = Min(Max(precision, 1), 8);
                string precisionSpecifier = "F" + precision;
                string header = "mesh \"" + mesh.name + "\", " + (mesh.vertices.Length) + " vertices:\n";
                StringBuilder sb = new StringBuilder(header, 24 * ((mesh.triangles.Length / 3)) + 1);
                string formatString = "{0}:{{ {1:" + precisionSpecifier + "}, {2:" + precisionSpecifier + "}, {3:" + precisionSpecifier + "} }}\n";
                for (int i = 0; i < mesh.vertices.Length; i++)
                {
                    sb.AppendFormat(formatString, i, mesh.vertices[i].x, mesh.vertices[i].y, mesh.vertices[i].z);
                }
                return sb.ToString();
            }
            /// <summary> Print the vertices of this mesh to 4 decimal places </summary>
            public static string PrintVertices(in Mesh mesh) => PrintVertices(mesh, 4);
        }
    }

}
public static partial class MeshHelpers
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
