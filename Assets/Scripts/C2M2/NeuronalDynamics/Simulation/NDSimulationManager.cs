using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
using C2M2.Visualization;
namespace C2M2.NeuronalDynamics.Simulation
{
    public class NDSimulationManager : MeshSimulationManager
    {
        private bool paused = false;
        public bool Paused
        {
            get
            {
                return paused;
            }
            set
            {
                paused = value;
                foreach(NDSimulation sim in ActiveSimulations)
                {
                    sim.paused = paused;
                }
            }
        }
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