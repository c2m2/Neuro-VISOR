using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction {
    public class DirectModeToggle : NDFeatureToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggled)
        {
            // Check for valid simulation, buttons
            if (sim == null)
            {
                Debug.LogError("No simulation given to NDClampModeButtonController!");
                return;
            }

            if (toggled)
            {
                Debug.Log("Direct interaction turned on.");
                sim.raycastEventManager.LRTrigger = sim.defaultRaycastEvent;
            }
        }
    }
}