using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Visualization;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    [RequireComponent(typeof(LineGrapher))]
    public class NDLineGraph : MonoBehaviour
    {
        public NDGraphManager manager = null;
        public NDSimulation Sim { get { return manager.sim; } }
        public int vert = -1;
        public Vector3 focusPos { get; private set; }

        private LineGrapher lineGraph;
        private RectTransform rt = null;

        private void Awake()
        {
            lineGraph = GetComponent<LineGrapher>();

            // Get width and height of the graph
            rt = (RectTransform)lineGraph.transform;
        }

        // Start is called before the first frame update
        void Start()
        {
            if(vert == -1)
            {
                Debug.LogError("Invalid vertex given to NDLineGraph");
                Destroy(this);
            }

            string title = "Voltage vs. Time (Vert " + vert + ")";
            string xLabel = "Time (ms)";
            string yLabel = "Voltage (" + Sim.unit + ")";

            lineGraph.SetLabels(title, xLabel, yLabel);

            transform.SetParent(Sim.transform);

            focusPos = Sim.Verts1D[vert];

            // worldspace position of vertex
            Vector3 cellPos = new Vector3(focusPos.x * Sim.transform.localScale.x,
                focusPos.y * Sim.transform.localScale.y,
                focusPos.z * Sim.transform.localScale.z);
            cellPos += Sim.transform.position;
            Vector3 cameraPos = Camera.main.transform.position;
            // Vector pointing from camera to cell
            Vector3 direction = (cellPos - cameraPos);
            
            float xSign = (cameraPos.x > cellPos.x) ? -1 : 1;
            float zSign = (cameraPos.z > cellPos.z) ? -1 : 1;

            Vector3 lwh = rt.sizeDelta;

            Vector3 graphScaleW = new Vector3(lwh.x * rt.localScale.x, lwh.y * rt.localScale.y, lwh.z * rt.localScale.z);

            // Worldspace positional shift for panel
            //rt.localPosition = new Vector3(focusPos.x + (xSign * posShift.x), focusPos.y - posShift.y, focusPos.z + (zSign * posShift.z));
            rt.position = new Vector3(cameraPos.x + (direction.x + xSign * (graphScaleW.x /2)), 
                cellPos.y - (graphScaleW.y / 2),
                cameraPos.z + (direction.z + zSign * (graphScaleW.y / 2)));

            // Only keep y rotation
            rt.LookAt(Camera.main.transform);
            rt.localRotation = Quaternion.Euler(new Vector3(0f, rt.localRotation.eulerAngles.y, 0f));

            // Point anchor lines to vertex
            if(lineGraph.pointerLines != null)
            {
                lineGraph.pointerLines.UseWorldSpace = true;
                lineGraph.pointerLines.onlyRenderShortestAnchor = true;
            }
            else Debug.LogWarning("Couldn't access pointer lines, feature may be disabled.");

            // Reset graph to match original worldspace size
            transform.localScale = new Vector3(transform.localScale.x / Sim.transform.localScale.x,
                transform.localScale.y / Sim.transform.localScale.y,
                transform.localScale.z / Sim.transform.localScale.z);

            lineGraph.MaxSamples = 300;
        }

        public void AddValue(float x, float y)
        {
            lineGraph.YMin = Sim.globalMin * Sim.unitScaler;
            lineGraph.YMax = Sim.globalMax * Sim.unitScaler;

            // Add point to graph
            lineGraph.AddValue(x, y);
        }

        private Vector3 SimLocalScale
        {
            get
            {
                return Sim.transform.localScale;
            }
        }
        private void Update()
        {
            if (lineGraph.pointerLines != null)
            {
                Vector3 pointTo = new Vector3((focusPos.x * SimLocalScale.x) + Sim.transform.position.x, 
                    (focusPos.y * SimLocalScale.y) + Sim.transform.position.y, 
                    (focusPos.z * SimLocalScale.z) + Sim.transform.position.z);

                lineGraph.pointerLines.targetPos = pointTo;
            }
        }

        private void OnDestroy()
        {
            manager.graphs.Remove(this);
        }    
    }
}
