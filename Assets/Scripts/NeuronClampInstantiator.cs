using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class NeuronClampInstantiator : MonoBehaviour
    {
        public GameObject clampPrefab = null;
        public bool allActive = false;
        public List<NeuronClamp> clamps = new List<NeuronClamp>();
        public List<int> ClampInds
        {
            get
            {
                List<int> inds = new List<int>();
                if (clamps.Count > 0) {
                    foreach (NeuronClamp clamp in clamps)
                    {
                        if(clamp != null && clamp.focusVert != -1)
                        {
                            inds.Add(clamp.focusVert);
                        }
                    }
                }
                return inds;
            }
        }
        private List<NeuronClamp> clampGarbage = new List<NeuronClamp>();

        private void Start()
        {
            // Monitors for removed/null clamps
            StartCoroutine(ClampGC());
            StartCoroutine(CheckForDuplicates(0.5f));
        }

        public void InstantiateClamp(RaycastHit hit)
        {
            // Make sure we have a valid prefab and simulation
            if (clampPrefab == null) Debug.LogError("No Clamp prefab found");
            var sim = hit.collider.GetComponentInParent<NDSimulation>();
            if (sim == null) return;

            var clamp = Instantiate(clampPrefab, sim.transform);
           
            clamp.transform.position = hit.point;
            clamps.Add(clamp.GetComponentInChildren<NeuronClamp>());
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
        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void MonitorInput()
        {
            if (PressedToggleDestroy)
                holdCount++;
            else
                CheckInput();

            float power = PowerModifier;
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !powerClick) powerClick = true;

            foreach (NeuronClamp clamp in clamps)
            {
                if (clamp != null) clamp.clampPower += power;
                else if (!clampGarbage.Contains(clamp)) clampGarbage.Add(clamp);
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
            if (clamps.Count > 0)
            {
                if (allActive)
                {
                    foreach (NeuronClamp clamp in clamps)
                    {
                        if (clamp != null && clamp.focusVert != -1) clamp.DeactivateClamp();
                        else if (!clampGarbage.Contains(clamp)) clampGarbage.Add(clamp);
                    }
                }
                else
                {
                    foreach (NeuronClamp clamp in clamps)
                    {
                        if (clamp != null && clamp.focusVert != -1) clamp.ActivateClamp();
                        else if (!clampGarbage.Contains(clamp)) clampGarbage.Add(clamp);
                    }
                }
                allActive = !allActive;
            }
        }
        private void DestroyAll()
        {
            if (clamps.Count > 0)
            {
                foreach (NeuronClamp clamp in clamps)
                {
                    if (clamp != null && clamp.focusVert != -1)
                        Destroy(clamp.transform.parent.gameObject);

                    if (!clampGarbage.Contains(clamp)) clampGarbage.Add(clamp);
                }
                clamps.Clear();
            }
        }

        private IEnumerator CheckForDuplicates(float waitTime)
        {
            while (true)
            {
                if (clamps.Count > 0)
                {
                    List<int> takenVerts = new List<int>();
                    
                    foreach (NeuronClamp clamp in clamps)
                    {
                        if (takenVerts.Contains(clamp.focusVert))
                        {
                            clampGarbage.Add(clamp);
                            Destroy(clamp.transform.parent);
                        }
                    }
                }
                yield return new WaitForSeconds(waitTime);
            }
        }

        // TODO: This is is not tested
        private IEnumerator ClampGC()
        {
            int maxGarbage = 10;
            // How many clamps need to exist before we check for garbage?
            while (true)
            {
                if (clampGarbage.Count >= maxGarbage)
                {
                    int garbageCount = clampGarbage.Count;
                    // Take out the trash
                    foreach(NeuronClamp clamp in clampGarbage)
                    {
                        clamps.Remove(clamp);
                    }
                    clampGarbage.Clear();
                    Debug.Log("Removed " + garbageCount + " null clamps.");
                }
                else
                {
                    // Wait until there w=are enough clamps in the trash;
                    yield return new WaitUntil(() => clampGarbage.Count >= maxGarbage);
                }
            }
        }
    }
}