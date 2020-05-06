using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Reordering = System.Collections.Generic.Dictionary<int, int>;
using Vertex = C2M2.UGX.Vertex;
using System.Linq;
using Grid = C2M2.UGX.Grid;
using System;

namespace C2M2 {
  namespace UGX {
    internal enum OrderType : byte 
    {
       Identity, // Identity
       DFS, // Simple DFS
       CMK // Cuthill-McKee
    }
 
    internal static class Algebra {
      /// GetDoFIndex
      /// <summary>
      /// Returns the associated degree of freedom index in the matrix.
      /// Note: An optional re-ordering on the vertices can be applied by using r
      /// </summary>
   
      public static int GetDoFIndex(in Vertex v, in Reordering r=null) {
	return GetDoFIndex(v.Id, r);
      }
      
      /// GetDoFIndex
      /// <summary>
      /// Returns the associated degree of freedom index in the matrix.
      /// Note: An optional re-ordering on the vertices can be applied by using r
      /// </summary>
      internal static int GetDoFIndex(in int id, in Reordering r=null) {
	return r?[id] ?? id;
      }
      
      /// GetVertexIndex
      /// <summary>
      /// Returns the vertex index in the mesh
      /// </summary>
      internal static int GetVertexIndex(in Vertex v, in Reordering r=null) {
	return GetVertexIndex(v.Id, r);
      }
      
      /// GetVertexIndex
      /// <summary>
      /// Returns the vertex index in the mesh
      /// </summary>
      internal static int GetVertexIndex(in int id, in Reordering r = null) {
	return r?[id] ?? id; 
      }
      
      /// ReorderMatrix
      /// <summary>
      /// Reorders a matrix (Just a test method)
      /// </summary>
      internal static Reordering ReorderMatrix(Grid grid, in string filename="test.csv") {
	UnityEngine.Debug.LogError("Reordering matrix!");
	
	 List<Vertex> vertices2 = grid.Vertices;
	 List<Edge> edges2 = grid.Edges;
	 List<Vertex> newVertices = new List<Vertex>();
		
	var ordering = new Reordering();
	if (grid.Type == C2M2.UGX.OrderType.Identity) {
	  foreach (int index in Enumerable.Range(0, vertices2.Count)) {
	    ordering[index] = index;
	  }
	}
		
	if (grid.Type == C2M2.UGX.OrderType.DFS) {
	  var vertices = Enumerable.Range(0, vertices2.Count);
          UnityEngine.Debug.LogError("num verts: " + vertices2.Count);
            
          List<Tuple<int, int>> edges = new List<Tuple<int, int>>();
	  foreach (Edge edge in edges2) {
	    edges.Add(Tuple.Create(edge.From.Id, edge.To.Id));
	  }
       
          var graph = new C2M2.ALG.Graph<int>(vertices, edges);
          var algorithms = new C2M2.ALG.Algorithms();

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
}
