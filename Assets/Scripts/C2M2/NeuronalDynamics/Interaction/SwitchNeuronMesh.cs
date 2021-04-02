using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Provides a public method for switching NDSimulation meshes
    /// </summary>
    /// <remarks>
    /// Can be used to implement buttons that switch NDSimulation visual inflation
    /// </remarks>
    public class SwitchNeuronMesh : MonoBehaviour
    {
        public NDSimulation ndSimulation = null;
        public double inflation = 1;

        public void Switch()
        {
            if (ndSimulation != null)
            {
                ndSimulation.SwitchVisualMesh(inflation);
            }
            else Debug.LogError("No neuron simulation given");
        }
    }
}