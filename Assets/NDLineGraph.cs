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
        public int vertex = -1;
        public Vector3 focusPos { get; private set; }
        public NDGraphManager manager = null;
        private LineGrapher lineGraph;
        private Coroutine addValueCoroutine;
    
        // Start is called before the first frame update
        void Start()
        {
            if(sim == null)
            {
                Debug.LogError("No simulation given to NDLineGraph.");
                Destroy(this);
            }

            if(vertex == -1)
            {
                Debug.LogError("Invalid vertex given to NDLineGraph");
                Destroy(this);
            }

            focusPos = sim.Verts1D[vertex];
            transform.localPosition = focusPos;
        }

        private void FixedUpdate()
        {
            // Get value at vertex ID

            // Get time value from simulation

            // Add point to graph
            //lineGraph.AddValue(sim.GetSimulationTime(), );
        }

        private void OnDestroy()
        {
            manager.graphs.Remove(this);
        }

       
    }
}
