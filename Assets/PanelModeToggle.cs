using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Visualization;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class PanelModeToggle : NDFeatureToggle
    {
        public NDGraphManager Manager
        {
            get
            {
                return GameManager.instance.ndGraphManager;
            }
        }

        public override void OnToggle(RaycastHit hit, bool toggled)
        {
            if (toggled)
            {
                // Set NDSimulation's event to that event
                Sim.raycastEventManager.LRTrigger = Manager.hitEvent;
                Debug.Log("Panel interaction turned on.");
            }
        }
    }
}