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
    using DiameterAttachment = IAttachment<DiameterData>;

    public class CellSolver2SimpleDiffusion : NeuronSimulation1D
    {
        //Set cell biological paramaters
        public const double res = 10.0;
        public const double gk = 36.0;
        public const double gna = 120.0;
        public const double gl = 0.3;
        public const double ek = -12.0;
        public const double ena = 220; //70;//112.0;
        public const double el = 0.6;
        public const double cap = 0.09;
        public const double ni = 0.5, mi = 0.4, hi = 0.2;       //state probabilities

        //Simulation parameters
        public const int nT = 40000;      // Number of Time steps
        public const double endTime = 25;  // End time value
        public const double vstart = 55;

        private Vector U;

        // Keep track of i locally so that we know which simulation frame to send to other scripts
        private int i = -1;

        // Secnd simulation 1D values 
        public override double[] Get1DValues()
        {
            mutex.WaitOne();
            double[] curVals = null;
            if (i > -1)
            {
                Vector curTimeSlice = U.SubVector(0, NeuronCell.vertCount);
                curTimeSlice.Multiply(1, curTimeSlice);

                curVals = curTimeSlice.ToArray();
            }
            mutex.ReleaseMutex();
            return curVals;
        }

        // Receive new simulation 1D index/value pairings
        public override void Set1DValues(Tuple<int, double>[] newValues)
        {
            mutex.WaitOne();
            foreach (Tuple<int, double> newVal in newValues)
            {
                int j = newVal.Item1;
                double val = newVal.Item2 * vstart;
                U[j] += val;
            }

            mutex.ReleaseMutex();
        }

        protected override void Solve()
        {
            InitializeNeuronCell();
            // Computer simulation stepping parameters
            double k = endTime / (double)nT; //Time step size
            //double h = myCell.edgeLengths.Average();

            //double k = 0.001;
            //double h = k/0.9;
            double h = System.Math.Sqrt(2 * k)+0.09;
            double cap = 10e-2;

            double cfl =cap*k / h;
            Debug.Log("cfl = " + cfl);               
            Matrix rhsM = Matrix.Build.Dense(NeuronCell.vertCount,NeuronCell.vertCount);

            int nghbrCount;
            int nghbrInd;

            Debug.Log("Soma node neighbors" + NeuronCell.nodeData[0].neighborIDs.Count);
            Debug.Log("Last node neighbors" + NeuronCell.nodeData[631].neighborIDs.Count);


            //for(int p =1; p<myCell.vertCount-1; p++)
            //rhsM[0, 0] = 1; //Set Soma coefficient to 1
            for (int p = 0; p < NeuronCell.vertCount; p++)
            {
                nghbrCount = NeuronCell.nodeData[p].neighborIDs.Count;
                if (nghbrCount == 1)
                {
                    rhsM[p, p] = 1;
                }
                else
                {
                    rhsM[p, p] = 1 - nghbrCount * cfl / h;
                    for(int q =0; q<nghbrCount; q++)
                    {
                        nghbrInd = NeuronCell.nodeData[p].neighborIDs[q];
                        rhsM[p, nghbrInd] = cfl / h;
                    }
                }
                //rhsM[p, p] = -2;
                //rhsM[p, p - 1] = 1;
                //rhsM[p, p + 1] = 1;
            }
            //rhsM[0, 1] = 1;
            //rhsM[0, 0] = -2;
            //rhsM[myCell.vertCount - 1, myCell.vertCount - 2] = 1;
            //rhsM[myCell.vertCount - 1, myCell.vertCount - 1] = -2;

            //rhsM.Multiply(cfl/h, rhsM);

            Debug.Log(rhsM);

            //Identity Matrix
            //Matrix eye = Matrix.Build.DenseIdentity(myCell.vertCount);

            //rhsM.Add(eye, rhsM);
            //Debug.Log(rhsM);

            //this is for checking the boundary indices
            for (int mm = 0; mm < NeuronCell.boundaryID.Count; mm++)
            {
                Debug.Log((NeuronCell.boundaryID[mm]));
            }

            int tCount = 0;

            for (i = 0; i < nT; i++)
            {
                mutex.WaitOne();
                Debug.Log("Time counter = " + tCount);
                //Debug.Log("Elapsed Time = " + ((double)i) * k);
                Debug.Log("U[0]:" + U[0] + "\n\tU[" + (300) + "]:" + U[300]);

                //This is the solver Vnxt = Vcur + k*f(Vcur)
                //Where f(Vcur)=2.5
                //U.Add(2.5 * k, U);

                rhsM.Multiply(U, U);
                //U.Add(U, updateBC(myCell.vertCount,myCell.boundaryID, i));
                U.SetSubVector(0, NeuronCell.vertCount, setBC(U, i,k, NeuronCell.boundaryID));
                //U.SetSubVector(0, myCell.vertCount, eye * U);
                tCount++;
                //U.SetSubVector(0, myCell.vertCount, eye.Multiply(U));

                mutex.ReleaseMutex();
            }
            Debug.Log("Simulation Over.");
        }

        #region Local Functions
        private void InitializeNeuronCell()
        {
            //Initialize vector with all zeros
            U = Vector.Build.Dense(NeuronCell.vertCount);

            //Set the initial conditions of the solution
            U.SetSubVector(0, NeuronCell.vertCount, initialConditions(U, NeuronCell.boundaryID));
        }
        //Function for initialize voltage on cell
        public static Vector initialConditions(Vector V, List<int> bcIndices)
        {
            Vector ic = Vector.Build.Dense(V.Count);
            ic.SetSubVector(0, V.Count, boundaryConditions(ic,bcIndices));
            ic[0] = 0;

            return ic;
        }

        public static Vector boundaryConditions(Vector V, List<int> bcIndices)
        {
            for (int ind = 0; ind < bcIndices.Count; ind++)
            {
                V[bcIndices[ind]] = 55;
                    
            }

            return V;

        }
        public static Vector setBC(Vector V, int tInd, double dt, List<int> bcIndices)
        {

            //uncomment this for loop to implement oscillating b.c.
            //for(int p=0; p<bcIndices.Count;p++)
            //{
                //V[bcIndices[p]] = 55 * System.Math.Sin(tInd * dt * 0.09 * 3.1415962);
                    
            //}

            return V;
        }

        public static Vector updateBC(int size,List<int> bcIndices, int tInd)
        {
            Vector bc = Vector.Build.Dense(size);

            return bc;
        }
    #endregion
    }
}