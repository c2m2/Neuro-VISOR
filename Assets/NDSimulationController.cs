using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDSimulationController : MonoBehaviour
    {
        public NDSimulation sim = null;

        private void Start()
        {
            if(sim == null)
            {
                Debug.LogError("No simulation given.");
                Destroy(gameObject);
            }
        }
    }
}