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
    using Utils;
    using Interaction;
    namespace Simulation
    {
        using DiameterAttachment = IAttachment<DiameterData>;

        public class CellSolver2DiffusionReaction : HHSimulation
        {
            //Set cell biological paramaters
            public const double res = 10.0;
            public const double gk = 36.0;
            public const double gna = 180.0;
            public const double gl = 0.3;
            public const double ek = -2.0;
            public const double ena = 150; //70;//112.0;
            public const double el = 0.6;
            public const double cap = 0.09;
            public const double ni = 0.5, mi = 0.4, hi = 0.2;       //state probabilities

            //Simulation parameters
            public const int nT = 40000;      // Number of Time steps
            public const double endTime = 50;  // End time value
            public const double vstart = 55;

            //Solution vectors
            private Vector U;
            private Vector M;
            private Vector N;
            private Vector H;

            // Keep track of i locally so that we know which simulation frame to send to other scripts
            private int i = -1;

            private NeuronCell myCell;

            // NeuronCellSimulation handles reading the UGX file
            protected override void SetNeuronCell(Grid grid)
            {
                myCell = new NeuronCell(grid);

                //Initialize vector with all zeros
                U = Vector.Build.Dense(myCell.vertCount);
                M = Vector.Build.Dense(myCell.vertCount);
                N = Vector.Build.Dense(myCell.vertCount);
                H = Vector.Build.Dense(myCell.vertCount);

                //Set all initial state probabilities
                M.Add(mi, M);
                N.Add(ni, N);
                H.Add(hi, H);

                //Set the initial conditions of the solution
                U.SetSubVector(0, myCell.vertCount, initialConditions(U, myCell.boundaryID));
            }

            // Secnd simulation 1D values 
            protected override double[] Get1DValues()
            {
                mutex.WaitOne();
                double[] curVals = null;
                if (i > -1)
                {
                    Vector curTimeSlice = U.SubVector(0, myCell.vertCount);
                    curTimeSlice.Multiply(1, curTimeSlice);

                    curVals = curTimeSlice.ToArray();
                }
                mutex.ReleaseMutex();
                return curVals;
            }

            // Receive new simulation 1D index/value pairings
            protected override void Set1DValues(Tuple<int, double>[] newValues)
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
                Timer timer = new Timer(nT + 1);
                timer.StartTimer();

                // Access the color manager and have it color the surface based on our preset max/min value
                Gradient32LUT colorManager = GetComponent<Gradient32LUT>();
                if (colorManager != null)
                {
                    colorManager.extremaMethod = Gradient32LUT.ExtremaMethod.GlobalExtrema;
                    colorManager.globalMax = 65;
                    colorManager.globalMin = -15;
                }

                // Computer simulation stepping parameters
                double k = endTime / (double)nT; //Time step size
                //double h = myCell.edgeLengths.Average();

                //double k = 0.001;
                //double h = k/0.9;
                double h = System.Math.Sqrt(2 * k) + 0.09;
                double alpha = 1e2; //for testing purposes only! Must set to 1!
                double a = 1e-5;
                double diffConst =a/(2*res*cap)*alpha;

                double cfl = diffConst * k / h;
                Debug.Log("cfl = " + cfl);

                // Stencil matrix for diffusion solve
                Matrix rhsM = Matrix.Build.Dense(myCell.vertCount, myCell.vertCount);

                // reaction vector
                Vector R = Vector.Build.Dense(myCell.vertCount);

                // temporary voltage vector
                Vector tempV = Vector.Build.Dense(myCell.vertCount);

                int nghbrCount;
                int nghbrInd;

                timer.StopTimer("Init");
                Debug.Log("Soma node neighbors" + myCell.nodeData[0].neighborIDs.Count);
                Debug.Log("Last node neighbors" + myCell.nodeData[631].neighborIDs.Count);


                //for(int p =1; p<myCell.vertCount-1; p++)
                //rhsM[0, 0] = 1; //Set Soma coefficient to 1
                for (int p = 0; p < myCell.vertCount; p++)
                {
                    nghbrCount = myCell.nodeData[p].neighborIDs.Count;
                    //if (nghbrCount == 1)
                    //{
                    //    rhsM[p, p] = 1;
                    //}
                    //else
                    //{
                        rhsM[p, p] = 1 - nghbrCount * cfl / h;
                        for (int q = 0; q < nghbrCount; q++)
                        {
                            nghbrInd = myCell.nodeData[p].neighborIDs[q];
                            rhsM[p, nghbrInd] = cfl / h;
                        }
                    //}

                }
               // Debug.Log(rhsM);

                //this is for checking the boundary indices
                /*for (int mm = 0; mm < myCell.boundaryID.Count; mm++)
                {
                    Debug.Log((myCell.boundaryID[mm]));
                }*/

                int tCount = 0;

                try
                {
                    for (i = 0; i < nT; i++)
                    {
                        mutex.WaitOne();
                        Debug.Log("U[0]:" + U[0] + "\n\tU[" + (300) + "]:" + U[300]);
                        timer.StartTimer();

                        // Diffusion solver
                        rhsM.Multiply(U, U);

                        // Save voltage from diffusion step for state probabilities
                        tempV.SetSubVector(0, myCell.vertCount, U);

                        // Reaction
                        R.SetSubVector(0, myCell.vertCount, reactF(U, N, M, H));
                        R.Multiply(k / cap, R);

                        // This is the solution for the voltage after the reaction is included!
                        U.Add(R, U);
                        U[0] = 55;

                        //Now update state variables using FE on M,N,H
                        N.Add(fN(tempV, N).Multiply(k), N);
                        M.Add(fM(tempV, M).Multiply(k), M);
                        H.Add(fH(tempV, H).Multiply(k), H);

                        //U.Add(U, updateBC(myCell.vertCount,myCell.boundaryID, i));
                        //U.SetSubVector(0, myCell.vertCount, setBC(U, i, k, myCell.boundaryID));

                        //Always reset to IC conditions and boundary conditions (for now)
                        //U.SetSubVector(0, myCell.vertCount, initialConditions(U,myCell.boundaryID));
                        tCount++;

                        mutex.ReleaseMutex();
                        timer.StopTimer("[cell " + i + "]:");
                    }
                }catch(Exception e)
                {
                    Debug.LogError(e);
                }
                Debug.Log("Simulation Over.");
                Debug.Log("Time results:" + timer.ToString());
            }

            #region Local Functions

            //Function for initialize voltage on cell
            public static Vector initialConditions(Vector V, List<int> bcIndices)
            {
                //Vector ic = Vector.Build.Dense(V.Count);
                V.SetSubVector(0, V.Count, boundaryConditions(V, bcIndices));
                V[0] = 55;

                return V;
            }


            public static Vector boundaryConditions(Vector V, List<int> bcIndices)
            {
                for (int ind = 0; ind < bcIndices.Count; ind++)
                {
                    V[bcIndices[ind]] = 0;
                }

                return V;

            }
            public static Vector setBC(Vector V, int tInd, double dt, List<int> bcIndices)
            {

                //uncomment this for loop to implement oscillating b.c.
                //for(int p=0; p<bcIndices.Count;p++)
                //{
                //    V[bcIndices[p]] = 0 * System.Math.Sin(tInd * dt * 0.09 * 3.1415962);
                //}

                return V;
            }

            public static Vector updateBC(int size, List<int> bcIndices, int tInd)
            {
                Vector bc = Vector.Build.Dense(size);

                return bc;
            }


            private static Vector reactF(Vector V, Vector NN, Vector MM, Vector HH)
            {
                Vector output = Vector.Build.Dense(V.Count);
                Vector prod = Vector.Build.Dense(V.Count);

                // Add current due to potassium
                prod = NN.PointwisePower(4);
                prod = (V - ek).PointwiseMultiply(prod);
                output = gk * prod;

                // Add current due to sodium
                prod = MM.PointwisePower(3);
                prod = HH.PointwiseMultiply(prod);
                prod = (V - ena).PointwiseMultiply(prod);
                output = output + gna * prod;

                // Add leak current
                output = output + gl * (V - el);

                // Return the negative of the total
                return -1 * output;
            }

            private static Vector fN(Vector V, Vector N)
            {
                Vector output = Vector.Build.Dense(V.Count);

                output = an(V).PointwiseMultiply(1 - N) - bn(V).PointwiseMultiply(N);

                return output;

            }

            private static Vector fM(Vector V, Vector M)
            {
                Vector output = Vector.Build.Dense(V.Count);

                output = am(V).PointwiseMultiply(1 - M) - bm(V).PointwiseMultiply(M);

                return output;

            }

            private static Vector fH(Vector V, Vector H)
            {
                Vector output = Vector.Build.Dense(V.Count);

                output = ah(V).PointwiseMultiply(1 - H) - bh(V).PointwiseMultiply(H);

                return output;

            }

            //The following functions are for the state variable ODEs on M,N,H
            private static Vector an(Vector V)
            {
                Vector output = Vector.Build.Dense(V.Count);
                Vector temp = 10 - V;

                output = 0.01 * temp.PointwiseDivide((temp / 10).PointwiseExp() - 1);
                return output;
            }

            private static Vector bn(Vector V)
            {
                Vector output = Vector.Build.Dense(V.Count);

                output = 0.125 * (-1 * V / 80).PointwiseExp();
                return output;
            }

            private static Vector am(Vector V)
            {
                Vector output = Vector.Build.Dense(V.Count);
                Vector temp = 25 - V;

                output = 0.1 * temp.PointwiseDivide((temp / 10).PointwiseExp() - 1);
                return output;
            }

            private static Vector bm(Vector V)
            {
                Vector output = Vector.Build.Dense(V.Count);

                output = 4 * (-1 * V / 18).PointwiseExp();
                return output;
            }

            private static Vector ah(Vector V)
            {
                Vector output = Vector.Build.Dense(V.Count);

                output = 0.07 * (-1 * V / 20).PointwiseExp();
                return output;
            }

            private static Vector bh(Vector V)
            {
                Vector output = Vector.Build.Dense(V.Count);
                Vector temp = 30 - V;

                output = 1 / ((temp / 10).PointwiseExp() + 1);
                return output;
            }
        }
        #endregion
    }
}