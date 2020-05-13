using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System;
using UnityEngine;

// These are the MathNet Numerics Libraries needed
// They need to dragged and dropped into the Unity assets plugins folder!
using SparseMatrix = MathNet.Numerics.LinearAlgebra.Double.SparseMatrix;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using Double = MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Data.Text;
using solvers = MathNet.Numerics.LinearAlgebra.Double.Solvers;

using C2M2.NeuronalDynamics.UGX;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
namespace C2M2.NeuronalDynamics.Simulation
{
    public class SimpleSolver_constDerivative : HHSimulation
    {
        //Set cell biological constants
        public const double res = 10.0;
        public const double gk = 36.0;
        public const double gna = 120.0;
        public const double gl = 0.3;
        public const double ek = -12.0;
        public const double ena = 220; //70;//112.0;
        public const double el = 0.6;
        public const double cap = 0.09;
        public const double ni = 0.5, mi = 0.4, hi = 0.2;       //state probabilities

        public const double vstart = 55;

        private Vector U;
        // NeuronCellSimulation handles reading the UGX file
        private NeuronCell myCell;
        protected override void SetNeuronCell(Grid grid)
        {
            myCell = new NeuronCell(grid);
            U = Vector.Build.Dense(myCell.vertCount);

            U.SetSubVector(0, myCell.vertCount, ic(myCell.vertCount));
            //U = Vector.Build.Dense(myCell.vertCount);
        }
        // Keep track of i locally so that we know which simulation frame to send to other scripts
        private int i = -1;

        protected override double[] Get1DValues()
        {
            //return (U != null && i > -1) ? U.SubMatrix(0, U.RowCount, i, 1).ToColumnMajorArray() : null;
            Debug.Log("Here is state: " + U.ToString());
            //i = i + 1;

            return (U != null) ? U.SubVector(0, myCell.vertCount).ToArray() : null;
        }
        // Receive new simulation 1D index/value pairings
        protected override void Set1DValues(Tuple<int, double>[] newValues)
        {
            foreach (Tuple<int, double> newVal in newValues)
            {
                int j = newVal.Item1;
                double val = newVal.Item2 * vstart;
                U[j] += val;
            }
        }

        protected override void Solve()
        {
            int numVert = myCell.vertCount;

            int nT = 9000;
            double endTime = 25;

            double k = endTime / nT;

            // I am beginning to wonder if the for loop is even necessary
            // Maybe do a solve on every timestep call to solve and increment i  by 1

            //This for loop is for solving the simple ode dV/dt = 1;
            //for (i = 0; i < nT; i++)
            //{
            Debug.Log("Hello" + U.ToString());
            // Forward Euler Solve for Vnext = Vcurr+ k*f(Vcurr)
            // Here f(V) = 1;
            U.Add(k, U);

            i = i + 1;
            //}
        }
        #region Local Functions
        private static Vector ic(int size)
        {
            double[] outV = new double[size];
            for (int j = 0; j < size; j++)
            {
                outV[j] = 0.002;
            }

            return Vector.Build.Dense(outV);
        }
        #endregion
    }
}