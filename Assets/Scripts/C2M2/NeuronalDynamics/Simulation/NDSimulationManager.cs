using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
namespace C2M2.NeuronalDynamics.Simulation
{
    public class NDSimulationManager : MonoBehaviour
    {
        public List<NDSimulation> ActiveSimulations
        {
            get
            {
                List <NDSimulation> activeSims = new List<NDSimulation>(GameManager.instance.activeSims.Count);
                foreach(Interactable sim in GameManager.instance.activeSims)
                {
                    activeSims.Add((NDSimulation)sim);
                }
                return activeSims;
            }
        }
    }
}