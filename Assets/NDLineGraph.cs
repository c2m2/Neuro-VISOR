using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Visualization;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Visualization
{
    [RequireComponent(typeof(LineGrapher))]
    public class NDLineGraph : MonoBehaviour
    {
        public NDSimulation sim = null;
        public int vert = -1;
        public Vector3 focusPos { get; private set; }
        public NDGraphManager manager = null;

        private LineGrapher lineGraph;
        private Coroutine addValueCoroutine;
        private RectTransform rt = null;
        private Vector3 SimLocalScale
        {
            get
            {
                return sim.transform.localScale;
            }
        }

        private void Awake()
        {
            lineGraph = GetComponent<LineGrapher>();

            // Get width and height of the graph
            rt = (RectTransform)lineGraph.transform;
        }
        // Start is called before the first frame update
        void Start()
        {
            if(sim == null)
            {
                Debug.LogError("No simulation given to NDLineGraph.");
                Destroy(this);
            }

            if(vert == -1)
            {
                Debug.LogError("Invalid vertex given to NDLineGraph");
                Destroy(this);
            }

            string title = "Voltage vs. Time (Vert " + vert + ")";
            string xLabel = "Time (ms)";
            string yLabel = "Voltage (" + sim.unit + ")";

            lineGraph.SetLabels(title, xLabel, yLabel);

            transform.SetParent(sim.transform);

            focusPos = sim.Verts1D[vert];

            Vector3 lwh = rt.sizeDelta;

            // Gets an adjusted local scale for the graph, assuming it is parented under the simulation
            Vector3 graphScale = new Vector3(
                    rt.localScale.x / sim.transform.localScale.x,
                    rt.localScale.y / sim.transform.localScale.y,
                    rt.localScale.z / sim.transform.localScale.z);

            // We shift the graph so that it is centered on the 1D vertex and behind the geometry by a a graph-width
            Vector3 posShift = new Vector3(
                lwh.x / 2 * graphScale.x,
                lwh.y / 2 * graphScale.y,
                lwh.x * graphScale.z);

 
            rt.localPosition = new Vector3(focusPos.x - posShift.x, focusPos.y - posShift.y, focusPos.z + posShift.z);
            // Worldspace positional shift for panel
            // Vector3 posShift = ne
            // Point anchor lines to vertex
            if(lineGraph.pointerLines != null)
            {
                lineGraph.pointerLines.UseWorldSpace = true;
                lineGraph.pointerLines.onlyRenderShortestAnchor = true;
            }
            else
            {
                Debug.LogWarning("Couldn't access pointer lines, feature may be disabled.");
            }

            // Reset graph to match original worldspace size
            transform.localScale = new Vector3(transform.localScale.x / sim.transform.localScale.x,
                transform.localScale.y / sim.transform.localScale.y,
                transform.localScale.z / sim.transform.localScale.z);

            lineGraph.MaxSamples = 300;
        }

        private void FixedUpdate()
        {
            // Get value at vertex ID
            double val = sim.curVals1D[vert];

            // Get time value from simulation
            float time = sim.GetSimulationTime();

            lineGraph.YMin = sim.globalMin;
            lineGraph.YMax = sim.globalMax;
            
            // Add point to graph
            lineGraph.AddValue(time, (float)val);
        }

        private void Update()
        {
            if (lineGraph.pointerLines != null)
            {
                Vector3 pointTo = new Vector3(focusPos.x * SimLocalScale.x, 
                    focusPos.y * SimLocalScale.y, 
                    focusPos.z * SimLocalScale.z);

                lineGraph.pointerLines.targetPos = pointTo;
            }
        }

        private void OnDestroy()
        {
            manager.graphs.Remove(this);
        }    
    }
}
