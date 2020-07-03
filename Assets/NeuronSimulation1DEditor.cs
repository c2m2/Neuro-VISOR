using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace C2M2.NeuronalDynamics.Simulation
{
    [CustomEditor(typeof(NeuronSimulation1D), true)]
    public class NeuronSimulation1DEditor : Editor
    {
        string[] _cellOptions = new[] { "NULL" };
        int _cellIndex = 0;

        private readonly char slash = Path.DirectorySeparatorChar;
        private readonly string activeCellFolder = "ActiveCell";
        private readonly string neuronCellFolder = "NeuronalDynamics";
        private readonly string ugxExt = ".ugx";
        private readonly string spec1D = "_1d";
        private readonly string specTris = "_tris";
        private string[] cellFileNames;


        public override void OnInspectorGUI()
        {
            if (!Application.isPlaying)
            {
                var neuronSimulation = target as NeuronSimulation1D;

                string basePath;
                string rendCellPath = Application.streamingAssetsPath + slash + neuronCellFolder + slash + activeCellFolder + slash;

                _cellOptions = Directory.GetDirectories(rendCellPath);

                _cellIndex = EditorGUILayout.Popup("Neuron Cell Source", _cellIndex, _cellOptions);

                rendCellPath = _cellOptions[_cellIndex] + slash;

                string colCellPath = rendCellPath;
                switch (neuronSimulation.meshColScale)
                {
                    case NeuronSimulation1D.MeshColScaling.x1:
                        colCellPath += "1xDiameter" + slash;
                        break;
                    case NeuronSimulation1D.MeshColScaling.x2:
                        colCellPath += "2xDiameter" + slash;
                        break;
                    case NeuronSimulation1D.MeshColScaling.x3:
                        colCellPath += "3xDiameter" + slash;
                        break;
                    case NeuronSimulation1D.MeshColScaling.x4:
                        colCellPath += "4xDiameter" + slash;
                        break;
                    case NeuronSimulation1D.MeshColScaling.x5:
                        colCellPath += "5xDiameter" + slash;
                        break;
                    default:
                        Debug.LogError("ERROR");
                        break;
                }

                rendCellPath += "1xDiameter";

                string identifier = "x";
                switch (neuronSimulation.refinementLevel)
                {
                    case NeuronSimulation1D.RefinementLevel.x0:
                        identifier = "0ref";
                        break;
                    case NeuronSimulation1D.RefinementLevel.x1:
                        identifier = "1ref";
                        break;
                    case NeuronSimulation1D.RefinementLevel.x2:
                        identifier = "2ref";
                        break;
                    case NeuronSimulation1D.RefinementLevel.x3:
                        identifier = "3ref";
                        break;
                    case NeuronSimulation1D.RefinementLevel.x4:
                        identifier = "4ref";
                        break;
                    default:
                        Debug.LogError("ERROR");
                        break;
                }

                string[] rendRefinementOptions = Directory.GetDirectories(rendCellPath);
                string[] colRefinementOptions = Directory.GetDirectories(colCellPath);

                for (int i = 0; i < rendRefinementOptions.Length; i++)
                {
                    if (rendRefinementOptions[i].EndsWith(identifier))
                    {
                        rendCellPath = rendRefinementOptions[i];
                    }
                }
                for (int i = 0; i < colRefinementOptions.Length; i++)
                {
                    if (colRefinementOptions[i].EndsWith(identifier))
                    {
                        colCellPath = colRefinementOptions[i];
                    }
                }

                string[] cellNames = new string[5];

                // Get simulation and rendering files
                string[] files = Directory.GetFiles(rendCellPath);
                foreach (string file in files)
                {
                    // If this isn't a non-metadata ugx file,
                    if (!file.EndsWith(".meta") && file.EndsWith(ugxExt))
                    {
                        if (file.EndsWith(spec1D + ugxExt)) cellNames[1] = file;  // 1D cell
                        else if (file.EndsWith(specTris + ugxExt)) cellNames[2] = file;    // Triangles
                        else if (file.EndsWith(ugxExt)) cellNames[0] = file;     // If it isn't specified as 1D or triangles, it's most likely 3D
                    }
                }

                // Get interaction files
                cellNames[3] = "NULL"; // Blown up mesh default
                cellNames[4] = "NULL"; // Blown up mesh triangles default
                files = Directory.GetFiles(colCellPath);

                foreach (string file in files)
                {
                    // If this isn't a non-metadata ugx file,
                    if (!file.EndsWith(".meta"))
                    {
                        if (file.EndsWith(spec1D + ugxExt)) ; // 1D cell isn't needed for the blownup mesh
                        else if (file.EndsWith(specTris + ugxExt)) cellNames[4] = file;    // Triangles
                        else if (file.EndsWith(ugxExt)) cellNames[3] = file;    // If it isn't specified as 1D or triangles, it's most likely 3D
                    }
                }


                neuronSimulation.cellFile3D = cellNames[0];
                neuronSimulation.cellFile1D = cellNames[1];
                neuronSimulation.cellFileTriangles = cellNames[2];
                neuronSimulation.cellColliderFile3D = cellNames[3];
                neuronSimulation.cellColliderFileTriangles = cellNames[4];

                // Update the selected choice in the underlying object
                //someClass.choice = _choices[_choiceIndex];

                // Save the changes back to the object
                //EditorUtility.SetDirty(target);
            }
            // Draw the default inspector
            DrawDefaultInspector();

        }
    }
}