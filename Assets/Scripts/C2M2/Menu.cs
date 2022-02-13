using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace C2M2
{
    using Interaction;
    using NeuronalDynamics.Simulation;
    using NeuronalDynamics.Interaction;
    using NeuronalDynamics.Interaction.UI;
    using NeuronalDynamics.UGX;

    /// <summary>
    /// Provides Save and Load functionality for cells
    /// </summary>
    public class Menu : MonoBehaviour
    {
        private CellData data; // data for each cell
        public NDSimulationLoader loader = null; // current loader used
        private string path; // save/load file path (for now)

        void Awake()
        {
            path = Application.dataPath + "/save.dat";
        }

        public void Save()
        {
            NDSimulation[] cells; // array of all the cells in the scene

            cells = FindObjectsOfType<SparseSolverTestv1>(); // get all the cells in the scene

            if (cells.Length > 0)
            {
                File.Delete(path); // for now
                StreamWriter sw = File.AppendText(path);
                int limit = cells.Length - 1;

                for (int i = 0; i <= limit; i++)
                {
                    // fill in the variables to be saved
                    data = new CellData();

                    data.vals1D = cells[i].vals1D; // voltage at every node

                    data.pos = cells[i].transform.position;
                    //data.rotation = cells[i].transform.rotation;
                    //data.scale = cells[i].transform.scale;
                    data.vrnFileName = cells[i].vrnFileName;
                    data.gradient = cells[i].gradient;
                    data.globalMin = cells[i].globalMin;
                    data.globalMax = cells[i].globalMax;
                    data.timeStep = cells[i].timeStep;
                    data.endTime = cells[i].endTime;
                    data.raycastHitValue = cells[i].raycastHitValue;
                    data.unit = cells[i].unit;
                    data.unitScaler = cells[i].unitScaler;
                    data.colorMarkerPrecision = cells[i].colorMarkerPrecision;

                    if (cells[i].clamps.Count > 0)
                    {
                        data.clamp = new ClampData[cells[i].clamps.Count];

                        for (int j = 0; j < data.clamp.Length; j++)
                        {
                            data.clamp[j] = new ClampData();
                            data.clamp[j].vertex = cells[i].clamps[j].focusVert;
                            data.clamp[j].live = cells[i].clamps[j].ClampLive;
                            data.clamp[j].power = cells[i].clamps[j].ClampPower;
                        }
                    }

                    string json = JsonUtility.ToJson(data); // convert to Json
                    sw.Write(json);
                    if (i != limit)
                        sw.Write(';'); // add delimiter unless it's the last object
                }

                sw.Close();
            }
        }

        public void Load()
        {
            if (loader != null)
            {
                NeuronClampManager clampMng = GameManager.instance.ndClampManager;

                ClearScene();
                string[] json = File.ReadAllText(path).Split(';');

                for (int i = 0; i < json.Length; i++)
                {
                    // retrieve saved data
                    data = JsonUtility.FromJson<CellData>(json[i]);
                    loader.vrnFileName = data.vrnFileName;
                    loader.gradient = data.gradient;
                    loader.globalMin = data.globalMin;
                    loader.globalMax = data.globalMax;
                    loader.timestepSize = data.timeStep;
                    loader.endTime = data.endTime;
                    loader.raycastHitValue = data.raycastHitValue;
                    loader.unit = data.unit;
                    loader.unitScaler = data.unitScaler;
                    loader.colorScalePrecision = data.colorMarkerPrecision;

                    GameObject go = loader.Load(new RaycastHit()); // load the cell
                    go.transform.position = data.pos;
                    //go.transform.rotation = data.rotation;
                    //go.transform.scale = data.scale;
                    NDSimulation sim = go.GetComponent<SparseSolverTestv1>();

                    // recreate voltages at every node
                    Tuple<int, double>[] values = new Tuple<int, double>[data.vals1D.Length];
                    for (int j = 0; j < data.vals1D.Length; j++)
                        values[j] = Tuple.Create(j, data.vals1D[j]);
                    sim.Set1DValues(values);

                    // recreate clamps
                    clampMng.currentSimulation = sim;
                    if (data.clamp.Length > 0)
                    {
                        for (int j = 0; j < data.clamp.Length; j++)
                        {
                            NeuronClamp clamp;
                            clamp = Instantiate(clampMng.clampPrefab, go.transform).GetComponentInChildren<NeuronClamp>();
                            clamp.AttachSimulation(sim, data.clamp[j].vertex);

                            clamp.ClampPower = data.clamp[j].power;
                            if (data.clamp[j].live) clamp.ActivateClamp();
                        }
                    }
                }

                // set paused to true
                NDPauseButton pauseBtn = FindObjectOfType<NDPauseButton>();
                pauseBtn.PauseState = true;
            }
            else
                Debug.LogError("Check that loader are not null in Menu!");
        }

        public void ClearScene()
        {
            CloseNDSimulation[] sims;

            sims = FindObjectsOfType<CloseNDSimulation>();

            for (int i = 0; i < sims.Length; i++)
                sims[i].CloseSimulation();
        }
    }
}
