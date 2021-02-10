using UnityEngine;
using System;
using C2M2.Utils.Exceptions;

namespace C2M2.Simulation.Samples
{
    using Interaction;
    /// <summary>
    /// Store a scalar for each vertex on an existing MeshFilter.mesh
    /// </summary>
    /// <remarks>
    /// Good for testing interaction features
    /// </remarks>
    public class MeshSimulation : Simulation.MeshSimulation
    {
        public override float GetSimulationTime() { return 0; }

        private double[] scalars;

        public override double[] GetValues() => scalars;
        public override void SetValues(RaycastHit hit)
        {
            Tuple<int, double>[] newValues = ((RaycastSimHeaterDiscrete)Heater).HitToTriangles(hit);

            foreach (Tuple<int, double> newVal in newValues)
            {
                scalars[newVal.Item1] += newVal.Item2;
            }
        }

        #region Unity Methods

        #endregion

        protected override void SolveStep(int t)
        { // Do nothing, essentially
            while (true)
            {
                for (int i = 0; i < scalars.Length; i++)
                {
                }
            }
        }

        protected override Mesh BuildVisualization()
        {
            MeshFilter meshf = GetComponent<MeshFilter>() ?? throw new MeshFilterNotFoundException();
            Mesh mesh = meshf.sharedMesh ?? throw new MeshNotFoundException();
            scalars = new double[mesh.vertexCount];
            return mesh;
        }
    }
}