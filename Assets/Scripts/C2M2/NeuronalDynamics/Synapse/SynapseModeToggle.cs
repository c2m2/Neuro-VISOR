using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class SynapseModeToggle : NDFeatureToggle
    {
        public NDSimulation Sim { get { return GameManager.instance.simulationManager.ActiveSimulations[0]; } }
        public override void OnToggle(RaycastHit hit, bool toggle)
        {
            if (toggle)
            {

                GameManager.instance.simulationManager.FeatState = NDSimulationManager.FeatureState.Synapse;
            }
        }
    }
}