using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
using C2M2.Visualization;
using C2M2.Interaction;
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

        public enum FeatureState { Direct = 0, Clamp = 1, Plot = 2, Synapse = 3 };
        private FeatureState featState = FeatureState.Direct;
        public FeatureState FeatState
        {
            get
            {
                return featState;
            }
            set
            {
                featState = value;

                foreach (NDSimulation sim in ActiveSimulations)
                {
                    switch (featState)
                    {
                        case (FeatureState.Direct):
                            sim.raycastEventManager.LRTrigger = sim.defaultRaycastEvent;
                            break;
                        case (FeatureState.Clamp):
                            sim.raycastEventManager.LRTrigger = sim.clampManager.hitEvent;
                            break;
                        case (FeatureState.Plot):
                            sim.raycastEventManager.LRTrigger = sim.graphManager.hitEvent;
                            break;
                        case (FeatureState.Synapse):

                            break;
                    }
                }
            }
        }
    }
}