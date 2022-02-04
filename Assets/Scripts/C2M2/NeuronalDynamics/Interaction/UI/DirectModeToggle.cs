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
                Debug.Log("Direct interaction turned on.");
                foreach (NDSimulation sim in GameManager.instance.activeSims)
                {
                    sim.raycastEventManager.LRTrigger = sim.defaultRaycastEvent;
                }
            }
        }
    }
}