using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;
using System.IO;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDGraphManager : MonoBehaviour
    {
        public NDSimulation sim = null;
        public GameObject graphPrefab { get; private set; } = null;
        public RaycastPressEvents hitEvent { get; private set; } = null;
        public List<NDLineGraph> graphs = new List<NDLineGraph>();

        private void Awake()
        {
            graphPrefab = Resources.Load("Prefabs" + Path.DirectorySeparatorChar + "NeuronalDynamics" + Path.DirectorySeparatorChar + "NDLineGraph") as GameObject;
            if (graphPrefab == null)
            {
                Debug.LogError("No graph prefab found.");
                Destroy(this);
            }

            hitEvent = gameObject.AddComponent<RaycastPressEvents>();
            hitEvent.OnPress.AddListener((hit) => InstantiateGraph(hit));
        }

        /// <summary>
        /// Looks for NDSimulation instance and adds neuronClamp object if possible
        /// </summary>
        /// <param name="hit"></param>
        public void InstantiateGraph(RaycastHit hit)
        {
            // Make sure we have a valid prefab and simulation
            if (graphPrefab == null) Debug.LogError("No Clamp prefab found");

            NDSimulation sim = hit.collider.GetComponentInParent<NDSimulation>();
            // If there is no NDSimulation, don't try instantiating a clamp
            if (sim == null) return;

            var graphObj = Instantiate(graphPrefab, sim.transform);
            NDLineGraph graph = graphObj.GetComponent<NDLineGraph>();
            graph.manager = this;
            AttachToSimulation(graph, sim, hit);

        }
        /// <summary>
        /// Attempt to latch a graph onto a given simulation
        /// </summary>
        public void AttachToSimulation(NDLineGraph graph, NDSimulation simulation, RaycastHit hit)
        {
            int vertIndex = simulation.GetNearestPoint(hit);

            // Check for duplicates
            if (!VertIsAvailable(vertIndex, simulation))
            {
                Destroy(graph.gameObject);
                return;
            }

            graph.vert = vertIndex;

            graphs.Add(graph);
        }

        /// <returns> True if the 1D index is available, otherwise returns false</returns>
        private bool VertIsAvailable(int clampIndex, NDSimulation simulation)
        {
            bool validLocation = true;

            foreach(NDLineGraph graph in graphs)
            {
                if(graph.vert == clampIndex)
                {
                    Debug.LogWarning("Clamp already exists on vertex [" + clampIndex + "].");
                    validLocation = false;
                }
            }

            return validLocation;
        }
    }
}