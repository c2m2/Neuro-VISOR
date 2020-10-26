using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.NeuronalDynamics.Interaction {
    public class NDClampModeSwitch : NDControlButton
    {
        public void Toggle()
        {
            ndSimulation.ClampMode = !ndSimulation.ClampMode;
        }
    }
}