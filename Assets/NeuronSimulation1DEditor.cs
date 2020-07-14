using System.IO;
using UnityEngine;
using UnityEditor;

namespace C2M2.NeuronalDynamics.Simulation
{
    /// <summary>
    /// Provides editor features so that users can more easily select neuron cells and options for them
    /// </summary>
    [CustomEditor(typeof(NeuronSimulation1D), true)]
    public class NeuronSimulation1DEditor : Editor
    {
        private int _cellIndex = 0;
        private int prevIndex = -1;
        private NeuronSimulation1D.MeshScaling prevScale = NeuronSimulation1D.MeshScaling.x1;
        private NeuronSimulation1D.RefinementLevel prevRef = NeuronSimulation1D.RefinementLevel.x1;

        private string lastPath = "";
        NeuronSimulation1D neuronSimulation;

        public void Awake()
        {
            neuronSimulation = target as NeuronSimulation1D;
        }

        public override void OnInspectorGUI()
        {
            if (!Application.isPlaying)
            { 
                DrawTextField(ref neuronSimulation.cell1xPath, "Cell Path Diameter 1x");
                DrawTextField(ref neuronSimulation.cell2xPath, "Cell Path Diameter 2x");
                DrawTextField(ref neuronSimulation.cell3xPath, "Cell Path Diameter 3x");
                DrawTextField(ref neuronSimulation.cell4xPath, "Cell Path Diameter 4x");
                DrawTextField(ref neuronSimulation.cell5xPath, "Cell Path Diameter 5x");
            }
            else
            {
                DrawTextArea(ref neuronSimulation.cell1xPath, "Cell Path Diameter 1x");
                DrawTextArea(ref neuronSimulation.cell2xPath, "Cell Path Diameter 2x");
                DrawTextArea(ref neuronSimulation.cell3xPath, "Cell Path Diameter 3x");
                DrawTextArea(ref neuronSimulation.cell4xPath, "Cell Path Diameter 4x");
                DrawTextArea(ref neuronSimulation.cell5xPath, "Cell Path Diameter 5x");
            }

            // Draw the default inspector
            DrawDefaultInspector();

            return;

            string DrawTextField(ref string target, string name = "")
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(name))
                {
                    target = EditorUtility.OpenFolderPanel("Cell Path", lastPath, "");
                }
                target = GUILayout.TextField(target);
                GUILayout.EndHorizontal();

                lastPath = target;

                return target;
            }

            string DrawTextArea(ref string target, string name = "")
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(name);
                GUILayout.TextArea(target);
                GUILayout.EndHorizontal();
                return target;
            }

        }
    }
}