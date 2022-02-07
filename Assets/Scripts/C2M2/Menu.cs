﻿using System;
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
        public NeuronClampManager clampMng = null;
        private string path; // save/load file path (for now)

        // Simulation state
        private NDPauseButton pauseBtn; // TODO: change how paused is saved; not best version right now

        void Awake()
        {
            path = Application.dataPath + "/save.dat";
        }

        public void Save()
        {
            if (clampMng == null)
            {
                Debug.Log("No clamp manager given to Menu!");
                return;
            }

            NDSimulation[] cells; // array of all the cells in the scene

            cells = FindObjectsOfType<SparseSolverTestv1>(); // get all the cells in the scene

            if (cells.Length > 0)
            {
                File.Delete(path); // for now
                StreamWriter sw = File.AppendText(path);
                int limit = cells.Length - 1;

                pauseBtn = FindObjectOfType<NDPauseButton>();

                for (int i = 0; i <= limit; i++)
                {
                    // fill in the variables to be saved
                    data = new CellData();

                    data.paused = pauseBtn.PauseState;
                    data.vals1D = cells[i].vals1D; // voltage at every node

                    data.pos = cells[i].transform.position;
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

                    if (clampMng.clampIndices.Count > 0)
                    {
                        data.clampIndices = new List<int>();
                        for (int j = 0; j < clampMng.clampIndices.Count; j++)
                            data.clampIndices.Add(clampMng.clampIndices[j]);
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
            if (loader != null && clampMng != null)
            {
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

                    // recreate voltages at every node
                    NDSimulation sim = go.GetComponent<SparseSolverTestv1>();
                    Neuron n = sim.Neuron;
                    Tuple<int, double>[] values = new Tuple<int, double>[n.nodes.Count];

                    for (int j = 0; j < n.nodes.Count; j++)
                        values[j] = Tuple.Create(j, data.vals1D[j]);

                    sim.Set1DValues(values);

                    clampMng.currentSimulation = sim;

                    List<int> clampIndices = data.clampIndices;
                    if (clampIndices.Count > 0)
                    {
                        for (int j = 0; j < clampIndices.Count; j++)
                        {
                            NeuronClamp clamp;
                            clamp = Instantiate(clampMng.clampPrefab, go.transform).GetComponentInChildren<NeuronClamp>();
                            clamp.AttachSimulation(sim, clampIndices[j]);
                        }
                    }
                }

                pauseBtn = FindObjectOfType<NDPauseButton>();
                pauseBtn.PauseState = data.paused;
            }
            else
                Debug.LogError("Check that loader or clampMng are not null in Menu!");
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
