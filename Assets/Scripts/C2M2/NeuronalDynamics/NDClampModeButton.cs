using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction
{
    public class NDClampModeButton : MonoBehaviour
    {
        public NDSimulation sim = null;

        public void Toggle(bool enable)
        {
            if(sim == null)
            {
                Debug.LogError("No simulation found for NDClampModeButton!");
            }
            else
            {
                sim.ClampMode = enable;
            }
        }
    }
}