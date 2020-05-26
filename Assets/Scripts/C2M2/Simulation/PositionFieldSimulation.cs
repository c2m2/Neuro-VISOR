using System;
using UnityEngine;

namespace C2M2.Simulation
{
    using Utils.MeshUtils;
    /// <summary>
    /// Simulation of type Vector3[] for simulating positional fields
    /// </summary>
    public abstract class PositionFieldSimulation : Simulation<Vector3[]>
    {
        private Transform[] transforms;
        protected override void OnAwake()
        {
            transforms = BuildTransforms();
            Vector3[] pos = new Vector3[transforms.Length];

            // Make each transform a child of this gameobject so the hierarchy isn't flooded
            for(int i = 0; i < transforms.Length; i++)
            {
                // transforms[i].parent = transform;
                // pos[i] = transforms[i].position;
            }

            // Rescale the field and reapply
            //pos = RescaleField(pos);
            /*for(int i = 0; i < transforms.Length; i++)
            {
                transforms[i].position = pos[i];
            }*/
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
