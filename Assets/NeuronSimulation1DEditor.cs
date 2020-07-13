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

        private readonly char slash = Path.DirectorySeparatorChar;
        private readonly string activeCellFolder = "ActiveCell";
        private readonly string neuronCellFolder = "NeuronalDynamics";
        private readonly string ugxExt = ".ugx";
        private readonly string spec1D = "_1d";
        private readonly string specTris = "_tris";
        private string basePath = "NULL";
        private string[] cellFileNames;

        private int _cellIndex = 0;
        private int prevIndex = -1;
        private NeuronSimulation1D.MeshColScaling prevScale = NeuronSimulation1D.MeshColScaling.x1;
        private NeuronSimulation1D.RefinementLevel prevRef = NeuronSimulation1D.RefinementLevel.x1;

        private void Awake()
        {
            basePath = Application.streamingAssetsPath + slash + neuronCellFolder + slash + activeCellFolder + slash;
        }

        /*static void Apply(string directory)
        {
            Texture2D texture = Selection.activeObject as Texture2D;
            if (texture == null)
            {
                EditorUtility.DisplayDialog("Select Texture", "You must select a texture first!", "OK");
                return;
            }


            if (path.Length != 0)
            {
                var fileContent = File.ReadAllBytes(path);
                texture.LoadImage(fileContent);
            }
        }*/

        float thumbnailWidth = 70;
        float thumbnailHeight = 70;
        float labelWidth = 150f;
        string lastPath = "";

        string path1x = "NULL";
        string path2x = "NULL";
        string path3x = "NULL";
        string path4x = "NULL";
        string path5x = "NULL";

        public override void OnInspectorGUI()
        {
            string cellPath = "";
            string cellColPath = "";

            var neuronSimulation = target as NeuronSimulation1D;
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

            string[] BuildCellOptions(string basePath)
            {
                // Separate cell option names from full paths
                string[] allPaths = Directory.GetDirectories(basePath);
                string[] allPathEnds = new string[allPaths.Length];
                for (int i = 0; i < allPaths.Length; i++)
                {
                    int pos = allPaths[i].LastIndexOf(slash) + 1;
                    allPathEnds[i] = allPaths[i].Substring(pos, allPaths[i].Length - pos);
                }
                return allPathEnds;
            }

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