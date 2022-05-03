using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

namespace C2M2
{
    using Utils;
    using NeuronalDynamics.Simulation;
    using NeuronalDynamics.Interaction;
    using NeuronalDynamics.Interaction.UI;

    /// <summary>
    /// Provides Save and Load functionality for cells
    /// </summary>
    public class Menu : MonoBehaviour
    {
        private const int MAX_SAVE_FILES = 9; // max # of files that can be created and saved to
        private DateTime date = DateTime.Today;
        private int file_index; // keep track of how many files are saved already
        private String filename; // filename to save to
        private String[] files = null; // list of files saved
        private bool filelist_visible = false; // if true the children of File object under SaveLoad are active

        GameManager gm = null; // current instance of GameManager

        private CellData data = null; // data for each cell
        private SynapseManager synM = null;
        private SynapseData synD = null;
        private NDSimulationLoader loader = null; // current loader used
        private string path; // save/load file path

        // this is for restoring the gradient saved
        public int gradientIndex = 0;
        public bool loading = false;
        public bool finishedLoading = false;

        // camera transform
        [System.Serializable]
        public struct CamTransform
        {
            public Vector3 camPos;
            public Vector3 camRot;
            public Vector3 xyz; //x, y, z values from MovementController.cs

            public CamTransform(Vector3 pos, Vector3 rot, Vector3 abc)
            {
                camPos = pos;
                camRot = rot;
                xyz = abc;
            }
        }

        // current time step
        [System.Serializable]
        public struct CurrentTimeStep // have to create a struct because of Json
        {
            public int currentTimeStep;

            public CurrentTimeStep(int time)
            {
                currentTimeStep = time;
            }
        }

        // Graph manager
        private NDGraphManager graphM = null;

        void Awake()
        {
            path = Application.dataPath + "/";
            if (!Directory.Exists(path + "Save")) Directory.CreateDirectory(path + "Save");
            path += "Save/";
            gm = GameManager.instance;
            if (File.Exists(path + "/settings.txt"))
                file_index = int.Parse(File.ReadAllLines(path + "/settings.txt")[0]);
            else file_index = 0;
        }

        private void OnDestroy()
        {
            File.WriteAllText(path + "/settings.txt", file_index.ToString()); // save current file index to settings.txt
        }

