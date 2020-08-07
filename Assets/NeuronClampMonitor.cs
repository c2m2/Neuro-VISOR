using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.NeuronalDynamics.Interaction {
    [RequireComponent(typeof(Rigidbody))]
    public class NeuronClampMonitor : MonoBehaviour
    {
        public NeuronClamp clamp = null;
        public NeuronSimulation1D curSim = null;
        private Rigidbody rb = null;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            if(clamp == null)
            {
                clamp = GetComponentInParent<NeuronClamp>();
                if(clamp == null)
                {
                    Debug.LogError("No NeuronClamp found for NeuronClampMonitor.");
                    Destroy(this);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (curSim == null)
            {
                NeuronSimulation1D simulation = other.GetComponentInParent<NeuronSimulation1D>() ?? other.GetComponent<NeuronSimulation1D>();
                if (simulation != null)
                {
                    curSim = clamp.ReportSimulation(simulation, transform.position);

                }
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (curSim != null)
            {
                if (curSim == other.GetComponentInParent<NeuronSimulation1D>() || curSim == other.GetComponent<NeuronSimulation1D>())
                {
                    clamp.ReportExit(curSim);
                    curSim = null;
                }
            }
        }
    }
}
