using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Simulation
{
    public class SwitchNeuronMesh : MonoBehaviour
    {
        public NeuronSimulation1D neuronSimulation1D;
        [Tooltip("If true, changes the visual Mesh. Otherwise changes MeshCollider")]
        public bool changeViz = true;
        public NeuronSimulation1D.MeshScaling meshScale = NeuronSimulation1D.MeshScaling.x1;

        public void Switch()
        {
            if (neuronSimulation1D != null)
            {
                if (changeViz) neuronSimulation1D.SwitchMesh((int)meshScale);
                else neuronSimulation1D.SwitchColliderMesh((int)meshScale);
            }
            else Debug.LogError("No neuron simulation given");
        }
    }
}