        /// <summary>
        /// Save the current scene.
        /// </summary>
        public void Save()
        {
            if (gm.activeSims.Count > 0)
            {
                // hide save button
                SaveButtonVisible(false);

                // reset file_index
                if (file_index == MAX_SAVE_FILES)
                    file_index = 0;

                // filename format: index_date-time.dat
                filename = file_index + "_" + date.Date.ToString("M-d-yyyy") + "-" + DateTime.Now.ToString("HH-mm-ss") + ".dat";
                // delete and replace file with the same index
                String[] files = Directory.GetFiles(path, file_index + "_*");
                foreach (String f in files)
                    File.Delete(f);
                file_index++;

                StreamWriter sw = File.AppendText(path + filename);
                int limit = gm.activeSims.Count - 1;

                // save camera position and rotation (for desktop only!)
                MovementController cam = FindObjectOfType<MovementController>();
                if (cam != null && cam.transform.parent.gameObject.activeSelf)
                {
                    CamTransform c = new CamTransform(cam.transform.position, cam.transform.eulerAngles, cam.getXYZ());
                    string saveCam = JsonUtility.ToJson(c); // convert to Json
                    sw.Write(saveCam + ";");
                }

                SparseSolverTestv1 s = (SparseSolverTestv1)gm.activeSims[0];

                // save current time step
                CurrentTimeStep t = new CurrentTimeStep(s.curentTimeStep);
                string sCurrT = JsonUtility.ToJson(t);
                sw.Write(sCurrT + ";");

                // get current gradient
                ChangeGradient grad = FindObjectOfType<ChangeGradient>();
                int gIndex = grad.GetActiveGradient();

                // save cells
                for (int i = 0; i <= limit; i++)
                {
                    SparseSolverTestv1 sim = (SparseSolverTestv1)gm.activeSims[i];
                    graphM = sim.graphManager;

                    // fill in the variables to be saved
                    data = new CellData();

                    data.U = sim.Get1DValues(); // voltage at every node
                    data.M = sim.getM(); // M vector
                    data.N = sim.getN(); // N vector
                    data.H = sim.getH(); // H vector

                    data.Upre = sim.getUpre(); // Upre vector
                    data.Mpre = sim.getMpre(); // Mpre vector
                    data.Npre = sim.getNpre(); // Npre vector
                    data.Hpre = sim.getHpre(); // Hpre vector

                    data.simID = sim.simID;
                    data.pos = sim.transform.position;
                    data.rotation = sim.transform.rotation;
                    data.scale = sim.transform.localScale;
                    data.vrnFileName = sim.vrnFileName;
                    data.gradientIndex = gIndex;
                    data.refinementLevel = sim.RefinementLevel;
                    data.timeStep = sim.timeStep;
                    data.endTime = sim.endTime;

                    // save clamps
                    if (sim.clamps.Count > 0)
                    {
                        data.clamps = new CellData.ClampData[sim.clamps.Count];

                        for (int j = 0; j < data.clamps.Length; j++)
                        {
                            data.clamps[j].vertex1D = sim.clamps[j].FocusVert;
                            data.clamps[j].live = sim.clamps[j].ClampLive;
                            data.clamps[j].power = sim.clamps[j].ClampPower;
                        }
                    }

                    // save graphs
                    if (graphM.interactables.Count > 0)
                    {
                        data.graphs = new CellData.Graph[graphM.interactables.Count];
                        for (int j = 0; j < data.graphs.Length; j++)
                        {
                            data.graphs[j].vertex = graphM.interactables[j].FocusVert;
                            data.graphs[j].positions = new Vector3[graphM.interactables[j].ndlinegraph.positions.Count];

                            for (int k = 0; k < graphM.interactables[j].ndlinegraph.positions.Count; k++)
                                data.graphs[j].positions[k] = graphM.interactables[j].ndlinegraph.positions[k];
                        }
                    }

                    string json = JsonUtility.ToJson(data); // convert to Json
                    sw.Write(json);
                    if (i != limit)
                        sw.Write(';'); // add delimiter unless it's the last object
                }

                // write synapses list to file (Json doesn't work with List)
                synM = gm.simulationManager.synapseManager;
                sw.Write(';');
                synD = new SynapseData();

                if (synM.synapses.Count > 0)
                {
                    synD.syns = new SynapseData.SynData[synM.synapses.Count];
                    for (int j = 0; j < synM.synapses.Count; j++)
                    {
                        synD.syns[j*2].synVert = synM.synapses[j].Item1.FocusVert;
                        synD.syns[j*2].simID = synM.synapses[j].Item1.simulation.simID;
                        synD.syns[(j*2)+1].synVert = synM.synapses[j].Item2.FocusVert;
                        synD.syns[(j*2)+1].simID = synM.synapses[j].Item2.simulation.simID;
                    }
                }
                string jSon = JsonUtility.ToJson(synD);
                sw.Write(jSon);

                sw.Close();

                StartCoroutine(ShowSaveButton()); // show save button after 1.5 seconds
            }
        }

        /// <summary>
        /// Show save button if it is possible.
        /// </summary>
        IEnumerator ShowSaveButton()
        {
            yield return new WaitForSeconds(1.5f);
            if (!filelist_visible && !gm.cellPreviewer.activeInHierarchy)
                SaveButtonVisible(true);
        }

