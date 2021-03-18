using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class PanelModeToggle : NDToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggled)
        {
            // Get panel event from panel manager

            if (toggled)
            {
                // Set NDSimulation's event to that event

            }
        }
    }
}