using UnityEngine;
using C2M2.Interaction;
using System.IO;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDGraphManager : NDInteractablesManager<NDGraph>
    {
        public GameObject graphPrefab { get; private set; } = null;
        public RaycastPressEvents hitEvent { get; private set; } = null;

        private void Awake()
        {
            graphPrefab = Resources.Load("Prefabs" + Path.DirectorySeparatorChar + "NeuronalDynamics" + Path.DirectorySeparatorChar + "NDLineGraph") as GameObject;
            if (graphPrefab == null)
            {
                Debug.LogError("No graph prefab found.");
                Destroy(this);
            }

            hitEvent = gameObject.AddComponent<RaycastPressEvents>();
            hitEvent.OnPress.AddListener((hit) => BuildGraph(hit));
        }

        /// <summary>
        /// Looks for NDSimulation instance and adds neuronClamp object if possible
        /// </summary>
        /// <param name="hit"></param>
        public NDGraph BuildGraph(RaycastHit hit)
        {
            // Make sure we have a valid prefab and simulation
            if (graphPrefab == null) Debug.LogError("No Graph prefab found");

            int vertIndex = currentSimulation.GetNearestPoint(hit);
            if (VertexAvailable(vertIndex))
            {
                GameObject graphObj = Instantiate(graphPrefab);
                NDGraph graph = graphObj.GetComponent<NDGraph>();
                graph.AttachToSimulation(currentSimulation, vertIndex);

                interactables.Add(graph);

                return graph;
            }

            return null;
        }

        private void OnDestroy()
        {
            foreach(var graph in interactables)
            {
                graph.ndlinegraph.DestroyPlot();
            }
        }

        public override bool VertexAvailable(int index)
        {
            // minimum distance between graphs 
            float distanceBetweenGraphs = currentSimulation.AverageDendriteRadius * 2;

            foreach (NDGraph graph in interactables)
            {
                if (graph.simulation == currentSimulation)
                {
                    // If there is a graph on that 1D vertex, the spot is not open
                    if (graph.FocusVert == index)
                    {
                        Debug.LogWarning("Graph already exists on focus vert [" + index + "]");
                        return false;
                    }
                    // If there is a clamp within distanceBetweenGraphs, the spot is not open
                    else
                    {
                        float dist = (currentSimulation.Verts1D[graph.FocusVert] - currentSimulation.Verts1D[index]).magnitude;
                        if (dist < distanceBetweenGraphs)
                        {
                            Debug.LogWarning("Graph too close to graph located on vert [" + graph.FocusVert + "].");
                            return false;
                        }
                    }
                }
                
            }
            return true;
        }
    }
}