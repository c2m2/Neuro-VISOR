using System.Collections.Generic;
using UnityEngine;
using C2M2.Utils.Exceptions;
namespace C2M2.Interaction.Adjacency
{
    /// <summary>
    /// Given a mesh, creates and stores an edge list and adjacency list of the mesh vertices
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(UniqueVertices))]
    public class AdjacencyList : MonoBehaviour
    {
        /// <summary> 
        /// Stores the distance between adjacent vertices 
        /// </summary>
        public List<Node>[] adjacencyList { get; private set; }
        /// <summary>
        /// Stores a list of edges between vertices
        /// </summary>
        public List<Edge> edgeList { get; private set; }
        /// <summary>
        /// Stores the number of edges connected to each vertex.
        /// </summary>
        /// <remarks>
        /// edgeCount indices correspond to mesh.vertices indices
        /// </remarks>
        public int[] edgeCount { get; private set; }
        private Mesh mesh;
        private Vector3[] vertices;
        private int[] triangles;
        private UniqueVertices uniqueVertices;
        private void Awake()
        {
            CreateAdjacencyList();
        }
        /// <summary> Build an adjacency matrix using an edge list </summary>
        private void CreateAdjacencyList()
        {
            uniqueVertices = GetComponent<UniqueVertices>() ?? gameObject.AddComponent<UniqueVertices>();
            mesh = GetComponent<MeshFilter>().mesh;
            vertices = mesh.vertices;
            triangles = mesh.triangles;
            // Each unique vertex will have its own list of nodes
            adjacencyList = new List<Node>[uniqueVertices.uniqueVerts.Length];
            int space = 6 * (uniqueVertices.subdivisions + 1); // Reserve 6 places if no subdivisions, 12 places if 1, etc
                                                               // Create a new list for each adjacency list index to store adjacent info
            for (int i = 0; i < uniqueVertices.uniqueVerts.Length; i++) { adjacencyList[i] = new List<Node>(space); }
            edgeList = new List<Edge>(uniqueVertices.uniqueVerts.Length * 2);
            edgeCount = new int[uniqueVertices.uniqueVerts.Length];
            // If we want 1 subdivision, we want to split the edge in half (1 / 2) or [1 / (subdivisions + 1)]
            float divider;
            if (uniqueVertices.subdivisions > 0) divider = (1f / (uniqueVertices.subdivisions + 1));
            else divider = 1;
            if (uniqueVertices.subdivisions == 0)
            { // If we DON'T want to add any invisible edges, just add the triangle edges
                for (int i = 0; i < triangles.Length; i += 3)
                { // For each triangle, 
                  // Find the Vector3 position of each point in the triangle, 
                    Vector3 vec0 = vertices[triangles[i]];
                    Vector3 vec1 = vertices[triangles[i + 1]];
                    Vector3 vec2 = vertices[triangles[i + 2]];
                    // Find the UNIQUE vertex that occupies that same Vector3, 
                    int v0 = uniqueVertices.uniqueVertReverseLookup[vec0];
                    int v1 = uniqueVertices.uniqueVertReverseLookup[vec1];
                    int v2 = uniqueVertices.uniqueVertReverseLookup[vec2];
                    // Calculate the distance between each unqiue point and store them in the adjacency matrix
                    float dist01 = Vector3.Distance(vec0, vec1);
                    float dist12 = Vector3.Distance(vec1, vec2);
                    float dist20 = Vector3.Distance(vec2, vec0);
                    AdjacencyListAddEdge(v0, v1, dist01, uniqueVertices.uniqueVerts.Length);
                    AdjacencyListAddEdge(v1, v2, dist12, uniqueVertices.uniqueVerts.Length);
                    AdjacencyListAddEdge(v2, v0, dist20, uniqueVertices.uniqueVerts.Length);
                }
            }
            else
            { // If we DO want to add invisible edges,
                for (int i = 0; i < triangles.Length; i += 3)
                { // For each triangle, find the points of the triange, find the UNIQUE vertex that occupies that same spot, and add the unique vertices as new edges
                  // Get the vector of each vertex in the triangle
                    Vector3 vec0 = vertices[triangles[i]];
                    Vector3 vec1 = vertices[triangles[i + 1]];
                    Vector3 vec2 = vertices[triangles[i + 2]];
                    // Get the UNIQUE index of each vertex in the triangle
                    int v0 = uniqueVertices.uniqueVertReverseLookup[vec0];
                    int v1 = uniqueVertices.uniqueVertReverseLookup[vec1];
                    int v2 = uniqueVertices.uniqueVertReverseLookup[vec2];
                    // Add the default edges to the graph so that we can skip invisible edges later if we want to
                    float dist01 = Vector3.Distance(vec0, vec1);
                    float dist12 = Vector3.Distance(vec1, vec2);
                    float dist20 = Vector3.Distance(vec2, vec0);
                    AdjacencyListAddEdge(v0, v1, dist01, uniqueVertices.uniqueMeshVertLength);
                    AdjacencyListAddEdge(v1, v2, dist12, uniqueVertices.uniqueMeshVertLength);
                    AdjacencyListAddEdge(v2, v0, dist20, uniqueVertices.uniqueMeshVertLength);
                    // We're going to "crawl" along each edge 01, 12, and 20 of this triangle and add edges connecting each intermediate vertex with the previous one AND the opposite one
                    int v01previous = v0;
                    int v12previous = v1;
                    int v20previous = v2;
                    // Get the distance between each invisible vertex on the same edge, since they will be placed along even fractions of the edge
                    dist01 *= divider;
                    dist12 *= divider;
                    dist20 *= divider;
                    // Go through each subdivision
                    float scaler = divider;
                    for (int s = 0; s < uniqueVertices.subdivisions; s++)
                    { // Go along each subdivided region from (1/4 to 2/4 to 3/4 to finally 4/4
                      // Recalculate the inbetween positions
                      // Get the index of each in between vertex from its position
                        int v01current = uniqueVertices.uniqueVertReverseLookup[Vector3.Lerp(vec0, vec1, scaler)];
                        int v12current = uniqueVertices.uniqueVertReverseLookup[Vector3.Lerp(vec1, vec2, scaler)];
                        int v20current = uniqueVertices.uniqueVertReverseLookup[Vector3.Lerp(vec2, vec0, scaler)];
                        // Calculate distance from each in between vertex to its opposite
                        float dist01t2 = Vector3.Distance(uniqueVertices.uniqueVerts[v01current], vec2);
                        float dist12t0 = Vector3.Distance(uniqueVertices.uniqueVerts[v12current], vec0);
                        float dist20t1 = Vector3.Distance(uniqueVertices.uniqueVerts[v20current], vec1);
                        // Add an edge between "current" and its opposite vert AND add THE BACKWARDS EDGE
                        AdjacencyListAddEdge(v01current, v2, dist01t2, uniqueVertices.uniqueMeshVertLength);
                        AdjacencyListAddEdge(v12current, v0, dist12t0, uniqueVertices.uniqueMeshVertLength);
                        AdjacencyListAddEdge(v20current, v1, dist20t1, uniqueVertices.uniqueMeshVertLength);
                        // Add an edge between "previous" and "current" AND add THE BACKWARDS EDGE
                        AdjacencyListAddEdge(v01previous, v01current, dist01, uniqueVertices.uniqueMeshVertLength);
                        AdjacencyListAddEdge(v12previous, v12current, dist12, uniqueVertices.uniqueMeshVertLength);
                        AdjacencyListAddEdge(v20previous, v20current, dist20, uniqueVertices.uniqueMeshVertLength);
                        // Slide down the edge
                        v01previous = v01current;
                        v12previous = v12current;
                        v20previous = v20current;
                        // Slide up the Lerp scaler
                        scaler += divider;
                    }
                    // Now we're at the end, so just connect the second to last invisible vert to the opposite vert. Don't connect to the opposite vert because there will be more invisible verts along that edge
                    AdjacencyListAddEdge(v01previous, v1, dist01, uniqueVertices.uniqueMeshVertLength);
                    AdjacencyListAddEdge(v2, v12previous, dist12, uniqueVertices.uniqueMeshVertLength);
                    AdjacencyListAddEdge(v0, v20previous, dist20, uniqueVertices.uniqueMeshVertLength);
                }
            }
        }
        bool duplicate;
        /// <summary> If it doesn't already exist, add an adjacency list entry </summary>
        /// <param name="point1"> First point of the edge </param>
        /// <param name="point2"> second point of the edge </param>
        /// <param name="distance"> Vector3 distance between point1 and point2 </param>
        private void AdjacencyListAddEdge(int point1, int point2, float distance, int uniqueMeshVertLength)
        {
            duplicate = false;
            for (int i = 0; i < adjacencyList[point1].Count; i++)
            { // If we have never added point [end] to point [origin]'s list, then the back edge should not exist either, so we only need to check one.
                if (adjacencyList[point1][i].index == point2)
                {
                    duplicate = true;
                }
            }
            if (!duplicate)
            {
                adjacencyList[point1].Add(new Node(distance, point2)); adjacencyList[point2].Add(new Node(distance, point1));
                if (point1 < uniqueMeshVertLength && point2 < uniqueMeshVertLength)
                {
                    // If it's on the mesh, add the edge to edgeList
                    edgeList.Add(new Edge(point1, point2, distance));
                    edgeCount[point1]++; edgeCount[point2]++;
                }
            }
        }
    }
}
