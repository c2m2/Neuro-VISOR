using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Visualization
{
    public class NDGraphManager : MonoBehaviour
    {
        public NDSimulation sim = null;
        public GameObject graphPrefab = null;
        public RaycastPressEvents hitEvent = null;
        public List<NDLineGraph> graphs = new List<NDLineGraph>();

        public void InstantiatePanel(RaycastHit hit)
        {
            // Find nearest 1D vertex to hit point
            int nearestVert = GetNearestPoint(sim, hit);

            // Instantiate new NDGraph

            // Attach 1D value to graph
        }

        /// <summary>
        /// Looks for NDSimulation instance and adds neuronClamp object if possible
        /// </summary>
        /// <param name="hit"></param>
        public void InstantiateGraph(RaycastHit hit)
        {
            // Make sure we have a valid prefab and simulation
            if (graphPrefab == null) Debug.LogError("No Clamp prefab found");

            var sim = hit.collider.GetComponentInParent<NDSimulation>();
            // If there is no NDSimulation, don't try instantiating a clamp
            if (sim == null) return;

            var graphObj = Instantiate(graphPrefab, sim.transform);
            NDLineGraph graph = graphObj.GetComponentInChildren<NDLineGraph>();
            AttachToSimulation(graph, sim, hit);
        }
        /// <summary>
        /// Attempt to latch a clamp onto a given simulation
        /// </summary>
        public void AttachToSimulation(NDLineGraph graph, NDSimulation simulation, RaycastHit hit)
        {
            graph.sim = sim;

            graph.transform.parent.parent = simulation.transform;

            int vertIndex = GetNearestPoint(sim, hit);

            // Check for duplicates
            if (!VertIsAvailable(vertIndex, sim))
            {
                Destroy(graph.gameObject);
                return;
            }

            graph.vertex = vertIndex;

            graphs.Add(graph);       

        }

        private int GetNearestPoint(NDSimulation simulation, RaycastHit hit)
        {
            // Translate contact point to local space
            MeshFilter mf = simulation.transform.GetComponentInParent<MeshFilter>();
            if (mf == null) return -1;

            // Get 3D mesh vertices from hit triangle
            int triInd = hit.triangleIndex * 3;
            int v1 = mf.mesh.triangles[triInd];
            int v2 = mf.mesh.triangles[triInd + 1];
            int v3 = mf.mesh.triangles[triInd + 2];

            // Find 1D verts belonging to these 3D verts
            int[] verts1D = new int[]
            {
                simulation.Map[v1].v1, simulation.Map[v1].v2,
                simulation.Map[v2].v1, simulation.Map[v2].v2,
                simulation.Map[v3].v1, simulation.Map[v3].v2
            };
            Vector3 localHitPoint = simulation.transform.InverseTransformPoint(hit.point);

            float nearestDist = float.PositiveInfinity;
            int nearestVert1D = -1;
            foreach (int vert in verts1D)
            {
                float dist = Vector3.Distance(localHitPoint, simulation.Verts1D[vert]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestVert1D = vert;
                }
            }

            return nearestVert1D;
        }

        /// <returns> True if the 1D index is available, otherwise returns false</returns>
        private bool VertIsAvailable(int clampIndex, NDSimulation simulation)
        {
            bool validLocation = true;

            foreach(NDLineGraph graph in graphs)
            {
                if(graph.vertex == clampIndex)
                {
                    Debug.LogWarning("Clamp already exists on vertex [" + clampIndex + "].");
                    validLocation = false;
                }
            }

            return validLocation;
        }
    }
}