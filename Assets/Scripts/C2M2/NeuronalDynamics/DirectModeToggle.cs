using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction.UI {
    public class DirectModeToggle : NDFeatureToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggled)
        {
            if (toggled)
            {
                Debug.Log("Direct interaction turned on.");
                Sim.raycastEventManager.LRTrigger = Sim.defaultRaycastEvent;
            }
        }
    }
}