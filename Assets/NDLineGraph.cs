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

        private void Awake()
        {
            lineGraph = GetComponent<LineGrapher>();
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
            //transform.parent = sim.transform;

            focusPos = sim.Verts1D[vert];
            transform.localPosition = focusPos;

            // Reset graph to match original worldspace size
            transform.localScale = new Vector3(transform.localScale.x / sim.transform.localScale.x,
                transform.localScale.y / sim.transform.localScale.y,
                transform.localScale.z / sim.transform.localScale.z);

            lineGraph.NumSamples = 1000;
        }

        private void FixedUpdate()
        {
            // Get value at vertex ID
            double val = sim.Get1DValue(vert);

            // Get time value from simulation
            float time = sim.GetSimulationTime();

            lineGraph.YMin = sim.globalMin;
            lineGraph.YMax = sim.globalMax;
            
            // Add point to graph
            lineGraph.AddValue(time, (float)val);
        }

        private void OnDestroy()
        {
            manager.graphs.Remove(this);
        }    
    }
}
