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

namespace C2M2
{
    using UGX;
    namespace Simulation
    {
        using DiameterAttachment = IAttachment<DiameterData>;

        public class CellSolver2vb : HHSimulation
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
            public const int nT = 100000; //9000;  //16000;          // Number of Time steps
            public const double vstart = 55;

            private Vector U;

            // Keep track of i locally so that we know which simulation frame to send to other scripts
            private int i = -1;

            private NeuronCell myCell;
            // NeuronCellSimulation handles reading the UGX file
            protected override void SetNeuronCell(Grid grid)
            {
                myCell = new NeuronCell(grid);
                U = Vector.Build.Dense(myCell.vertCount);
                U.SetSubVector(0, myCell.vertCount, initialConditions(myCell.vertCount));
            }

            protected override double[] Get1DValues()
            {
                double[] curVals = null;
                if (i > -1) {
                    Vector curTimeSlice = U.SubVector(0, myCell.vertCount);
                    curVals = curTimeSlice.ToArray();
                }
                return curVals;
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
                //Set solving parameters standard run set endTime = 2 and nT = 1000;
                double endTime = 25; //25;//32;    // End time value
                //int nT = 9000; //9000;  //16000;          // Number of Time steps

                double k = endTime / (double)nT; //Time step size
                                                 //double h = 0.008; //myCell.edgeLengths.Average()*1e4; //Spatial Step Size
        
                //double h = myCell.edgeLengths.Average() * 1e4;

                //Debug.Log("h = " + h);
                //Debug.Log("Ave Edge Length = " + myCell.edgeLengths.Average());

                //This is where I begin using MathNumerics Library
                //Make grid function as a matrix for now! and then I initialize the voltage at one
                //location. Note: the row index will correspond to the vertex number! (hopefully)
                //U = Vector.Build.Dense(myCell.vertCount);
                
                for(i = 0; i < nT; i++)
                {
                    Debug.Log("U[0]:" + U[0] 
                        + "\n\tU[" + (myCell.vertCount - 1) + "]:" + U[myCell.vertCount - 1]);

                    U.Add(k, U);
                }
                Debug.Log("Simulation Over.");
            }

            #region Local Functions

            //Function for initialize voltage on cell
            public static Vector initialConditions(int size)
            {
                Vector ic = Vector.Build.Dense(size);
                for (int ind = 0; ind < size; ind++)
                {
                    ic[ind] = 0;
                }

                ic[0] = 55;

                return ic;
            }
        }
        #endregion
    }
}