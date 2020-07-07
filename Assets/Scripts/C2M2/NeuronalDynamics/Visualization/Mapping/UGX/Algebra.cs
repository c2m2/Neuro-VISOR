using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Reordering = System.Collections.Generic.Dictionary<int, int>;
using System.Linq;
using System;
using C2M2.NeuronalDynamics.Alg;

namespace C2M2.NeuronalDynamics.UGX
{
    /// <summary>
    /// Different choices for reordering the DOF matrix
    /// </summary>
    internal enum OrderType : byte 
    {
        Identity, // Identity
        DFS, // Simple DFS
        CMK // Cuthill-McKee
    }
 
    /// <summary>
    /// Utility class to provide access to vertex and DOF indices
    internal static class Algebra {
        /// GetDoFIndex
        /// <summary>
        /// Returns the associated degree of freedom index in the matrix.
        /// Note: An optional re-ordering on the vertices can be applied by using r
        /// </summary>
        /// <param name="v">Vertex index</param>
        /// <param name="r">Reordering type</param>
        /// <return> int </return>
        /// Returns the DoF index
        public static int GetDoFIndex(in Vertex v, in Reordering r=null) {
            return GetDoFIndex(v.Id, r);
        }
      
        /// GetDoFIndex
        /// <summary>
        /// Returns the associated degree of freedom index in the matrix.
        /// Note: An optional re-ordering on the vertices can be applied by using r
        /// </summary>
        /// <param name="id">DOF index</param>
        /// <param name="r">Reordering type</param>
        /// <return> int </return>
        /// Returns the DoF index
        internal static int GetDoFIndex(in int id, in Reordering r=null) {
            return r?[id] ?? id;
        }
      
        /// GetVertexIndex
        /// <summary>
        /// Returns the vertex index in the mesh
        /// </summary>
        /// <param name="v">Vertex index</param>
        /// <param name="r">Reordering type</param>
        /// <return> int </return>
        /// Returns the vertex index
        internal static int GetVertexIndex(in Vertex v, in Reordering r=null) {
            return GetVertexIndex(v.Id, r);
        }
      
        /// GetVertexIndex
        /// <summary>
        /// Returns the vertex index in the mesh
        /// </summary>
        /// <param name="id">DOF index</param>
        /// <param name="r">Reordering type</param>
        /// <return> int </return>
        /// Returns the vertex index
        internal static int GetVertexIndex(in int id, in Reordering r = null) {
            return r?[id] ?? id; 
        }
      
        /// ReorderMatrix
        /// <summary>
        /// Reorders a matrix provided by a grid in UGX format 
        /// This is a simple test method not to be used in production!
        /// It reads the grid and produces an adjacency matrix as CSV file
        /// to test for the sparsity pattern (reordering) of the grid.
        /// </summary>
        /// <param name="grid">a grid </param>
        /// <param name="filename">output filename</param>
        /// <return> Reordering </return>
        /// Returns the matrix reordering
        internal static Reordering ReorderMatrix(Grid grid, in string filename="test.csv")
        {
           UnityEngine.Debug.LogError("Reordering matrix!");
   
           List<Vertex> vertices2 = grid.Vertices;
           List<Edge> edges2 = grid.Edges;
           List<Vertex> newVertices = new List<Vertex>();
      
            var ordering = new Reordering();
            if (grid.Type == OrderType.Identity) {
               foreach (int index in Enumerable.Range(0, vertices2.Count)) {
                   ordering[index] = index;
               }
            }
      
            if (grid.Type == OrderType.DFS) {
               var vertices = Enumerable.Range(0, vertices2.Count);
                UnityEngine.Debug.LogError("num verts: " + vertices2.Count);
            
                List<Tuple<int, int>> edges = new List<Tuple<int, int>>();
               foreach (Edge edge in edges2) {
                   edges.Add(Tuple.Create(edge.From.Id, edge.To.Id));
               }
       
                var graph = new Graph<int>(vertices, edges);
                var algorithms = new Algorithms();

                HashSet<int> indices = algorithms.DFS(graph, grid.Subsets["soma"].Indices[0]);
                UnityEngine.Debug.LogError("real old soma index: " + grid.Subsets["soma"].Indices[0]);
                /// new vertex Ids
                int k = 0;
                UnityEngine.Debug.LogError("Num indices: " + indices.Count);
            
                foreach (int index in indices) { 
                   ordering[index] = k;
                  k++;
               }
           }
       
           int[,] arr= new int[vertices2.Count, vertices2.Count];
           foreach (Edge edge in edges2) {
               arr[ordering[edge.From.Id], ordering[edge.To.Id]] = 1;
               arr[ordering[edge.To.Id], ordering[edge.From.Id]] = 1;
           }
      
           for (int i = 0; i < vertices2.Count; i++) {
               arr[i,i] = 1;
           }   
      
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
           {
               for (int i = 0; i < arr.GetLength(0); i++)
               {
                   for (int j = 0; j < arr.GetLength(1); j++)
                   {
                     file.Write(string.Format("{0} ", arr[i, j]));
                   }
                   file.WriteLine(Environment.NewLine + Environment.NewLine);
               }
           }

            return ordering;
        }
    }
}
