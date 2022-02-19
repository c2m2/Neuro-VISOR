using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;
using Random = System.Random;

namespace C2M2.NeuronalDynamics.Interaction {
    using Utils.DebugUtils;
    using NeuronalDynamics.Visualization;
    using Utils;
    public class CellPreviewer : MonoBehaviour
    {
        [Tooltip("Cell path relative to StreamingAssets")]
        /// <summary>
        /// Cell path relative to StreamingAssets
        /// </summary>
        public string cellsPath = "NeuronalDynamics" + Path.DirectorySeparatorChar + "Geometries";
        public GameObject previewWindowPrefab = null;
        public NDSimulationLoader loader = null;
        public bool renderWalls = true;
        public Color32 windowColor = Color.black;
        public GameObject ErrorWindow = null;

        public int maxPrevWnds = 6; // total number of preview windows
        string[] geoms; // neuron filenames (geometries)
        string fullPath; // absolute path of the geometries files
        Vector3[] windowPositions; // preview windows positions
        Color32[] previewColors; // preview neurons colors
        GameObject[] wnds; // array of preview windows objects
        int geomsStartInd = 0; // start position in geoms
        int geomsEndInd = 0; // end position in geoms
        bool cantScroll = false; // true if number of neurons is less or equal to number of preview windows
        static bool folderUpdated = false; // true if Geometries folder was updated

        /// <summary>
        /// Colors to use for the 1D cell renderings. More than cellColors.Length cells will repeat these colors
        /// </summary>
        public Color32[] cellColors = new Color32[]
        {
            new Color32(255, 200, 0, 255),
            new Color32(0, 200, 0, 255),
            new Color32(0, 100, 255, 255),
            new Color32(200, 0, 0, 255)
        };

