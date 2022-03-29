using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        GameManager gm = null; // current instance of GameManager

        private CellData data = null; // data for each cell
        private SynapseManager synM = null;
        private SynapseData synD = null;
        public NDSimulationLoader loader = null; // current loader used
        private string path; // save/load file path (for now)

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
        public NDGraphManager graphM = null;

        void Awake()
        {
            path = Application.dataPath + "/save.dat";
            gm = GameManager.instance;
        }

        public void Save()
        {
            if (gm.activeSims.Count > 0)
            {
                File.Delete(path); // for now
                StreamWriter sw = File.AppendText(path);
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

                // get graph manager
                graphM = s.graphManager;

                // save cells
                for (int i = 0; i <= limit; i++)
                {
                    SparseSolverTestv1 sim = (SparseSolverTestv1)gm.activeSims[i];

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
                            data.clamps[j].vertex1D = sim.clamps[j].focusVert;
                            data.clamps[j].live = sim.clamps[j].ClampLive;
                            data.clamps[j].power = sim.clamps[j].ClampPower;
                        }
                    }

                    // save graphs
                    if (graphM.graphs.Count > 0)
                    {
                        data.graphs = new CellData.Graph[graphM.graphs.Count];
                        for (int j = 0; j < data.graphs.Length; j++)
                        {
                            data.graphs[j].vertex = graphM.graphs[j].vert;
                            data.graphs[j].positions = new Vector3[graphM.graphs[j].positions.Count];

                            for (int k = 0; k < graphM.graphs[j].positions.Count; k++)
                                data.graphs[j].positions[k] = graphM.graphs[j].positions[k];
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
                        synD.syns[j].synVert = synM.synapses[j].nodeIndex;
                        synD.syns[j].simID = synM.synapses[j].curSimulation.simID;
                    }
                }
                string jSon = JsonUtility.ToJson(synD);
                sw.Write(jSon);

                sw.Close();
            }
        }

        public void Load()
        {
            ClearScene();

            loader = gm.cellPreviewer.GetComponentInChildren<NDSimulationLoader>();

            if (loader != null)
            {
                loading = true; // this is for ChangeGradient
                gm.Loading = true;

                string[] json = File.ReadAllText(path).Split(';');

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
                        Destroy(go);
                        Debug.Log(e.Message);
                        return;
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
                    clampMng.currentSimulation = sim;
                    if (data.clamps.Length > 0)
                    {
                        for (int j = 0; j < data.clamps.Length; j++)
                        {
                            NeuronClamp clamp;
                            clamp = Instantiate(clampMng.clampPrefab, go.transform).GetComponentInChildren<NeuronClamp>();
                            clamp.AttachSimulation(sim, data.clamps[j].vertex1D);

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
                            g.vert = data.graphs[j].vertex;
                            g.sim = sim;
                            g.manager = graphM;
                            graphM.graphs.Add(g);
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
                    foreach (NDSimulation sim in gm.activeSims)
                    {
                        if (sim.simID == synD.syns[j].simID)
                        {
                            synM.focusVert = synD.syns[j].synVert;
                            synM.curSimulation = sim;
                            // the function automatically takes care of post synapses too
                            synM.preSynapticPlacement(new RaycastHit());
                        }
                    }
                }

                finishedLoading = true; // this is for ChangeGradient
                gm.Loading = false;
                GameManager.simID = ID;

                // set paused
                NDPauseButton pauseBtn = FindObjectOfType<NDPauseButton>();
                pauseBtn.TogglePause();
            }
            else
                Debug.LogError("Check that loader are not null in Menu!");
        }

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
    }
}
