using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction.UI;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction
{
    public abstract class NDFeatureToggle : RaycastToggle
    {
        public NDSimulation sim = null;
    }
}