        /// <summary>
        /// Defines the normalized positions of the first x-y row of preview windows.
        /// </summary>
        /// <remarks>
        /// The default array represents normalized positions of placing up to four preview windows in a four-by-one alignment.
        /// if stackPos is true, this will automatically "stack" positionsNorm above and below this array in the y-axis
        /// </remarks>
        public Vector3[] positionsNorm = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -2),
        };
        [Tooltip("If true, positionsNorm will stack above and below on the y axis")]
        public bool stackPos = true;

        // Functions that track the state of Geometries folder
        private static void OnDeleted(object sender, FileSystemEventArgs e) => folderUpdated = true;
        private static void OnCreated(object sender, FileSystemEventArgs e) => folderUpdated = true;

        private void Awake()
        {
            // Make sure we have window preview prefab and a pointer to a simulation loader
            FindWindowPrefab();
            FindSimulationLoader();

            // detect macOS
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
                cellsPath = "NeuronalDynamics" + Path.AltDirectorySeparatorChar + "Geometries";

            // Get possible geometries from given direcrory
            fullPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + cellsPath;
            geoms = GetGeometryNames(fullPath);

            // Setup for tracking Geometries folder
            var watcher = new FileSystemWatcher(fullPath);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Filter = "*.vrn";
            watcher.EnableRaisingEvents = true;

            wnds = new GameObject[maxPrevWnds];
            if (geoms.Length <= maxPrevWnds) cantScroll = true;

            if (geoms.Length > 0)
            {
                if (ErrorWindow != null) ErrorWindow.SetActive(false);

                // Make preview windows; maxPrevWnds in total
                windowPositions = GetWindowPositions(maxPrevWnds);
                previewColors = GetWindowColors(geoms.Length);
                for (int i = 0; i < maxPrevWnds; i++)
                    wnds[i] = InstantiatePreviewWindow(windowPositions[i]);

                // Populate the preview windows
                for (int i = 0; i < geoms.Length && i < maxPrevWnds; i++)
                    PopulatePreviewWindow(wnds[i], geoms[geomsEndInd++], previewColors[i]);
                --geomsEndInd; // set to actual end index
            }
            else
            {
                if (ErrorWindow != null)
                {
                    ErrorWindow.SetActive(true);
                    var go = ErrorWindow.transform.FindChildRecursive("FileName");
                    if (go != null)
                    {
                        TMPro.TextMeshProUGUI errorMsg = go.GetComponent<TMPro.TextMeshProUGUI>();
                        if (errorMsg != null)
                        {
                            errorMsg.text = "No cells found in " + fullPath;
                        }
                    }
                }
                Debug.LogWarning("No cells found in " + fullPath);
            }

            void FindWindowPrefab()
            {
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
            }
            void FindSimulationLoader()
            {
                if (loader == null)
                {
                    loader = GetComponent<NDSimulationLoader>();
                    if (loader == null)
                    {
                        Debug.LogError("No simulation loader given to CellPreviewer!");
                        Destroy(this);
                    }
                }
            }
            Vector3[] GetWindowPositions(int numWindows)
            {
                // Default Example: 0 <= numWindows <= 18
                // numWindows = Utils.Math.Clamp(numWindows, 0, positionsNorm.Length * 3); // Me
                if (numWindows == 0) return null;

                // We'll scale the default box positions by the preview window's edge length
                Vector3 windowLength = previewWindowPrefab.transform.localScale;

                Vector3[] positions = new Vector3[numWindows];

                for (int i = 0; i < numWindows; i++)
                {
                    // 0 <= i < 6 is the original row, 6 <= i < 12 is stacked ontop, 12 <= i < 18 is stacked below
                    int stackAmount = 0;
                    if (stackPos)
                    {
                        if (i < positionsNorm.Length) stackAmount = 0;
                        else if (i < positionsNorm.Length * 2) stackAmount = 1;
                        else if (i < positionsNorm.Length * 3) stackAmount = -1;
                    }

                    // possiblePositions only contains indices 0-5

                    int ind = (i % positionsNorm.Length);


                    // Copies positions from possiblePositions, stacks if necessary, and scales positions using window length
                    positions[i] = new Vector3(windowLength.x * positionsNorm[ind].x,
                        windowLength.y * (positionsNorm[ind].y + stackAmount),
                        windowLength.z * positionsNorm[ind].z);

                }

                return positions;
            }

            GameObject InstantiatePreviewWindow(Vector3 position)
            {
                // Instantiate window
                GameObject go = Instantiate(previewWindowPrefab);
                go.transform.parent = transform;
                go.transform.localPosition = position;
                go.name = "EmptyPreview";

                // Find each wall in window, color accoring to input
                MeshRenderer[] prefabWalls = go.GetComponentsInChildren<MeshRenderer>();
                if (prefabWalls.Length > 0)
                {
                    foreach (MeshRenderer r in prefabWalls)
                    {
                        r.enabled = renderWalls;
                        r.material.color = windowColor;
                    }
                }

                return go;

            }
        }

        string[] GetGeometryNames(string targetDir)
        {
            DirectoryInfo d = new DirectoryInfo(targetDir);

            FileInfo[] files = d.GetFiles("*.vrn");
            if (files.Length == 0) return new string[] { };

            string[] fileNames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                fileNames[i] = files[i].Name;
            }
            return fileNames;
        }

        Color32[] GetWindowColors(int numColors)
        {
            numColors = Utils.Math.Clamp(numColors, 0, positionsNorm.Length * 3);
            if (numColors == 0) return null;

            Color32[] colors = new Color32[numColors];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = cellColors[i % cellColors.Length];
            }
            Random rnd = new Random();

            // Randomize the order of the colors and return them
            return colors.OrderBy(x => rnd.Next()).ToArray();
        }

        void PopulatePreviewWindow(GameObject go, string fileName, Color color)
        {
            // Start neuron cell previewer.
            NeuronCellPreview preview = go.GetComponentInChildren<NeuronCellPreview>();
            preview.vrnFileName = fileName;
            preview.windowColor = color;
            preview.loader = loader;
            preview.PreviewCell(fileName, color);

            go.name = fileName + "Preview"; // update parent name
        }

        /// <summary>
        /// Clear the preview windows by destroying the LineRenderer object
        /// </summary>
        void ClearPreviewWnds(GameObject[] o)
        {
            for (int i = 0; i < o.Length; i++)
            {
                LinesRenderer linesRnd = o[i].GetComponentInChildren<LinesRenderer>();
                Destroy(linesRnd);
                o[i].name = "EmptyPreview";
            }
        }

        /// <summary>
        /// Scroll through geoms and update the preview windows.
        /// If scrollRight is true it will scroll right; otherwise it will scroll left.
        /// If cantScroll is true the function just returns.
        /// </summary>
        public void ScrollPreviewWnds(bool scrollRight)
        {
            if (cantScroll) return;
            else if (!scrollRight && geomsStartInd == 0) return;
            else if (scrollRight && geomsEndInd == geoms.Length - 1) return;

            for (int i = 0; i < maxPrevWnds; i++)
            {
                if (scrollRight)
                {
                    if (geomsEndInd == geoms.Length - 1)
                        break;
                    geomsStartInd++;
                    geomsEndInd++;
                }
                else
                {
                    if (geomsStartInd == 0)
                        break;
                    geomsStartInd--;
                    geomsEndInd--;
                }
            }

            ClearPreviewWnds(wnds);

            for (int i = 0, j = geomsStartInd; j <= geomsEndInd; i++, j++)
                PopulatePreviewWindow(wnds[i], geoms[j], previewColors[i]);
        }

        void Update()
        {
            // Files were added or removed from Geometries folder
            if (folderUpdated)
            {
                geoms = GetGeometryNames(fullPath); // reset geoms
                previewColors = GetWindowColors(geoms.Length); // reset previewColors
                folderUpdated = false;
                cantScroll = false;
                ClearPreviewWnds(wnds);

                if (geoms.Length <= maxPrevWnds) cantScroll = true;
                geomsStartInd = geomsEndInd = 0; // reset indices

                for (int i = 0; i < geoms.Length && i < maxPrevWnds; i++)
                    PopulatePreviewWindow(wnds[i], geoms[geomsEndInd++], previewColors[i]);
                --geomsEndInd; // set to actual end index
            }
        }
    }
}