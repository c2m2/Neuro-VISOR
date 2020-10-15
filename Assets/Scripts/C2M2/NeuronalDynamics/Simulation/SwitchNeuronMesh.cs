using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class SwitchNeuronMesh : NDControlButton
    {
        [Tooltip("If true, changes the visual Mesh. Otherwise changes MeshCollider")]
        public bool changeViz = true;
        public double inflation = 1;

        public void Switch()
        {
            if (ndSimulation != null)
            {
                if (changeViz) ndSimulation.SwitchMesh(inflation);
                else ndSimulation.SwitchColliderMesh(inflation);
            }
            else Debug.LogError("No neuron simulation given");
        }
    }
}