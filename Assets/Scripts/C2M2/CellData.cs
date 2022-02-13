using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace C2M2
{
    using NeuronalDynamics.Interaction;

    /// <summary>
    /// Encapsulates the cell data that will be saved
    /// </summary>
    [System.Serializable]
    public class CellData
    {
        // Simulation state
        public double[] vals1D;
        public double[] M;
        public double[] N;
        public double[] H;

        // Neuron data
        public Vector3 pos;
        public Quaternion rotation;
        public Vector3 scale;
        public string vrnFileName;
        public Gradient gradient;
        public float globalMin;
        public float globalMax;
        public int refinementLevel;
        public double timeStep;
        public double endTime;
        public double raycastHitValue;
        public string unit;
        public float unitScaler;
        public int colorMarkerPrecision;

        // Clamp data
        public ClampData[] clamp;
    }
}
