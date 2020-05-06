using UnityEngine;
using System;

namespace C2M2
{
    namespace SimulationScripts
    {
        /// <summary>
        /// Given an existing mesh, store a scalar for each vertex.
        /// Good for testing interactionf features
        /// </summary>
        public class ExampleSimulation : ScalarFieldSimulation
        {
            private double[] scalars;

            public override double[] GetValues() => scalars;
            public override void SetValues(Tuple<int, double>[] newValues)
            {
                foreach(Tuple<int, double> newVal in newValues)
                {
                    scalars[newVal.Item1] += newVal.Item2;
                }
            }
            #region Unity Methods

            #endregion
            protected override void Solve()
            { // Do nothing, essentially
                while (true)
                {
                    for (int i = 0; i < scalars.Length; i++)
                    {
                    }
                }
            }
            protected override Mesh BuildMesh()
            {
                MeshFilter meshf = GetComponent<MeshFilter>() ?? throw new MeshFilterNotFoundException();
                Mesh mesh = meshf.sharedMesh ?? throw new MeshNotFoundException();
                scalars = new double[mesh.vertexCount];
                return mesh;
            }
        }
    }
}
