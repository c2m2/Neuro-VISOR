using UnityEngine;
using C2M2.Interaction;
using System.IO;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDGraphManager : NDInteractablesManager<NDGraph>
    {
        public GameObject graphPrefab { get; private set; } = null;

        private void Awake()
        {
            graphPrefab = Resources.Load("Prefabs" + Path.DirectorySeparatorChar + "NeuronalDynamics" + Path.DirectorySeparatorChar + "NDLineGraph") as GameObject;
        }

        protected override void AddHitEventListeners()
        {
            //HitEvent.OnHover.AddListener((hit) => Preview(hit));
            HitEvent.OnHoverEnd.AddListener((hit) => DestroyPreview());
            HitEvent.OnPress.AddListener((hit) => InstantiateNDInteractable(hit));
        }

        public override GameObject IdentifyBuildPrefab(int index)
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

        protected override void PreviewCustom()
        {

        }
    }
}