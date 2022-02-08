using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI {
    public class DirectModeToggle : NDFeatureToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggled)
        {
            if (toggled)
            {
                GameManager.instance.simulationManager.FeatState = NDSimulationManager.FeatureState.Direct;
            }
        }
    }
}