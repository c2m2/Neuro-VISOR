using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;
using Random = System.Random;

namespace C2M2.NeuronalDynamics.Interaction
{
    using Utils.DebugUtils;
    using NeuronalDynamics.Visualization;
    using System.Net;

    public class CellPreviewer : MonoBehaviour
    {
        
            [Tooltip("Cell path relative to StreamingAssets")]
            /// <summary>
            /// Cell path relative to StreamingAssets
            /// </summary>
            public string cellsPath = "NeuronalDynamics/Geometries";
            public GameObject previewWindowPrefab = null;
            public NDSimulationLoader loader = null;
            public bool renderWalls = true;
            public Color32 windowColor = Color.black;
            public GameObject ErrorWindow = null;
            public List<FileInfo> files = new List<FileInfo>();
            public List<FileInfo> newFiles = new List<FileInfo>();
            FileSystemWatcher watcher = new FileSystemWatcher();
            CellPreviewerController controller;

            /// <summary>
            /// Colors ot use for the 1D cell renderings. More than cellColors.Length cells will repeat these colors
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
            new Vector3(0, 0, -1),
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -2)
          
            };

            [Tooltip("If true, positionsNorm will stack above and below on the y axis")]
            public bool stackPos = true;
            public void generateCellPreviewer()
            {

                // Make sure we have window preview prefab and a pointer to a simulation loader
                FindWindowPrefab();
                FindSimulationLoader();

                // Get possible geometries from given direcrory
                string fullPath = Path.Combine(Application.streamingAssetsPath, cellsPath);
                if (!Directory.Exists(fullPath))
                {
                    Debug.LogWarning("CellPreviewer: Directory not found, creating: " + fullPath);
                    Directory.CreateDirectory(fullPath);
                }
                watcher.Path = fullPath;
                watcher.Filter = "*.VRN";
                List<string> geoms = GetGeometryNames(fullPath);
                if (geoms.Count > 0)
                {
                    if (ErrorWindow != null) ErrorWindow.SetActive(false);

                    // Make a preview window for each found geometry
                    Vector3[] windowPositions = GetWindowPositions(geoms.Count);
                    Color32[] previewColors = GetWindowColors(geoms.Count);
                    for (int i = 0; i < windowPositions.Length; i++)
                    {
                        InstantiatePreviewWindow(geoms[i], windowPositions[i], previewColors[i]);
                    }
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
                List<string> GetGeometryNames(string targetDir)
                {
                    DirectoryInfo d = new DirectoryInfo(targetDir);
                    if (files.Count == 0) { files = d.GetFiles("*.vrn").ToList(); }
                    int pageCount = controller.getPage();
                    int cellCount = controller.getCellSize();
                    int startIndex = pageCount * cellCount;
                    int range = System.Math.Min(cellCount, files.Count - startIndex); 
                    if (files.Count == 0) return new List<string> { };
                    List<string> fileNames = new List<string>();
                    if (pageCount != 0)
                    {
                        bool success = false;
                        while (!success && pageCount != 0)
                        {

                            try
                            {
                                if (System.Math.Min(cellCount, files.Count - startIndex) > 0) { range = System.Math.Min(cellCount, files.Count - startIndex); }
                                else
                                {
                                    throw new System.ArgumentOutOfRangeException("all files on page deleted");
                                }


                                success = true;
                            }


                            catch (System.ArgumentOutOfRangeException)
                            {
                                pageCount -= 1;
                                controller.setPage(pageCount);
                                startIndex = pageCount * cellCount;
                                range = System.Math.Min(cellCount, files.Count - startIndex);
                            }

                        }

                    foreach (FileInfo i in files.GetRange(startIndex, range))
                        {
                            fileNames.Add(i.Name);

                        }

                    }
                    else if (pageCount == 0)
                    {
                    if (files.Count >= cellCount)
                        {
                            foreach (FileInfo i in files.GetRange(0,cellCount))
                            {
                                fileNames.Add(i.Name);
                                
                            }
                        }
                        else
                        {
                            foreach (FileInfo i in files.GetRange(0, files.Count))
                            {
                                fileNames.Add(i.Name);
                            }
                        }
                    }
                    return fileNames;
                }
           
                Vector3[] GetWindowPositions(int numWindows)
                {
                    // Default Example: 0 <= numWindows <= 18
                    numWindows = Utils.Math.Clamp(numWindows, 0, positionsNorm.Length * 3);
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
                        if (i < positionsNorm.Length) stackAmount = 1;
                        else if (i < positionsNorm.Length * 2) stackAmount = 0;
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
                void InstantiatePreviewWindow(string fileName, Vector3 position, Color color)
                {
                    // Instantiate window
                    GameObject go = Instantiate(previewWindowPrefab);
                    go.transform.parent = transform;
                    go.transform.localPosition = position;
                    go.name = fileName + "Preview";

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

                    // Start neuron cell previewer.
                    NeuronCellPreview preview = go.GetComponentInChildren<NeuronCellPreview>();
                    preview.vrnFileName = fileName;
                    preview.windowColor = color;
                    preview.loader = loader;
                    preview.PreviewCell(fileName, color);



                }
                geoms.RemoveRange(0, geoms.Count);

            }


            private void ConfigureFileSystemWatcher(string path)
            {
                if (!Directory.Exists(path))
                {
                    Debug.LogWarning("CellPreviewer: Directory not found, creating: " + path);
                    Directory.CreateDirectory(path);
                }
                watcher.Path = path;
                watcher.Filter = "*.vrn";
                watcher.EnableRaisingEvents = true;
                watcher.Created += OnFileCreated;
                watcher.Deleted += OnFileDelete;
                watcher.Renamed += OnFileRename;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
            {

                // Debug.Log($"New file created: {e.FullPath}");
                FileInfo fileInfo = new FileInfo(e.FullPath);
                newFiles.Add(fileInfo);

            }
            private void OnFileDelete(object sender, FileSystemEventArgs e)
            {
                FileInfo fileInfo = new FileInfo(e.FullPath);

                var existingFileInfo = files.FirstOrDefault(f => f.FullName == fileInfo.FullName);
            if (existingFileInfo != null)
            {
                files.Remove(existingFileInfo);


            }
            else
            {
                var newExistingFileInfo = newFiles.FirstOrDefault(f => f.FullName == fileInfo.FullName);
                 if (newExistingFileInfo != null)
                {
                    newFiles.Remove(newExistingFileInfo);
                }
            }
            }
        private void OnFileRename(object sender, RenamedEventArgs e)
        {
           
            FileInfo oldFileInfo = new FileInfo(e.OldFullPath);
            FileInfo newFileInfo = new FileInfo(e.FullPath);

          
            var existingFileInfo = files.FirstOrDefault(f => f.FullName == oldFileInfo.FullName);
            if (existingFileInfo != null)
            {

                files.Remove(oldFileInfo);
                files.Add(newFileInfo);
            }
            else
            {
                var newExistingFileInfo = newFiles.FirstOrDefault(f => f.FullName == oldFileInfo.FullName);
                if (newExistingFileInfo != null)
                {
                    newFiles.Remove(oldFileInfo);
                    newFiles.Add(newFileInfo);
                }
            }
        }

        private void Start()
            {
                string fullPath = Path.Combine(Application.streamingAssetsPath, cellsPath);
                ConfigureFileSystemWatcher(fullPath);
                GameObject cellobj = GameObject.Find("Arrows");
                controller = cellobj.GetComponent<CellPreviewerController>();
                StartCoroutine(DelayedPreviewerStart(0.01f));
                //generateNeuron();
            }
        /// <summary>
        /// The length of position Norms equal 0 on awake. After 1st frame position norms equal 3 thus cellPreviewer must be delayed for a small instance of time.
        /// </summary>
            IEnumerator DelayedPreviewerStart(float delayInSeconds)
            {
                // Wait for the specified delay
                yield return new WaitForSeconds(delayInSeconds);
                generateCellPreviewer();

            }


        }


    }

    

