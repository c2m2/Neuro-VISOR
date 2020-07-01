using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace C2M2.NeuronalDynamics.UGX
{
    /// <summary>
    /// Grid extension methods
    /// </summary>
    static class GridExtensions
    {
        /// <summary>
        /// Returns the indices of a subset specified by name
        /// </summary>
        /// <param name="name"> Name of subset </param>
        /// <param name="grid"> A grid instance </param>
        public static int[] GetSubsetIndices(this Grid grid, in string name) => grid.Subsets[name].Indices;
        
        /// <summary>
        /// Returns the name of the subset the vertex belongs to
        /// <param name="grid">Name of grid</param>
        /// <param name="vertex">Vertex</param>
        /// <param name="subsetName"> Subset name </param>
        /// </summary>
        /// Returns false if subset not present in any available subset in this grid
        public static bool GetSubsetName(this Grid grid, in Vertex vertex, out string subsetName) {
          foreach(KeyValuePair<string, Subset> subset in grid.Subsets.subsets) {
            int[] indices = GetSubsetIndices(grid, subset.Key);
            if (indices.Contains(vertex.Id)) {
              subsetName = subset.Key;
            }
          }
          subsetName = "UNASSIGNED ELEMENT";
          return false;
        }

        /// <summary>
        /// Returns the name of the subset the vertex index belongs to
        /// <param name="grid">Name of grid</param>
        /// <param name="id">Vertex id</param>
        /// <param name="subsetName"> Subset name </param>
        /// </summary>
        /// Returns false if subset not present in any available subset in this grid
        /// <return> bool </return>
        public static bool GetSubsetName(this Grid grid, in int index, out string subsetName) {
          foreach(KeyValuePair<string, Subset> subset in grid.Subsets.subsets) {
            int[] indices = GetSubsetIndices(grid, subset.Key);
            if (indices.Contains(index)) {
              subsetName = subset.Key;
              return true;
            }
          }
          subsetName = "UNASSIGNED ELEMENT";
          return false;
        }

        /// <summary>
        /// Returns the name of a subset
        /// </summary>
        /// <param name="grid">Name of grid</param>
        /// <param name="subset">A subset</param>
        public static string GetSubsetName(this Grid grid, in Subset subset) => subset.Name;
    }
}
