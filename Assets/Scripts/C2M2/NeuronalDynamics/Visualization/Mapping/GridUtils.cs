using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.UGX
{
    /// <summary>
    /// Grid utility methods
    /// </summary>
    static class GridUtils
    {
        /// <summary>
        /// Prints available subsets and indices for the given grid
        /// </summary>
        /// <param name="grid">A grid</param>
        public static void AvailableSubsets(in Grid grid) => UnityEngine.Debug.Log(grid.Subsets);
    }
}
