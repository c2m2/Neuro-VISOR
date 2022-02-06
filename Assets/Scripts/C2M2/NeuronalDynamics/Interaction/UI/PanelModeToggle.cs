using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.Visualization;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class PanelModeToggle : NDFeatureToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggled)
        {
            if (toggled)
            {
                GameManager.instance.simulationManager.FeatState = NDSimulationManager.FeatureState.Plot;
                
                Debug.Log("Panel interaction turned on.");
            }
        }
    }
}