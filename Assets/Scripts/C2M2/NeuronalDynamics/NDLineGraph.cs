using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Visualization;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDLineGraph : LineGrapher
    {
        public NDGraphManager manager = null;
        public NDSimulation Sim { get { return manager.sim; } }
        public int vert = -1;

        /// <summary>
        /// If true, this object will scale with parent object as per usual.
        /// If false, this object will maintain worldspace size as parent scales
        /// </summary>
        public bool obeyParentScale = false;

        // Worldspace position of the vertex
        public Vector3 VertPos { get { return Sim.transform.TransformPoint(Sim.Verts1D[vert]); } }
        private new RectTransform rt = null;
        // World space size of the graph
        private Vector3 GraphSize { get { return rt.sizeDelta * rt.localScale; } }

        private void Awake()
        {
            // Get width and height of the graph
            rt = (RectTransform)transform;
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

            rt.position = GetPanelPos();

            // Rotate panel towards camera in y direction
            rt.LookAt(Camera.main.transform);
            rt.localRotation = Quaternion.Euler(new Vector3(0f, rt.localRotation.eulerAngles.y - 180, 0f));

            InitPointerLines();

            //UpdateSize();
            MaxSamples = 500;

            name = "LineGraph(" + Sim.name + ")[vert" + vert + "]";

            void SetLabels()
            {
                string title = "Voltage vs. Time (Vert " + vert + ")";
                string xLabel = "Time (ms)";
                string yLabel = "Voltage (" + Sim.unit + ")";

                base.SetLabels(title, xLabel, yLabel);
            }

            Vector3 GetPanelPos()
            {
                Debug.Log("Vert position: " + VertPos);
                Vector3 cameraPos = Camera.main.transform.position;
                // Vector pointing from camera to cell
                Vector3 direction = (VertPos - cameraPos);

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
            YMin = Sim.globalMin * Sim.unitScaler;
            YMax = Sim.globalMax * Sim.unitScaler;

            // Add point to graph
            base.AddValue(x, y);
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
            if (pointerLines != null)
            {
                pointerLines.targetPos = VertPos;
            }
            if(Sim == null || manager == null)
            {
                DestroyPlot();
            }
        }

        private void OnDestroy()
        {
            manager.graphs.Remove(this);
        }   
        
        private void UpdateSize()
        {
            // Reset graph to match original worldspace size
            transform.localScale = new Vector3(transform.localScale.x / Sim.transform.localScale.x,
                transform.localScale.y / Sim.transform.localScale.y,
                transform.localScale.z / Sim.transform.localScale.z);
        }
    }
}
