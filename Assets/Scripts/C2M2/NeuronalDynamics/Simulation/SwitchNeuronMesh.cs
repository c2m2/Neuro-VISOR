using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Simulation
{
    public class SwitchNeuronMesh : MonoBehaviour
    {
        public NeuronSimulation1D neuronSimulation1D = null;
        [Tooltip("If true, changes the visual Mesh. Otherwise changes MeshCollider")]
        public bool changeViz = true;
        public double inflation = 1;

        public void Switch()
        {
            if (neuronSimulation1D != null)
            {
                if (changeViz) neuronSimulation1D.SwitchMesh(inflation);
                else neuronSimulation1D.SwitchColliderMesh(inflation);
            }
            else Debug.LogError("No neuron simulation given");
        }
    }
}