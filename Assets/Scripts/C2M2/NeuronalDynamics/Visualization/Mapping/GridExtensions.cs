using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        /// <param name="name">Name of subset</param>
        public static int[] GetSubsetIndices(this Grid grid, in string name) => grid.Subsets[name].Indices;


        /// <summary>
        /// Returns the name of a subset
        /// </summary>
        /// <param name="subset">A subset</param>
        public static string GetSubsetName(this Grid grid, in Subset subset) => subset.Name;

    }
}