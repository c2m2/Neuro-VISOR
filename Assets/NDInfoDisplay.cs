using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using TMPro;
namespace C2M2.Visualization
{
    public class NDInfoDisplay : MonoBehaviour
    {
        public NDSimulation sim = null;
        public TextMeshProUGUI cellName = null;
        public TextMeshProUGUI refinement = null;
        public TextMeshProUGUI vert1DTxt = null;
        public TextMeshProUGUI vert3DTxt = null;
        public TextMeshProUGUI triTxt = null;

        // Refinement level
        // Simulation time
        // Simulation parameters:
        //      Timestep size
        //      endTime
        //      Biological parameters

        private void Awake()
        {
            if(cellName == null) Debug.LogError("No cell name TMPro.");
            if (vert1DTxt == null) Debug.LogError("No vert1D TMPro.");
            if (vert3DTxt == null) Debug.LogError("No vert3D TMPro.");
            if (triTxt == null) Debug.LogError("No triangle TMPro.");
            if (refinement == null) Debug.LogError("No refinement TMPro.");
        }

        // Start is called before the first frame update
        void Start()
        {
            if (sim == null) Debug.LogError("No simulation given.");

            string name = sim.vrnFileName;
            if (name.EndsWith(".vrn")) name = name.Substring(0, name.LastIndexOf(".vrn"));
            cellName.text = "Cell: " + name;

            refinement.text = "Refinement: " + sim.RefinementLevel;
            vert1DTxt.text = "1D Verts: " + sim.Grid1D.Mesh.vertexCount.ToString() 
                + ", Edges: " + sim.Grid1D.Edges.Count;
            vert3DTxt.text = "3D Verts: " + sim.Grid2D.Mesh.vertexCount.ToString()
                + ", Edges: " + sim.Grid2D.Edges.Count;
            triTxt.text = "Triangles: " + sim.Grid2D.Mesh.triangles.Length.ToString();
        }

    }
}