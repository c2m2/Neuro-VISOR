using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class ClampSimpleInstantiator : MonoBehaviour
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
                        if(clamp != null && clamp.nearestVert != -1)
                        {
                            inds.Add(clamp.nearestVert);
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
            var sim = hit.collider.GetComponentInParent<NeuronSimulation1D>();
            if (sim == null) return;

            var clamp = Instantiate(clampPrefab, sim.transform);
            clamp.transform.position = hit.point;
            clamps.Add(clamp.GetComponentInChildren<NeuronClamp>());
        }

        public void ToggleClamps(RaycastHit hit)
        {
            if (clamps.Count > 0)
            {
                string s = "";
                if (allActive)
                {
                    foreach (NeuronClamp clamp in clamps)
                    {
                        if (clamp != null && clamp.nearestVert != -1) clamp.DeactivateClamp();
                        else clampGarbage.Add(clamp);
                    }
                    s += "Deactivated ";
                }
                else
                {
                    foreach (NeuronClamp clamp in clamps)
                    {
                        if (clamp != null && clamp.nearestVert != -1) clamp.ActivateClamp();
                        else clampGarbage.Add(clamp);
                    }
                    s += "Activated ";
                }
                Debug.Log(s + (clamps.Count - clampGarbage.Count) + " clamps.\n" + clampGarbage.Count + " null clamps found.");

                allActive = !allActive;
            }
        }

        public int holdCount = 50;
        public int curCount = 0;
        public void MonitorDestroy(RaycastHit hit)
        {
            curCount++;
            if(curCount == holdCount)
            {
                foreach(NeuronClamp clamp in clamps)
                {
                    if (clamp != null && clamp.nearestVert != -1)
                    {
                        Destroy(clamp.transform.parent.gameObject);
                    }
                    clampGarbage.Add(clamp);
                }
                clamps.Clear();
            }
        }
        public void EndDestroyMonitor(RaycastHit hit)
        {
            curCount = 0;
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
                        if (takenVerts.Contains(clamp.nearestVert))
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