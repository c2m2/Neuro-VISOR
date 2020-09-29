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

        public void InstantiateClamp(RaycastHit hit)
        {
            if (clampPrefab == null) Debug.LogError("No Clamp prefab found");
            var sim = hit.collider.GetComponentInParent<NeuronSimulation1D>();
            if (sim == null) return;

            var clamp = Instantiate(clampPrefab, sim.transform);
            clamp.transform.position = hit.point;
            clamps.Add(clampPrefab.GetComponent<NeuronClamp>());
        }

        public void ToggleClamps(RaycastHit hit)
        {
            if (allActive)
            {
                foreach(NeuronClamp clamp in clamps)
                {
                    clamp.DeactivateClamp();
                }
            }
            else
            {
                foreach (NeuronClamp clamp in clamps)
                {
                    clamp.ActivateClamp();
                }
            }
            allActive = !allActive;
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
                    Destroy(clamp.transform.parent.gameObject);
                }
            }
        }
        public void EndDestroyMonitor(RaycastHit hit)
        {
            curCount = 0;
        }
    }
}