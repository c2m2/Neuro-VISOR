using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2
{
    /// <summary>
    /// Encapsulates the cell data that will be saved
    /// </summary>
    [System.Serializable]
    public class CellData
    {
        // Neuron data
        public int simID;
        public Vector3 pos;
        public Quaternion rotation;
        public Vector3 scale;
        public string vrnFileName;
        public int gradientIndex;
        public int refinementLevel;
        public double timeStep;
        public double endTime;

        // Clamp data
        [System.Serializable]
        public struct ClampData
        {
            public int vertex1D;
            public bool live;
            public double power;
        }

        public ClampData[] clamps;

        // Simulation state
        public double[] U;
        public double[] M;
        public double[] N;
        public double[] H;

        public double[] Upre;
        public double[] Mpre;
        public double[] Npre;
        public double[] Hpre;

        // Graph data
        [System.Serializable]
        public struct Graph
        {
            public int vertex;
            public Vector3[] positions;
        }

        public Graph[] graphs;
    }
}
