using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction.UI;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public abstract class NDFeatureToggle : RaycastToggle
    {
        public NDBoardController boardController = null;

        public List<NDSimulation> Sims
        {
            get { return GameManager.instance.simulationManager.ActiveSimulations; }
        }

        private void Awake()
        {
            if (boardController == null)
            {
                boardController = GetComponentInParent<NDBoardController>();
                if (boardController == null)
                {
                    Debug.LogError("No simulation controller found.");
                }
            }
        }
    }
}