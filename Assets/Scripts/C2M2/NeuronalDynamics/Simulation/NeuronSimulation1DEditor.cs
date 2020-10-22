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
        /*
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
                DrawTextField(ref neuronSimulation.vrnPath, "VRN Cell Path");
            }
            else
            {
                DrawTextArea(ref neuronSimulation.vrnPath, "VRN Cell Path");
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
        */
    }
}