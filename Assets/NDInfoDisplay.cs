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

        public TextMeshProUGUI text = null;
        public TextMeshProUGUI cellName = null;

        public int fontSize = 24;

        // Provides a way to access all text components at once
        private TextMeshProUGUI[] infoTexts
        {
            get
            {
                return new TextMeshProUGUI[] { cellName };
            }
        }

        // Refinement level
        // Simulation time
        // Simulation parameters:
        //      Timestep size
        //      endTime
        //      Biological parameters

        private void Awake()
        {
            NullChecks();

            foreach(TextMeshProUGUI text in infoTexts)
            {
                text.fontSize = fontSize;
            }

            void NullChecks()
            {
                bool fatal = false;
                foreach(TextMeshProUGUI text in infoTexts)
                {
                    if (text == null) fatal = true;
                }

                if (cellName == null) { Debug.LogError("No cell name TMPro."); }

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

            text.text = "Cell: " + name
                + "\nRefinement: " + Sim.RefinementLevel
                + "\n1D V: " + Sim.Grid1D.Mesh.vertexCount.ToString()
                + ", E: " + Sim.Grid1D.Edges.Count
                + "\n3D V: " + Sim.Grid2D.Mesh.vertexCount.ToString()
                + ", E: " + Sim.Grid2D.Edges.Count
                + "\nTris: " + Sim.Grid2D.Mesh.triangles.Length.ToString();
        }

    }
}