#pragma warning disable CS0618

using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace C2M2.Interaction
{
    using Simulation;
    using Utils;
    /// <summary> Create and store additional mesh info like an adjacency list, array of unique vertices, unique and mesh vertex Vector3 dictionaries </summary>
    [Obsolete("Separated into separate scripts AdjacencyList, UniqueVertices, ColorManager, etc")]
    public class MeshInfo : MonoBehaviour {
        private ObjectManager objectManager;
        #region PublicVariables
        [Header("Diffusion")]
        public float distanceThreshold = 0.35f;
        public float diffusionRate = 100f;
        [Header("AdjacencyList")]
        [Tooltip("Number of times to subdivide each mesh edge in order to gain more accurate distance measurement")]
        /// <summary> Number of times to subdivide each mesh edge in order to gain more accurate distance measurement </summary>
        [Range(0, 1000)]
        public int subdivisions = 1;
        /// <summary> Used to apply color to the mesh depending on the distance from the origin vertex </summary>
        public Gradient gradient { get; set; }
        [Header("Gaussian")]
        public float gaussianHeight = 1;
        [Tooltip("The width of the Gaussian curve. At runtime this corresponds to a local space distance threshold, before runtime this is a worldspace distance that will be translated.")]
        public float gaussianWidth = 0.1f;
        #region MeshInfo
        /// <summary> Stores the distance between adjacent vertices </summary>
        public List<Node>[] adjacencyList { get; private set; }
        /// <summary> Array to hold the unique mesh vertices up to uniqueMeshVertLength, and then the invisible vertices used for more accurate distance measurement </summary>
        public Vector3[] uniqueVerts { get; private set; }
        /// <summary> uniqueVerts[uniqueMeshVertLength - 1] is the last index for unqiue mesh vertices in uniqueVerts, after this are the invisible vertices </summary>
        public int uniqueMeshVertLength { get; private set; }
        /// <summary> Given a Vector3 point, return a list of unique AND invisible index numbers corresponding to the uniqueVerts array </summary>
        public Dictionary<Vector3, int> uniqueVertReverseLookup { get; private set; }
        /// <summary> Given a vector3 point, return a list of indices corresponding to mesh.vertices of all mesh vertices at that point </summary>
        public Dictionary<Vector3, List<int>> meshVertReverseLookup { get; private set; }
        /// <summary> Store a list of all edges v1 -> v2 on the geometry </summary>
        public List<Edge> edgeList { get; private set; }
        /// <summary> Store the number of unique edges that each unique vertex v belongs to </summary>
        public int[] edgeCount { get; private set; }
        /// <summary> Array for storing scalar values applied at different points </summary>
        public double[] scalars;
        /// <summary> Used to appply changes to mesh.colors32 </summary>
        public Color32[] colors32 { get; private set; }
        /// <summary> Mesh filter of the dijkstra object for retrieving current mesh </summary>
        public MeshFilter mf { get; private set; }
        /// <summary> Mesh to run Dijkstra's on </summary>
        public Mesh activeMesh { get; private set; }
        /// <summary> Copy of mesh.vertices </summary>
        public Vector3[] vertices { get; private set; }
        /// <summary> Copy of mesh.triangles </summary>
        public int[] triangles { get; private set; }
        #endregion
        #endregion
        #region PrivateVariables
        #region MeshInfo


        #endregion
        #region DijkstraSearchUtilities
        /// <summary> = 1 / Subdivisions. used to lerp along edges in order to add invisible edges </summary>
        private float divider;
        #endregion
        /// <summary> Ensure that the adjacency matrix and other important parts of the script have been initialized </summary>
        public bool initialized { get; private set; } = false;
        #endregion
        #region PublicMethods
        /// <summary> Build the Dijkstra Object's unique vertex list, adjacency list, etc </summary>
        public void Initialize(ObjectManager objectManager)
        {
            this.objectManager = objectManager;
            InitializeAll();
        }
        public void InitializeAll()
        {
            mf = GetComponent<MeshFilter>();
            activeMesh = mf.mesh;
            // Save copies of the vertices, find the unique vertices, and save enough space to store scalar values for each vertex
            vertices = activeMesh.vertices;
            triangles = activeMesh.triangles;
            if (subdivisions > 0)
            { // If we want 1 subdivision, we want to split the edge in half (1 / 2) or [1 / (subdivisions + 1)]
                divider = (1f / (subdivisions + 1));
            }
            else
            { // If we don't want any subdivisions, just set the divider to 1
                divider = 1;
            }
            // Build Dijkstra parts
            BuildColorArray();
            BuildUniqueVerts();
            BuildUniqueVertReverseLookup();
            BuildMeshVertReverseLookup();
            // Initialize new scalar field data
            scalars = new double[uniqueMeshVertLength];
            edgeList = new List<Edge>(uniqueVerts.Length * 2);
            edgeCount = new int[uniqueVerts.Length];
            adjacencyList = new List<Node>[uniqueVerts.Length];

            BuildAdjacencyList();

            // Convert our distance threshold into local space
            distanceThreshold /= transform.localScale.x;
            distanceThreshold = Mathf.Ceil(distanceThreshold);
            gaussianWidth = distanceThreshold / 3.716922188f;
            // BuildGeometryText();
            initialized = true;
        }
        // TODO: This needs to be called "RecolorSurface()" or something
        /// TODO: This overwrites the global scalar list each time. We need to add to it
        double maxConcentration = 1, minConcentration = 0, range = 1;
        public void SubmitComponentDataChanges(float[] scalars)
        {
            range = maxConcentration - minConcentration;
            if (maxConcentration == 1 && minConcentration == 0)
            { // We can simplify our gradscale equation a lot if max = 1 && min == 0
                for (int i = 0; i < scalars.Length; i++)
                { // For each change we want to submit,          
                  // Calculate the color for this point based on the Gaussian values
                    Color curCol = gradient.Evaluate(scalars[i]);
                    // Color all verts at this point appropriately
                    List<int> currentOverlappingVerts = meshVertReverseLookup[uniqueVerts[i]];
                    for (int j = 0; j < currentOverlappingVerts.Count; j++)
                    { /* Apply the color to every overlapping vertex */
                        colors32[currentOverlappingVerts[j]] = curCol;
                    }
                }
            }
            else
            {
                for (int i = 0; i < scalars.Length; i++)
                { // For each change we want to submit,          
                  // Calculate the color for this point based on the Gaussian values
                    Color curCol = gradient.Evaluate((float)((scalars[i] - minConcentration) / range));
                    // Color all verts at this point appropriately
                    List<int> currentOverlappingVerts = meshVertReverseLookup[uniqueVerts[i]];
                    for (int j = 0; j < currentOverlappingVerts.Count; j++)
                    { // Apply the color to every overlapping vertex
                        colors32[currentOverlappingVerts[j]] = curCol;
                    }
                }
            }
            activeMesh.colors32 = colors32;
        }
        public Color32 ColorFromUniqueIndex(int uniqueIndex) => colors32[meshVertReverseLookup[uniqueVerts[uniqueIndex]][0]];
        public Color32 Color32FromMeshIndex(int meshIndex) => colors32[meshIndex];
        public List<int> UniqueIndexToMeshIndex(int uniqueIndex) => meshVertReverseLookup[uniqueVerts[uniqueIndex]];
        /// <summary> Find the initial nodes for FindPath using raycast hit information </summary>
        public List<Node> BuildInitialNodes(RaycastHit hit)
        {
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
        #endregion
        #region PrivateMethods
        /// <summary> Build an adjacency matrix using an edge list </summary>
        private void BuildAdjacencyList()
        {
            int space = 6 * (subdivisions + 1); // Reserve 6 places if no subdivisions, 12 places if 1, etc
            for (int i = 0; i < uniqueVerts.Length; i++)
            { // Create a new list for each adjacency list index to store adjacent info
                adjacencyList[i] = new List<Node>(space);
            }
            Vector3 vec0, vec1, vec2;
            int v0, v1, v2, v01current, v01previous, v12current, v12previous, v20current, v20previous;
            float scaler, dist01, dist12, dist20, dist01t2, dist12t0, dist20t1;
            if (subdivisions == 0)
            { // If we DON'T want to add any invisible edges, just add the triangle edges
                for (int i = 0; i < triangles.Length; i += 3)
                { // For each triangle, 
                  // Find the Vector3 position of each point in the triangle, 
                    vec0 = vertices[triangles[i]];
                    vec1 = vertices[triangles[i + 1]];
                    vec2 = vertices[triangles[i + 2]];
                    // Find the UNIQUE vertex that occupies that same Vector3, 
                    v0 = uniqueVertReverseLookup[vec0];
                    v1 = uniqueVertReverseLookup[vec1];
                    v2 = uniqueVertReverseLookup[vec2];
                    // Calculate the distance between each unqiue point and store them in the adjacency matrix
                    dist01 = Vector3.Distance(vec0, vec1);
                    dist12 = Vector3.Distance(vec1, vec2);
                    dist20 = Vector3.Distance(vec2, vec0);
                    AdjacencyListAddEdge(v0, v1, dist01);
                    AdjacencyListAddEdge(v1, v2, dist12);
                    AdjacencyListAddEdge(v2, v0, dist20);
                }
            }
            else
            { // If we DO want to add invisible edges,
                for (int i = 0; i < triangles.Length; i += 3)
                { // For each triangle, find the points of the triange, find the UNIQUE vertex that occupies that same spot, and add the unique vertices as new edges
                  // Get the vector of each vertex in the triangle
                    vec0 = vertices[triangles[i]];
                    vec1 = vertices[triangles[i + 1]];
                    vec2 = vertices[triangles[i + 2]];
                    // Get the UNIQUE index of each vertex in the triangle
                    v0 = uniqueVertReverseLookup[vec0];
                    v1 = uniqueVertReverseLookup[vec1];
                    v2 = uniqueVertReverseLookup[vec2];
                    // Add the default edges to the graph so that we can skip invisible edges later if we want to
                    dist01 = Vector3.Distance(vec0, vec1);
                    dist12 = Vector3.Distance(vec1, vec2);
                    dist20 = Vector3.Distance(vec2, vec0);
                    AdjacencyListAddEdge(v0, v1, dist01);
                    AdjacencyListAddEdge(v1, v2, dist12);
                    AdjacencyListAddEdge(v2, v0, dist20);
                    // We're going to "crawl" along each edge 01, 12, and 20 of this triangle and add edges connecting each intermediate vertex with the previous one AND the opposite one
                    v01previous = v0;
                    v12previous = v1;
                    v20previous = v2;
                    // Get the distance between each invisible vertex on the same edge, since they will be placed along even fractions of the edge
                    dist01 *= divider;
                    dist12 *= divider;
                    dist20 *= divider;
                    // Go through each subdivision
                    scaler = divider;
                    for (int s = 0; s < subdivisions; s++)
                    { // Go along each subdivided region from (1/4 to 2/4 to 3/4 to finally 4/4
                      // Recalculate the inbetween positions
                      // Get the index of each in between vertex from its position
                        v01current = uniqueVertReverseLookup[Vector3.Lerp(vec0, vec1, scaler)];
                        v12current = uniqueVertReverseLookup[Vector3.Lerp(vec1, vec2, scaler)];
                        v20current = uniqueVertReverseLookup[Vector3.Lerp(vec2, vec0, scaler)];
                        // Calculate distance from each in between vertex to its opposite
                        dist01t2 = Vector3.Distance(uniqueVerts[v01current], vec2);
                        dist12t0 = Vector3.Distance(uniqueVerts[v12current], vec0);
                        dist20t1 = Vector3.Distance(uniqueVerts[v20current], vec1);
                        // Add an edge between "current" and its opposite vert AND add THE BACKWARDS EDGE
                        AdjacencyListAddEdge(v01current, v2, dist01t2);
                        AdjacencyListAddEdge(v12current, v0, dist12t0);
                        AdjacencyListAddEdge(v20current, v1, dist20t1);
                        // Add an edge between "previous" and "current" AND add THE BACKWARDS EDGE
                        AdjacencyListAddEdge(v01previous, v01current, dist01);
                        AdjacencyListAddEdge(v12previous, v12current, dist12);
                        AdjacencyListAddEdge(v20previous, v20current, dist20);
                        // Slide down the edge
                        v01previous = v01current;
                        v12previous = v12current;
                        v20previous = v20current;
                        // Slide up the Lerp scaler
                        scaler += divider;
                    }
                    // Now we're at the end, so just connect the second to last invisible vert to the opposite vert. Don't connect to the opposite vert because there will be more invisible verts along that edge
                    AdjacencyListAddEdge(v01previous, v1, dist01);
                    AdjacencyListAddEdge(v2, v12previous, dist12);
                    AdjacencyListAddEdge(v0, v20previous, dist20);
                }
            }
        }
        bool duplicate;
        /// <summary> If it doesn't already exist, add an adjacency list entry </summary>
        /// <param name="point1"> First point of the edge </param>
        /// <param name="point2"> second point of the edge </param>
        /// <param name="distance"> Vector3 distance between point1 and point2 </param>
        private void AdjacencyListAddEdge(int point1, int point2, float distance)
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
                adjacencyList[point1].Add(new Node(distance, point2));
                adjacencyList[point2].Add(new Node(distance, point1));
                if (point1 < uniqueMeshVertLength && point2 < uniqueMeshVertLength)
                {
                    // If it's on the mesh, add the edge to edgeList
                    edgeList.Add(new Edge(point1, point2, distance));
                    edgeCount[point1]++;
                    edgeCount[point2]++;
                }

            }
        }
        /// <summary> Initialize the array of colors used to visualize changes in the data </summary>
        private void BuildColorArray()
        { // Figure out how to initialize our color32 array (Color32 is takes up much less space than Color, but works just as well for our purposes)
            if (activeMesh.colors32.Length != 0)
            { // if dijkstraMesh DOES already have a color32 array,
                colors32 = activeMesh.colors32;
            }
            else if (activeMesh.colors.Length != 0)
            { // if dijkstraMesh DOES already have a normal color array, copy that array into an array of Color32
                Color[] originalColors = activeMesh.colors;
                colors32 = new Color32[originalColors.Length];
                for (int i = 0; i < colors32.Length; i++)
                { // We can copy those original values one-by-one into our color32 array
                    colors32[i] = originalColors[i];
                }
            }
            else
            { // If dijkstraMesh does NOT yet have ANY color array,
              // Create a new array and fill it with initial values
                colors32 = new Color32[vertices.Length];
                Color32 col32 = gradient.Evaluate(0);
                colors32.FillArray(col32);
            }
        }
        /// <summary> "Flip" the mesh vertex array around so that you can lookup a vertex index by its position </summary>
        private void BuildMeshVertReverseLookup()
        {
            meshVertReverseLookup = new Dictionary<Vector3, List<int>>(uniqueMeshVertLength);  // Look up a Vector3 position and get back the indices of every vertex at that position
            for (int i = 0; i < uniqueMeshVertLength; i++)
            { // Initialize the list to be as long as the unique mesh vertices
                meshVertReverseLookup[uniqueVerts[i]] = new List<int>();
            }
            for (int i = 0; i < vertices.Length; i++)
            { // Add each vertex's position as the key, & it's index as a value
                meshVertReverseLookup[vertices[i]].Add(i);
            }
        }
        /// <summary> Add vertices along each edge according to subdivisions and make it a unique list </summary>
        private Vector3[] BuildUniqueInvisibleVerts()
        {
            List<Vector3> invisibleVerts = new List<Vector3>(vertices.Length * subdivisions);
            int v0, v1, v2;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                v0 = triangles[i]; v1 = triangles[i + 1]; v2 = triangles[i + 2];
                Vector3 vec0 = vertices[v0]; Vector3 vec1 = vertices[v1]; Vector3 vec2 = vertices[v2];
                float scaler = divider;
                for (int s = 0; s < subdivisions; s++)
                { // Go along each subdivided region from (1/4 to 2/4 to 3/4 to finally 4/4
                  // Create new verts along edges 01, 12, 20
                    Vector3 invisVert01 = Vector3.Lerp(vec0, vec1, scaler); Vector3 invisVert12 = Vector3.Lerp(vec1, vec2, scaler); Vector3 invisVert20 = Vector3.Lerp(vec2, vec0, scaler);
                    // Add vertices to invisible vertex list
                    invisibleVerts.Add(invisVert01); invisibleVerts.Add(invisVert12); invisibleVerts.Add(invisVert20);
                    scaler += divider;
                }
            }
            return invisibleVerts.Distinct().ToArray();
        }
        /// <summary> "Flip" the unique vertex array around so that you can lookup a vertex index by its position </summary>
        private void BuildUniqueVertReverseLookup()
        {
            uniqueVertReverseLookup = new Dictionary<Vector3, int>(uniqueVerts.Length);
            for (int i = 0; i < uniqueVerts.Length; i++)
            { // for each vertex, store the Vector3 position as a lookup number for it's index
                uniqueVertReverseLookup.Add(uniqueVerts[i], i);
            }
        }
        /// <summary> Find the unique vertices of the mesh and build the invisible vertices that will be used for distance calculation </summary>
        private void BuildUniqueVerts()
        {
            // Get the unique vertices from mesh.vertices
            uniqueVerts = vertices.Distinct().ToArray();
            // Store the current length of uniqueVerts, because we're going to tack our "invisible" vertices onto the end of it, but we still want to know where the "real" mesh vertices are
            uniqueMeshVertLength = uniqueVerts.Length;
            // Find the invisible vertices and attach them to the end of the uniqueVerts array
            uniqueVerts = Utils.Array.MergeArrays(uniqueVerts, BuildUniqueInvisibleVerts()); // Up to uniqueVerts.Length - 1 are mesh vertices   
        }
        /// <summary> Given a raycast hit, find the nearest unqiue mesh vert to the hit </summary>
        /// <returns> uniqueVerts index of the nearest hit vert </returns>
        public int FindNearestUniqueVert(RaycastHit hit)
        {
            int trueTriangleIndex = hit.triangleIndex * 3;
            int unique0 = uniqueVertReverseLookup[vertices[triangles[trueTriangleIndex]]]; int unique1 = uniqueVertReverseLookup[vertices[triangles[trueTriangleIndex + 1]]]; int unique2 = uniqueVertReverseLookup[vertices[triangles[trueTriangleIndex + 2]]];
            float dist0 = Vector3.Distance(hit.point, uniqueVerts[unique0]); float dist1 = Vector3.Distance(hit.point, uniqueVerts[unique1]); float dist2 = Vector3.Distance(hit.point, uniqueVerts[unique2]);
            if (dist0 <= dist1 && dist0 <= dist2) { return unique0; }
            else if (dist1 <= dist0 && dist1 <= dist2) { return unique1; }
            else if (dist2 <= dist0 && dist2 <= dist1) { return unique2; }
            return -1;
        }
        #endregion
        #region Classes
        public class DataChangeRequest
        {
            public int index;
            public double curConcentration;
            public double prevConcentration;
            /// <summary> Data change request constructor </summary>
            /// <param name="index"> Index of the point to change in DijkstraObject.uniqueVerts </param>
            /// <param name="currentConcentration"> The current scalar value at this point </param>
            /// <param name="previousConcentration"> The scalar value at this point during the previous frame </param>
            public DataChangeRequest(int index, double currentConcentration, double previousConcentration)
            {
                this.index = index;
                curConcentration = currentConcentration;
                prevConcentration = previousConcentration;
            }
        }
        public class DNode
        {
            public double weight { get; set; }
            public int index { get; set; }
            /// <summary> Construct a neighbor </summary>
            /// <param name="weight"> Weight of node </param>
            /// <param name="index"> Index of node </param>
            public DNode(double weight, int index)
            {
                this.weight = weight;
                this.index = index;
            }
            public int CompareTo(Node other)
            {
                if (weight < other.weight) return -1;
                else if (weight > other.weight) return 1;
                else return 0;
            }
            /// <summary> Represent the node as a string </summary>
            /// <returns> string representation </returns>
            public override string ToString()
            {
                return "(" + index + ", " + weight + ")";
            }
        }
        #endregion
    }

#if (UNITY_EDITOR)
    [CustomEditor(typeof(MeshInfo))]
    public class ObjectBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MeshInfo myScript = (MeshInfo)target;
            if (GUILayout.Button("BuildMeshInfo"))
            {
                myScript.InitializeAll();
            }
        }
    }
    #endif

}
