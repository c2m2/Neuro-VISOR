using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using C2M2.NeuronalDynamics.UGX;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;

namespace C2M2.NeuronalDynamics.Alg
{
    /// <summary>
    /// Simple test behaviour to reorder and write out sparsity pattern of a reordered mesh
    /// </summary>
    [System.Obsolete("For now obsolete")]
    public class GraphBehaviour : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            Grid grid1d = new Grid(new Mesh(), "1D cell");
            UGXReader.Validate = false;
            UGXReader.ReadUGX(Application.dataPath + "/StreamingAssets/HHSolver/ActiveCell/12_a/10-dkvm2.CNG_1d_2nd_ref.ugx", ref grid1d);
            grid1d.Type = OrderType.DFS;
            Algebra.ReorderMatrix(grid1d, "test13.csv");
        }
    }
}