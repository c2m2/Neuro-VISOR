using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace C2M2.NeuronalDynamics.Interaction {
    using Utils.DebugUtils;
    using NeuronalDynamics.Visualization;
    public class CellPreviewer : MonoBehaviour
    {
        public GameObject previewWindowPrefab = null;
        public LoadSimulation loader = null;
        private float boxLength = 0.5f;
        private void Awake()
        {
            // Make sure we have window preview prefab and a pointer to a simulation loader
            if (previewWindowPrefab == null)
            {
                Object prefab = Resources.Load("Prefabs/CellPreviewWindow");
                if (prefab == null)
                {
                    Debug.LogError("No cell preview window prefab found!");
                    Destroy(this);
                }
                previewWindowPrefab = (GameObject)prefab;
            }
            if(loader == null)
            {
                loader = GetComponent<LoadSimulation>();
                if(loader == null)
                {
                    Debug.LogError("No simulation loader given to CellPreviewer!");
                    Destroy(this);
                }
            }

            // Get possible geometries from given direcrory
            string[] geoms = GetGeometryNames();

            // Instantiate a preview panel for each found geometry
            Vector3[] windowPositions = GetWindowPositions(geoms.Length);
            Color[] previewColors = GetColors(geoms.Length);
            //string s = "Found " + geoms.Length + " geometries:";
            for(int i = 0; i < geoms.Length; i++)
            {
                GameObject go = Instantiate(previewWindowPrefab);
                go.transform.parent = transform;
                go.transform.localPosition = windowPositions[i];
                go.name = geoms[i] + "Preview";

                NeuronCellPreview preview = go.GetComponentInChildren<NeuronCellPreview>();
                preview.vrnFileName = geoms[i];
                preview.color = previewColors[i];
                preview.loader = loader;
                preview.PreviewCell(geoms[i], previewColors[i]);
            }
        }
        private string[] GetGeometryNames()
        {
            char sl = Path.DirectorySeparatorChar; ;
            string targetDir = Application.streamingAssetsPath + sl + "NeuronalDynamics" + sl + "Geometries";
            DirectoryInfo d = new DirectoryInfo(targetDir);

            FileInfo[] files = d.GetFiles("*.vrn");
            if (files.Length == 0) return null;

            string[] fileNames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                fileNames[i] = files[i].Name;
            }
            return fileNames;
        }


        private Vector3[] GetWindowPositions(int numWindows)
        {
            // 0 <= numWindows <= 18
            numWindows = Utils.Math.Clamp(numWindows, 0, possiblePositions.Length * 3);
            if (numWindows == 0) return null;

            float scaler = previewWindowPrefab.transform.localScale.x;

            Vector3[] positions = new Vector3[numWindows];

            for(int i = 0; i < numWindows; i++)
            {
                if(i < possiblePositions.Length)
                {
                    positions[i] = possiblePositions[i];
                }
                else if(i < possiblePositions.Length * 2)
                {
                    positions[i] = new Vector3(possiblePositions[(i % possiblePositions.Length)].x, 1, possiblePositions[(i % possiblePositions.Length)].z);
                }
                else if (i < possiblePositions.Length * 3)
                {
                    positions[i] = new Vector3(possiblePositions[(i % possiblePositions.Length)].x, -1, possiblePositions[(i % possiblePositions.Length)].z);
                }
                positions[i] = scaler * positions[i];
            }

            return positions;
        }

        private Color[] GetColors(int numColors)
        {
            numColors = Utils.Math.Clamp(numColors, 0, possiblePositions.Length * 3);
            if (numColors == 0) return null;

            Color[] colors = new Color[numColors];
            for(int i = 0; i < colors.Length; i++)
            {
                colors[i] = possibleColors[i % possibleColors.Length];
            }

            return colors;
        }

        public Vector3[] possiblePositions = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, -1)
        };
        public Color[] possibleColors = new Color[]
        {
            new Color(255, 200, 0),
            new Color(0, 200, 0),
            new Color(0, 100, 255),
            new Color(200, 0, 0)
        };
    }
}