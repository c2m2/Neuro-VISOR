using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction;

namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Provides public method for instantiating clamps. Provides controls for multiple clamps
    /// </summary>
    public class NeuronClampManager : NDInteractablesManager<NeuronClamp>
    {
        public float MinPower { get { return currentSimulation.ColorLUT.GlobalMin; } }
        public float MaxPower { get { return currentSimulation.ColorLUT.GlobalMax; } }
        // Sensitivity of the clamp power control. Lower sensitivity means clamp power changes more quickly
        public float sensitivity = 5;
        public float Scaler { get { return (MaxPower - MinPower) / sensitivity; } }

        public GameObject clampPrefab = null;
        public GameObject somaClampPrefab = null;
        public bool allActive = false;
        public List<NeuronClamp> Clamps {
            get
            {
                if (currentSimulation == null) return null;
                return currentSimulation.clamps;
            }
        }

        public bool PowerClick { get; set; } = false;

        protected override void AddHitEventListeners()
        {
            HitEvent.OnHover.AddListener((hit) => Preview(hit));
            HitEvent.OnHoverEnd.AddListener((hit) => DestroyPreview());
            HitEvent.OnPress.AddListener((hit) => InstantiateNDInteractable(hit));
        }

        public override GameObject IdentifyBuildPrefab(int index)
        {
            if (currentSimulation.Neuron.somaIDs.Contains(index))
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

        protected override void PreviewCustom()
        {
            lock (currentSimulation.clampLock) Clamps.Remove(preview);
            Destroy(preview.capHolder);
        }

        /// <summary>
        /// Ensures that no clamp is placed too near to another clamp
        /// </summary>
        override public bool VertexAvailable(int index)
        {
            // minimum distance between clamps 
            float distanceBetweenClamps = currentSimulation.AverageDendriteRadius * 2;

            foreach (NeuronClamp clamp in currentSimulation.clamps)
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

                    float dist = (currentSimulation.Verts1D[clamp.FocusVert] - currentSimulation.Verts1D[index]).magnitude;
                    if (dist < distanceBetweenClamps)
                    {
                        Debug.LogWarning("Clamp too close to clamp located on vert [" + clamp.FocusVert + "].");
                        return false;
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

            float power = Time.deltaTime*PowerModifier*Scaler;
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !PowerClick) PowerClick = true;

            foreach (NeuronClamp clamp in Clamps)
            {
                if (clamp != null)
                {
                    clamp.ClampPower += power;
                    clamp.UpdateColor();
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
                    RemoveAll();
                else if (HoldCount > 0)
                    ToggleAll();
            }

            HoldCount = 0;
            PowerClick = false;
        }

        private void ToggleAll()
        {
            if (Clamps.Count > 0)
            {
                foreach (NeuronClamp clamp in Clamps)
                {
                    if (clamp != null && clamp.FocusVert != -1) {
                        if (allActive)
                            clamp.DeactivateClamp();
                        else
                            clamp.ActivateClamp();
                    }
                }

                allActive = !allActive;
            }
        }
    }
}