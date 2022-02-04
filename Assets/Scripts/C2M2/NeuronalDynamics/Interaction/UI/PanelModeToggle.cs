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
                // Set NDSimulation's event to that event
                foreach(NDSimulation sim in GameManager.instance.simulationManager.ActiveSimulations)
                {
                    sim.raycastEventManager.LRTrigger = sim.graphManager.hitEvent;
                }
                
                Debug.Log("Panel interaction turned on.");
            }
        }
    }
}