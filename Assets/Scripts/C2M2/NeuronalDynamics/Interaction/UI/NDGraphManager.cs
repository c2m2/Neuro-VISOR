using UnityEngine;
using System.IO;
using C2M2.NeuronalDynamics.Simulation;
using System.Collections.Generic;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDGraphManager : NDInteractablesManager<NDGraph>
    {
        public GameObject graphPrefab = null;
        public List<NDGraph> graphs = new List<NDGraph>();

        public override GameObject IdentifyBuildPrefab(NDSimulation sim, int index)
        {
            if (graphPrefab == null)
            {
                Debug.LogError("No Graph prefab found");
                return null;
            }
            else return graphPrefab;
        }

        private void OnDestroy()
        {
            foreach(NDGraph graph in graphs)
            {
                graph.ndlinegraph.DestroyPlot();
            }
        }

        override public bool VertexAvailable(NDSimulation sim, int index)
        {
            // minimum distance between graphs 
            float distanceBetweenGraphs = sim.AverageDendriteRadius * 2;

            foreach (NDGraph graph in graphs)
            {
                if (graph.simulation == sim)
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
                        float dist = (sim.Verts1D[graph.FocusVert] - sim.Verts1D[index]).magnitude;
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