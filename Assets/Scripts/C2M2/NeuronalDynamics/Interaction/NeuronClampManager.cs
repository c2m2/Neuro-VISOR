using C2M2.NeuronalDynamics.Simulation;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Provides public method for instantiating clamps. Provides controls for multiple clamps
    /// </summary>
    public class NeuronClampManager : NDInteractablesManager<NeuronClamp>
    {
        public List<NeuronClamp> clamps = new List<NeuronClamp>();
        public GameObject clampPrefab = null;
        public GameObject somaClampPrefab = null;
        public bool allActive = false;

        public bool PowerClick { get; set; } = false;

        private void OnDestroy()
        {
            foreach (NeuronClamp clamp in clamps)
            {
                Destroy(clamp);
            }
        }

        public override GameObject IdentifyBuildPrefab(NDSimulation sim, int index)
        {
            if (sim.Neuron.somaIDs.Contains(index))
            {
                if (somaClampPrefab == null) Debug.LogError("No Soma Clamp prefab found");
                else return somaClampPrefab;
            }
            else
            {
                if (clampPrefab == null) Debug.LogError("No Clamp prefab found");
                else return clampPrefab;
            }
            return null;
        }

        /// <summary>
        /// Ensures that no clamp is placed too near to another clamp
        /// </summary>
        override public bool VertexAvailable(NDSimulation sim, int index)
        {
            // minimum distance between clamps 
            float distanceBetweenClamps = sim.AverageDendriteRadius * 2;

            lock(sim.clampLock)
            {
                foreach (NeuronClamp clamp in clamps)
                {
                    // If there is a clamp on that 1D vertex, the spot is not open
                    if (clamp.FocusVert == index)
                    {
                        Debug.LogWarning("Clamp already exists on focus vert [" + index + "]");
                        return false;
                    }
                    // If there is a clamp within distanceBetweenClamps, the spot is not open
                    else
                    {
                        float dist = (sim.Verts1D[clamp.FocusVert] - sim.Verts1D[index]).magnitude;
                        if (dist < distanceBetweenClamps)
                        {
                            Debug.LogWarning("Clamp too close to clamp located on vert [" + clamp.FocusVert + "].");
                            return false;
                        }
                    }
                }
            }
            
            return true;
        }

        public void MonitorHighlight()
        {
            // Highlight all clamps if either hand trigger is held down
            HighlightAll(HighLightHold);
        }

        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void MonitorGroupInput()
        {
            if (InteractHold)
                HoldCount+=Time.deltaTime;
            else
                CheckInputResult();

            float power = Time.deltaTime * PowerModifier; // TODO *Scaler;
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !PowerClick) PowerClick = true;

            foreach (NDSimulation ndSim in GameManager.instance.activeSims)
            {
                lock (ndSim.clampLock)
                {
                    foreach (NeuronClamp clamp in ndSim.clampManager.clamps)
                    {
                        if (clamp != null)
                        {
                            clamp.ClampPower += power;
                            clamp.UpdateColor();
                        }
                    }
                }
            }
        }

        public void ResetGroupInput()
        {
            CheckInputResult();
            HighlightAll(false);
        }

        private void CheckInputResult()
        {
            if (!PowerClick)
            {
                if (HoldCount >= DestroyCount)
                    foreach (NeuronClamp clamp in clamps)
                    {
                        Destroy(clamp);
                    }
                else if (HoldCount > 0)
                    ToggleAll();
            }

            HoldCount = 0;
            PowerClick = false;
        }

        public void HighlightAll(bool highlight)
        {
            if (clamps.Count > 0)
            {
                foreach (NeuronClamp clamp in clamps)
                {
                    clamp.Highlight(highlight);
                }
            }
        }

        private void ToggleAll()
        {
            foreach (NDSimulation ndSim in GameManager.instance.activeSims)
            {
                lock (ndSim.clampLock)
                {
                    if (ndSim.clampManager.clamps.Count > 0)
                    {
                        foreach (NeuronClamp clamp in ndSim.clampManager.clamps)
                        {
                            if (clamp != null && clamp.FocusVert != -1)
                            {
                                if (allActive) clamp.DeactivateClamp();
                                else clamp.ActivateClamp();
                            }
                        }
                        allActive = !allActive;
                    }
                }
            }
        }
    }
}