        /// <summary>
        /// Load a file. Return true if successful.
        /// </summary>
        public bool Load(String f)
        {
            ClearScene();

            loader = gm.cellPreviewer.GetComponentInChildren<NDSimulationLoader>();

            if (loader != null)
            {
                loading = true; // this is for ChangeGradient
                gm.Loading = true;
                string[] json;

                try
                {
                    json = File.ReadAllText(path + f + ".dat").Split(';');
                }
                catch (FileNotFoundException e)
                {
                    Debug.Log(e.Message);
                    loading = false;
                    gm.Loading = false;
                    finishedLoading = true;
                    return false;
                }

                int i = 0; // index for json string

                // load camera position and rotation (for desktop only!)
                MovementController cam = FindObjectOfType<MovementController>();
                if (cam != null && cam.transform.parent.gameObject.activeSelf)
                {
                    CamTransform c = JsonUtility.FromJson<CamTransform>(json[i]);
                    cam.transform.position = c.camPos;
                    cam.transform.eulerAngles = c.camRot;
                    cam.setXYZ(c.xyz);
                }
                ++i;

                CurrentTimeStep t = JsonUtility.FromJson<CurrentTimeStep>(json[i++]);

                int limit = json.Length - 2; // account for the synapses array at the end of the file

                int ID = 0; // this will be the current ID when placing a new cell after loading

                for (; i <= limit; i++)
                {
                    // retrieve saved data
                    data = JsonUtility.FromJson<CellData>(json[i]);
                    loader.vrnFileName = data.vrnFileName;
                    loader.refinementLevel = data.refinementLevel;
                    loader.timestepSize = data.timeStep;
                    loader.endTime = data.endTime;

                    // restore vectors
                    gm.U = data.U;
                    gm.M = data.M;
                    gm.N = data.N;
                    gm.H = data.H;

                    gm.Upre = data.Upre;
                    gm.Mpre = data.Mpre;
                    gm.Npre = data.Npre;
                    gm.Hpre = data.Hpre;

                    GameObject go;
                    try
                    {
                        go = loader.Load(new RaycastHit()); // load the cell
                    }
                    catch (FileNotFoundException e)
                    {
                        go = FindObjectOfType<SparseSolverTestv1>().gameObject;
                        gm.activeSims.RemoveAt(gm.activeSims.Count-1);
                        Destroy(go);
                        Debug.Log(e.Message);
                        loading = false;
                        gm.Loading = false;
                        finishedLoading = true;
                        return false;
                    }

                    SparseSolverTestv1 sim = go.GetComponent<SparseSolverTestv1>();

                    // restore cell ID
                    sim.simID = data.simID;
                    ID = sim.simID;

                    go.transform.position = data.pos;
                    go.transform.rotation = data.rotation;
                    go.transform.localScale = data.scale;

                    // get clamp manager
                    NeuronClampManager clampMng = sim.clampManager;

                    // set current time step
                    sim.curentTimeStep = t.currentTimeStep;

                    // recreate clamps
                    if (data.clamps.Length > 0)
                    {
                        for (int j = 0; j < data.clamps.Length; j++)
                        {
                            NeuronClamp clamp;
                            clamp = Instantiate(clampMng.clampPrefab, go.transform).GetComponentInChildren<NeuronClamp>();
                            clamp.AttachToSimulation(sim, data.clamps[j].vertex1D);

                            clamp.ClampPower = data.clamps[j].power;
                            if (data.clamps[j].live) clamp.ActivateClamp();
                        }
                    }

                    // recreate graphs
                    if (data.graphs.Length > 0)
                    {
                        graphM = sim.graphManager;
                        GameObject graphPrefab = Resources.Load("Prefabs" + Path.DirectorySeparatorChar + "NeuronalDynamics" + Path.DirectorySeparatorChar + "NDLineGraph") as GameObject;
                        for (int j = 0; j < data.graphs.Length; j++)
                        {
                            var graphObj = Instantiate(graphPrefab);
                            NDLineGraph g = graphObj.GetComponent<NDLineGraph>();
                            g.ndgraph.FocusVert = data.graphs[j].vertex;
                            g.ndgraph.simulation = sim;
                            graphM.interactables.Add(g.ndgraph);
                            foreach (Vector3 v in data.graphs[j].positions)
                                g.positions.Add(v);
                        }
                    }

                    // each cell has the same gradient so assign gradientIndex only once
                    // the gradient is applied once loading is finished; check ChangeGradient.cs
                    if (i == limit)
                        gradientIndex = data.gradientIndex;
                }

                // recreate synapses
                synD = JsonUtility.FromJson<SynapseData>(json[i]);
                synM = gm.simulationManager.synapseManager;

                for (int j = 0; j < synD.syns.Length; j++)
                {
                    Synapse syn;
                    NDSimulation ndsim = null; ;
                    foreach (NDSimulation sim in gm.activeSims)
                    {
                        if (sim.simID == synD.syns[j].simID)
                        {
                            ndsim = sim;
                            break;
                        }
                    }
                    syn = Instantiate(GameManager.instance.synapseManagerPrefab.GetComponent<SynapseManager>().synapsePrefab, ndsim.transform).GetComponentInChildren<Synapse>();
                    syn.AttachToSimulation(ndsim, synD.syns[j].synVert);
                }

                finishedLoading = true; // this is for ChangeGradient
                gm.Loading = false;
                GameManager.simID = ID;

                // set paused
                gm.simulationManager.Paused = true;
            }
            else
            {
                Debug.LogError("Loader is null!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clear the scene in order to load a file.
        /// </summary>
        public void ClearScene()
        {
            GameObject controlPanel = GameObject.FindGameObjectWithTag("ControlPanel");
            if (controlPanel != null)
            {
                NDBoardController c = controlPanel.GetComponent<NDBoardController>();
                c.CloseAllSimulations();
                DestroyImmediate(c.gameObject);
                DestroyImmediate(controlPanel);
            }
        }

        /// <summary>
        /// Loads the selected (clicked) file.
        /// </summary>
        public void LoadThisFile(RaycastHit hit)
        {
            GameObject g = hit.collider.gameObject.transform.GetChild(1).gameObject;
            TextMeshProUGUI text = g.GetComponent<TextMeshProUGUI>();

            Transform t = hit.collider.transform.parent.parent;
            for (int i = 0; i < t.childCount; i++)
                t.GetChild(i).gameObject.SetActive(false);

            if (!Load(text.text))
                CloseFileList();

            LoadButtonVisible(true);
            CloseButtonVisible(false);
        }

        /// <summary>
        /// Lists the filename objects.
        /// </summary>
        public void ListSavedFiles()
        {
            files = Directory.GetFiles(path, "*.dat");

            if (files.Length > 0)
            {
                GameObject controlPanel = GameObject.FindGameObjectWithTag("ControlPanel");
                if (controlPanel != null)
                {
                    NDBoardController c = controlPanel.GetComponent<NDBoardController>();
                    c.MinimizeBoard(true);
                }

                if (gm.cellPreviewer != null) gm.cellPreviewer.SetActive(false);

                SaveButtonVisible(false);
                LoadButtonVisible(false);
                CloseButtonVisible(true);

                Transform g = gameObject.transform.GetChild(3);
                for (int i = 0; i < files.Length; i++)
                {
                    g.GetChild(i).gameObject.SetActive(true);
                    TextMeshProUGUI t = g.GetChild(i).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();
                    t.text = Path.GetFileNameWithoutExtension(files[i]);
                }
                filelist_visible = true;
            }
        }

        /// <summary>
        /// Close filename objects.
        /// </summary>
        public void CloseFileList()
        {
            if (filelist_visible)
            {
                CloseButtonVisible(false);

                Transform g = gameObject.transform.GetChild(3);
                for (int i = 0; i < files.Length; i++)
                    g.GetChild(i).gameObject.SetActive(false);

                filelist_visible = false;
                LoadButtonVisible(true);

                // if controlPanel is active unminimize it, otherwise show the cell previewer
                GameObject controlPanel = GameObject.FindGameObjectWithTag("ControlPanel");
                if (controlPanel != null)
                {
                    NDBoardController c = controlPanel.GetComponent<NDBoardController>();
                    c.MinimizeBoard(false);
                    SaveButtonVisible(true);
                }
                else
                    gm.cellPreviewer.SetActive(true);
            }

        }

        /*
         * Show/hide buttons.
         */

        public void SaveButtonVisible(bool visible)
        {
            GameObject save = gameObject.transform.GetChild(0).gameObject;
            save.SetActive(visible);
        }

        public void LoadButtonVisible(bool visible)
        {
            GameObject load = gameObject.transform.GetChild(1).gameObject;
            load.SetActive(visible);
        }

        public void CloseButtonVisible(bool visible)
        {
            GameObject close = gameObject.transform.GetChild(2).gameObject;
            close.SetActive(visible);
        }
    }
}
