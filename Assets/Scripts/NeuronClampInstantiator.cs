﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Provides public method for instantiating clamps. Provides controls for multiple clamps
    /// </summary>
    public class NeuronClampInstantiator : MonoBehaviour
    {
        public GameObject clampPrefab = null;
        public bool allActive = false;
        public NDSimulation simulation { get; private set; } = null;
        public List<NeuronClamp> Clamps {
            get
            {
                if (simulation == null) return null;
                return simulation.clamps;
            }
        }
        public Color32 inactiveCol = Color.black;

        public void InstantiateClamp(RaycastHit hit)
        {
            // Make sure we have a valid prefab and simulation
            if (clampPrefab == null) Debug.LogError("No Clamp prefab found");
            var sim = hit.collider.GetComponentInParent<NDSimulation>();
            if (sim == null) return;
            if (simulation == null) simulation = sim;
            // Only allow one simulation
            if (sim != simulation) return;


            var clampObj = Instantiate(clampPrefab, sim.transform);
            NeuronClamp clamp = clampObj.GetComponentInChildren<NeuronClamp>();

            clamp.InactiveCol = inactiveCol;

            clamp.ReportSimulation(sim, hit);
        }

        private int destroyCount = 50;
        private int holdCount = 0;
        private float thumbstickScaler = 1;
        /// <summary>
        /// Pressing this button toggles clamps on/off. Holding this button down for long enough destroys the clamp
        /// </summary>
        public OVRInput.Button toggleDestroyOVR = OVRInput.Button.Two;
        public OVRInput.Button toggleDestroyOVRS = OVRInput.Button.Four;
        private bool PressedToggleDestroy
        {
            get
            {
                if (GameManager.instance.vrIsActive)
                    return (OVRInput.Get(toggleDestroyOVR) || OVRInput.Get(toggleDestroyOVRS));
                else return true;
            }
        }
        public KeyCode powerModifierPlusKey = KeyCode.UpArrow;
        public KeyCode powerModifierMinusKey = KeyCode.DownArrow;
        private float PowerModifier
        {
            get
            {
                if (GameManager.instance.vrIsActive)
                {
                    // Use the value of whichever joystick is held up furthest
                    float y1 = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
                    float y2 = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
                    float scaler = (y1 + y2);

                    return thumbstickScaler * scaler;
                }
                else
                {
                    if (Input.GetKey(powerModifierPlusKey)) return thumbstickScaler;
                    if (Input.GetKey(powerModifierMinusKey)) return -thumbstickScaler;
                    else return 0;
                }
            }
        }
        private bool powerClick = false;

        public OVRInput.Button highlightOVR = OVRInput.Button.PrimaryHandTrigger;
        public OVRInput.Button highlightOVRS = OVRInput.Button.SecondaryHandTrigger;
        private bool PressedHighlight
        {
            get
            {
                if (GameManager.instance.vrIsActive)
                    return (OVRInput.Get(highlightOVR) || OVRInput.Get(highlightOVRS));
                else return false;
            }
        }

        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void MonitorInput()
        {
            if (PressedToggleDestroy)
                holdCount++;
            else
                CheckInput();

            // Highlight all clamps if either hand trigger is held down
            if (PressedHighlight)
            {
                if (highlightPrev == false)
                {
                    HighlightAll(true);
                    highlightPrev = true;
                }
            }
            else
            {
                if (highlightPrev == true)
                {
                    HighlightAll(false);
                    highlightPrev = false;
                }
            }

            float power = PowerModifier;
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !powerClick) powerClick = true;

            foreach (NeuronClamp clamp in Clamps)
            {
                if (clamp != null) clamp.clampPower += power;
            }       
        }

        public void ResetInput()
        {
            CheckInput();
        }

        private void CheckInput()
        {
            if (!powerClick)
            {
                if (holdCount >= destroyCount)
                    DestroyAll();
                else if (holdCount > 0)
                    ToggleAll();
            }

            holdCount = 0;
            powerClick = false;
        }

        private void ToggleAll()
        {
            if (Clamps.Count > 0)
            {
                foreach (NeuronClamp clamp in Clamps)
                {
                    if (clamp != null && clamp.focusVert != -1) {
                        if (allActive)
                            clamp.DeactivateClamp();
                        else
                            clamp.ActivateClamp();
                    }
                }

                allActive = !allActive;
            }
        }
        private void DestroyAll()
        {
            if (Clamps.Count > 0)
            {
                foreach (NeuronClamp clamp in Clamps)
                {
                    if (clamp != null && clamp.focusVert != -1)
                        Destroy(clamp.transform.parent.gameObject);
                }
            }
        }
        private bool highlightPrev = false;
        private void HighlightAll(bool highlight)
        {
            if (Clamps.Count > 0)
            {
                foreach (NeuronClamp clamp in Clamps)
                {
                   if(clamp.highlightObj != null)
                    {
                        clamp.highlightObj.SetActive(highlight);
                    }
                }
            }
        }
    }
}