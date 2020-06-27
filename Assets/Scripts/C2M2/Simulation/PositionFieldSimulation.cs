using System;
using UnityEngine;

namespace C2M2.Simulation
{
    using Utils;
    using Interaction.VR;
    /// <summary>
    /// Simulation of type Vector3[] for simulating positional fields
    /// </summary>
    public abstract class PositionFieldSimulation : Simulation<Vector3[], Transform[]>
    {
        public override Transform[] viz { get; protected set; }

        protected override void OnAwake()
        {
            if (!dryRun)
            {
                Vector3[] pos = new Vector3[viz.Length];

                Collider[] colliders = new Collider[viz.Length];
                for (int i = 0; i < viz.Length; i++)
                {
                    colliders[i] = viz[i].GetComponent<Collider>();
                }

                VRRaycastableColliders raycastable = gameObject.AddComponent<VRRaycastableColliders>();

                raycastable.SetSource(colliders);

                // Add custom grabbable here
            }
        }

        protected override void UpdateVisualization(in Vector3[] simulationValues)
        {
            for (int i = 0; i < simulationValues.Length; i++)
            {
                viz[i].localPosition = simulationValues[i];
            }

            UpdateVisChild(simulationValues);
        }
        /// <summary>
        /// Allow derived classes to implement custom visualization features
        /// </summary>
        protected virtual void UpdateVisChild(in Vector3[] simulationValues) { }
    }
}
