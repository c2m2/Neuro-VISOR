using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction.UI;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public abstract class NDFeatureToggle : RaycastToggle
    {
        public NDSimulationController simController = null;
        public NDSimulation Sim
        {
            get { return simController.sim; }
        }

        private void Awake()
        {
            if (simController == null)
            {
                simController = GetComponentInParent<NDSimulationController>();
                if (simController == null)
                {
                    Debug.LogError("No simulation controller found.");
                }
            }
        }
    }
}