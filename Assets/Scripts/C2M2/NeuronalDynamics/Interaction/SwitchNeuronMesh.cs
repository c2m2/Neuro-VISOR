using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Provides a public method for switching NDSimulation meshes
    /// </summary>
    /// <remarks>
    /// Can be used to implement buttons that switch NDSimulation visual and collider inflation
    /// </remarks>
    public class SwitchNeuronMesh : MonoBehaviour
    {
        public NDSimulation ndSimulation = null;
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