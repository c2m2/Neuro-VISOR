using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using C2M2.Utils.Exceptions;
namespace C2M2.Interaction.Adjacency
{
    /// <summary>
    /// Get the unique vertices of a mesh and add invisible vertices for adjacency list
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class UniqueVertices : MonoBehaviour
    {
        public int subdivisions { get; private set; } = 1;
        /// <summary> Array to hold the unique mesh vertices up to uniqueMeshVertLength, and then the invisible vertices used for more accurate distance measurement </summary>
        public Vector3[] uniqueVerts { get; private set; }
        /// <summary> Gives the number of unique mesh vertices, separate from invisible vertices used for distance measurements </summary>
        public int uniqueMeshVertLength { get; private set; }
        /// <summary> Given a Vector3 point, return a list of unique AND invisible index numbers corresponding to the uniqueVerts array </summary>
        public Dictionary<Vector3, int> uniqueVertReverseLookup { get; private set; }
        private Vector3[] vertices;
        private int[] triangles;
        private void Awake()
        {
            Mesh mesh = GetComponent<MeshFilter>().mesh ?? throw new MeshNotFoundException();
            vertices = mesh.vertices;
            triangles = mesh.triangles;
            // Find the unique vertices
            uniqueVerts = vertices.Distinct().ToArray();
            // Store the number of unique mesh verts
            uniqueMeshVertLength = uniqueVerts.Length;
            // Build invisible verts and append them to the end of uniqueVerts
            Vector3[] invisibleVerts = BuildInvisibleVerts(subdivisions);
            uniqueVerts = Utils.Array.MergeArrays(uniqueVerts, invisibleVerts);
            // Build unique vert dictionaries
            uniqueVertReverseLookup = BuildUniqueVertReverseLookup(uniqueVerts);
        }
        /// <summary> Add vertices along each edge according to subdivisions and make it a unique list </summary>
        private Vector3[] BuildInvisibleVerts(int subdivisions)
        {
            List<Vector3> invisibleVerts = new List<Vector3>(vertices.Length * subdivisions);
            float divider;
            if (subdivisions > 0)
            { // If we want 1 subdivision, we want to split the edge in half (1 / 2) or [1 / (subdivisions + 1)]
                divider = (1f / (subdivisions + 1));
            }
            else
            { // If we don't want any subdivisions, just set the divider to 1
                divider = 1;
            }
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                Vector3 vec0 = vertices[v0];
                Vector3 vec1 = vertices[v1];
                Vector3 vec2 = vertices[v2];
                float scaler = divider;
                for (int s = 0; s < subdivisions; s++)
                { // Go along each subdivided region from (1/4 to 2/4 to 3/4 to finally 4/4
                  // Create new verts along edges 01, 12, 20
                    Vector3 invisVert01 = Vector3.Lerp(vec0, vec1, scaler);
                    Vector3 invisVert12 = Vector3.Lerp(vec1, vec2, scaler);
                    Vector3 invisVert20 = Vector3.Lerp(vec2, vec0, scaler);
                    // Add vertices to invisible vertex list
                    invisibleVerts.Add(invisVert01);
                    invisibleVerts.Add(invisVert12);
                    invisibleVerts.Add(invisVert20);
                    scaler += divider;
                }
            }
            return invisibleVerts.Distinct().ToArray();
        }
        /// <summary> Given a raycast hit, find the nearest unqiue mesh vert to the hit </summary>
        /// <returns> uniqueVerts index of the nearest hit vert </returns>
        public int FindNearestUniqueVert(RaycastHit hit)
        {
            int trueTriangleIndex = hit.triangleIndex * 3;
            int unique0 = uniqueVertReverseLookup[vertices[triangles[trueTriangleIndex]]];
            int unique1 = uniqueVertReverseLookup[vertices[triangles[trueTriangleIndex + 1]]];
            int unique2 = uniqueVertReverseLookup[vertices[triangles[trueTriangleIndex + 2]]];
            float dist0 = Vector3.Distance(hit.point, uniqueVerts[unique0]);
            float dist1 = Vector3.Distance(hit.point, uniqueVerts[unique1]);
            float dist2 = Vector3.Distance(hit.point, uniqueVerts[unique2]);
            if (dist0 <= dist1 && dist0 <= dist2) { return unique0; }
            else if (dist1 <= dist0 && dist1 <= dist2) { return unique1; }
            else if (dist2 <= dist0 && dist2 <= dist1) { return unique2; }
            return -1;
        }
        public List<Node> RaycastFindNearestUniqueVerts(RaycastHit hit, int subdivisions)
        {
            subdivisions = Mathf.Max(subdivisions, 0);
            // If we want 1 subdivision, we want to split the edge in half (1 / 2) or [1 / (subdivisions + 1)]
            float divider = (1f / (subdivisions + 1));
   
            if (hit.triangleIndex == -1) { throw new ArgumentException(); }
            // Initialize our list of initial nodes
            List<Node> initialNodes = new List<Node>(3 * (subdivisions + 1));
            // Find the indices and vectors of the triangle our hit point falls into
            int trueTriangleIndex = hit.triangleIndex * 3;
            Vector3 vec0 = vertices[triangles[trueTriangleIndex]];
            Vector3 vec1 = vertices[triangles[trueTriangleIndex + 1]];
            Vector3 vec2 = vertices[triangles[trueTriangleIndex + 2]];
            int v0 = uniqueVertReverseLookup[vec0];
            int v1 = uniqueVertReverseLookup[vec1];
            int v2 = uniqueVertReverseLookup[vec2];
            // Calculate distance between these points and the hit point's position in local space and add our new nodes to the list
            Vector3 hitPoint = transform.InverseTransformPoint(hit.point);
            initialNodes.Add(new Node(Vector3.Distance(hitPoint, vec0), v0)); initialNodes.Add(new Node(Vector3.Distance(hitPoint, vec1), v1)); initialNodes.Add(new Node(Vector3.Distance(hitPoint, vec2), v2));
            // Find all of the invisible verts of ouir triangle and add those to our list in the same way
            float scaler = divider;
            for (int s = 0; s < subdivisions; s++)
            { // Iterate over this triangle and find the invisible verts
              // Calculate the invisible vertex positions
                Vector3 invisVec01 = Vector3.Lerp(vec0, vec1, scaler);
                Vector3 invisVec12 = Vector3.Lerp(vec1, vec2, scaler);
                Vector3 invisVec20 = Vector3.Lerp(vec2, vec0, scaler);
                // Find the indices of our invisible verts in the unique vertex array
                int invisV01 = uniqueVertReverseLookup[invisVec01];
                int invisV12 = uniqueVertReverseLookup[invisVec12];
                int invisV20 = uniqueVertReverseLookup[invisVec20];
                // Find the distance between the invisible verts and the hit point, and add them to our list of nodes
                initialNodes.Add(new Node(Vector3.Distance(hit.point, invisVec01), invisV01));
                initialNodes.Add(new Node(Vector3.Distance(hit.point, invisVec12), invisV12));
                initialNodes.Add(new Node(Vector3.Distance(hit.point, invisVec20), invisV20));
                // Slide up Lerp scaler
                scaler += divider;
            }
            return initialNodes;
        }
        /// <summary> "Flip" the unique vertex array around so that you can lookup a vertex index by its position </summary>
        private Dictionary<Vector3, int> BuildUniqueVertReverseLookup(Vector3[] uniqueVerts)
        {
            Dictionary<Vector3, int> uniqueVertReverseLookup = new Dictionary<Vector3, int>(uniqueVerts.Length);
            for (int i = 0; i < uniqueVerts.Length; i++)
            { // for each vertex, store the Vector3 position as a lookup number for it's index
                uniqueVertReverseLookup.Add(uniqueVerts[i], i);
            }
            return uniqueVertReverseLookup;
        }
    }
}