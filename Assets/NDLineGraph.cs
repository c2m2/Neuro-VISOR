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
        public Vector3 vertPos { get; private set; }

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

            SetLabels();

            transform.SetParent(Sim.transform);

            rt.position = GetPanelPos();

            // Rotate panel towards camera in y direction
            rt.LookAt(Camera.main.transform);
            rt.localRotation = Quaternion.Euler(new Vector3(0f, rt.localRotation.eulerAngles.y - 180, 0f));

            InitPointerLines();

            // Reset graph to match original worldspace size
            transform.localScale = new Vector3(transform.localScale.x / Sim.transform.localScale.x,
                transform.localScale.y / Sim.transform.localScale.y,
                transform.localScale.z / Sim.transform.localScale.z);

            lineGraph.MaxSamples = 300;

            void SetLabels()
            {
                string title = "Voltage vs. Time (Vert " + vert + ")";
                string xLabel = "Time (ms)";
                string yLabel = "Voltage (" + Sim.unit + ")";

                lineGraph.SetLabels(title, xLabel, yLabel);
            }

            Vector3 GetPanelPos()
            {
                // Convert vertex position to world space
                vertPos = Sim.Verts1D[vert];
                vertPos = new Vector3(vertPos.x * Sim.transform.localScale.x,
                    vertPos.y * Sim.transform.localScale.y,
                    vertPos.z * Sim.transform.localScale.z);

                Vector3 vertPosShift = vertPos + Sim.transform.position;
                Vector3 cameraPos = Camera.main.transform.position;
                // Vector pointing from camera to cell
                Vector3 direction = (vertPosShift - cameraPos);

                // Worldspace size of the graph
                float graphSize = rt.sizeDelta.x * rt.localScale.x;
                float newMagnitude = graphSize;
                float oldMagnitude = direction.magnitude;
                float magScale = newMagnitude / oldMagnitude;

                // The panel is placed along a vector pointing from camera to vertex position
                return new Vector3(direction.x * magScale, vertPosShift.y - (graphSize / 2), direction.z * magScale) + vertPos;
            }

            void InitPointerLines()
            {
                // Point anchor lines to vertex
                if (lineGraph.pointerLines != null)
                {
                    lineGraph.pointerLines.UseWorldSpace = true;
                    lineGraph.pointerLines.onlyRenderShortestAnchor = true;
                }
                else Debug.LogWarning("Couldn't access pointer lines, feature may be disabled.");
            }
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
                Vector3 pointTo = new Vector3(vertPos.x + Sim.transform.position.x, 
                    vertPos.y + Sim.transform.position.y, 
                    vertPos.z + Sim.transform.position.z);

                lineGraph.pointerLines.targetPos = pointTo;
            }
        }

        private void OnDestroy()
        {
            manager.graphs.Remove(this);
        }    
    }
}
