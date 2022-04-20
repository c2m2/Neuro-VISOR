using UnityEngine;
using C2M2.Visualization;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    [RequireComponent(typeof(NDGraph))]
    public class NDLineGraph : LineGrapher
    {

        private NDGraph ndgraph;

        public NDSimulation Sim
        {
            get
            {
                return ndgraph.simulation;
            } 
        }

        // Worldspace position of the vertex
        public Vector3 VertPos { get { return Sim.transform.TransformPoint(Sim.Verts1D[ndgraph.FocusVert]); } }
        // World space size of the graph
        private Vector3 GraphSize { get { return ((RectTransform)transform).sizeDelta * transform.localScale; } }

        private void Awake()
        {
            ndgraph = GetComponent<NDGraph>();
        }

        // Start is called before the first frame update
        public void SetUp()
        {
            SetLabels();

            transform.position = GetPanelPos();

            // Rotate panel towards camera in y direction
            transform.LookAt(Camera.main.transform);
            transform.localRotation = Quaternion.Euler(new Vector3(0f, rt.localRotation.eulerAngles.y - 180, 0f));

            InitPointerLines();

            //UpdateSize();
            MaxSamples = 500;

            void SetLabels()
            {
                string title = "Voltage vs. Time (Vert " + ndgraph.FocusVert + ")";
                string xLabel = "Time (ms)";
                string yLabel = "Voltage (" + Sim.unit + ")";

                base.SetLabels(title, xLabel, yLabel);
            }

            Vector3 GetPanelPos()
            {
                Vector3 cameraPos = Camera.main.transform.position;
                // Vector pointing from camera to cell
                Vector3 direction = VertPos - cameraPos;

                // Worldspace size of the graph
                float newMagnitude = GraphSize.x / 2;
                float oldMagnitude = direction.magnitude;
                float magScale = newMagnitude / oldMagnitude;

                // The panel is placed along a vector pointing from camera to vertex position'
                return new Vector3(VertPos.x + direction.x * magScale, VertPos.y, VertPos.z + direction.z * magScale);
            }

            void InitPointerLines()
            {
                // Point anchor lines to vertex
                if (pointerLines != null)
                {
                    pointerLines.UseWorldSpace = true;
                    pointerLines.onlyRenderShortestAnchor = true;
                }
                else Debug.LogWarning("Couldn't access pointer lines, feature may be disabled.");
            }
        }

        public override void AddValue(float x, float y)
        {
            YMin = Sim.ColorLUT.GlobalMin * Sim.unitScaler;
            YMax = Sim.ColorLUT.GlobalMax * Sim.unitScaler;

            // Add point to graph
            base.AddValue(x, y);
        }

        private void Update()
        {
            if (pointerLines != null)
            {
                pointerLines.targetPos = VertPos;
            }
        }
        
        public void UpdateSize()
        {
            // Reset graph to match original worldspace size
            transform.localScale = new Vector3(transform.localScale.x / Sim.transform.localScale.x,
                transform.localScale.y / Sim.transform.localScale.y,
                transform.localScale.z / Sim.transform.localScale.z);
        }

    }
}
