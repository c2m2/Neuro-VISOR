using UnityEngine;
using System.Text;

namespace C2M2
{
    namespace Utils
    {
        using static Math;
        /// <summary>
        /// Utilities that apply to Unity Mesh objects
        /// </summary>
        namespace MeshUtils
        {
            /// <summary>
            /// Utilities used to print Mesh information cleanly
            /// </summary>
            public static class MeshInfo
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

}
