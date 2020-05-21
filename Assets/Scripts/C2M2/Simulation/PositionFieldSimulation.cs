using System;
using UnityEngine;

namespace C2M2.Simulation
{
    /// <summary>
    /// Simulation of type Vector3[] for simulating positional fields
    /// </summary>
    public abstract class PositionFieldSimulation : Simulation<Vector3[]>
    {
        private Transform[] transforms;
        protected override void OnAwake()
        {
            transforms = BuildTransforms();
            // Make each transform a child of this gameobject so the hierarchy isn't flooded
            for(int i = 0; i < transforms.Length; i++)
            {
                transforms[i].parent = transform;
            }
        }

        protected abstract Transform[] BuildTransforms();

        protected override void UpdateVisualization(in Vector3[] simulationValues)
        {

            for (int i = 0; i < simulationValues.Length; i++)
            {
                transforms[i].position = simulationValues[i];
            }
        }
    }
}
