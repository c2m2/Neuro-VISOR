using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
namespace C2M2.NeuronalDynamics.Simulation
{
    public class NDSimulationManager : MeshSimulationManager
    {
        public bool Paused { get; set; } = false;
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

        public SynapseManager synapseManager = null;

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
                        case FeatureState.Direct:
                            sim.raycastEventManager.LRTrigger = sim.defaultRaycastEvent;
                            break;
                        case FeatureState.Clamp:
                            sim.raycastEventManager.LRTrigger = sim.clampManager.HitEvent;
                            break;
                        case FeatureState.Plot:
                            sim.raycastEventManager.LRTrigger = sim.graphManager.HitEvent;
                            break;
                        case FeatureState.Synapse:
                            sim.raycastEventManager.LRTrigger = synapseManager.HitEvent;
                            break;
                    }
                }

                string s = "";
                switch (featState)
                {
                    case FeatureState.Direct:
                        s = "Direct mode";
                        break;
                    case FeatureState.Clamp:
                        s = "Clamp mode";
                        break;
                    case FeatureState.Plot:
                        s = "Plot mode";
                        break;
                    case FeatureState.Synapse:
                        s = "Synapse mode";
                        break;
                }

                Debug.Log(s + " active on all cells.");
            }
        }

        protected override void Awake()
        {
            // Add synapse manager
            var synapseManagerObj = Instantiate(GameManager.instance.synapseManagerPrefab);
            synapseManagerObj.transform.parent = transform;
            synapseManager = synapseManagerObj.GetComponent<SynapseManager>();

            base.Awake();
        }
    }
}