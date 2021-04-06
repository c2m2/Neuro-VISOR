using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using TMPro;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDInfoDisplay : MonoBehaviour
    {
        public NDSimulationController simController = null;
        public NDSimulation Sim
        {
            get
            {
                return simController.sim;
            }
        }

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
            NullChecks();

            void NullChecks()
            {
                bool fatal = cellName == null || vert1DTxt == null || vert3DTxt == null || triTxt == null || refinement == null;

                if (cellName == null) { Debug.LogError("No cell name TMPro."); }
                if (vert1DTxt == null) Debug.LogError("No vert1D TMPro.");
                if (vert3DTxt == null) Debug.LogError("No vert3D TMPro.");
                if (triTxt == null) Debug.LogError("No triangle TMPro.");
                if (refinement == null) Debug.LogError("No refinement TMPro.");
                if (simController == null)
                {
                    simController = GetComponentInParent<NDSimulationController>();
                    if (simController == null)
                    {
                        Debug.LogError("No sim controller given.");
                        fatal = true;
                    }
                }
                if (fatal)
                {
                    Destroy(this);
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            string name = Sim.vrnFileName;
            if (name.EndsWith(".vrn")) name = name.Substring(0, name.LastIndexOf(".vrn"));
            cellName.text = "Cell: " + name;

            refinement.text = "Refinement: " + Sim.RefinementLevel;
            vert1DTxt.text = "1D Verts: " + Sim.Grid1D.Mesh.vertexCount.ToString() 
                + ", Edges: " + Sim.Grid1D.Edges.Count;
            vert3DTxt.text = "3D Verts: " + Sim.Grid2D.Mesh.vertexCount.ToString()
                + ", Edges: " + Sim.Grid2D.Edges.Count;
            triTxt.text = "Triangles: " + Sim.Grid2D.Mesh.triangles.Length.ToString();
        }

    }
}