using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System;
using UnityEngine;

// These are the MathNet Numerics Libraries needed
// They need to dragged and dropped into the Unity assets plugins folder!
//using SparseMatrix = MathNet.Numerics.LinearAlgebra.Double.SparseMatrix;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra.Double.Solvers;

using C2M2.NeuronalDynamics.UGX;
using C2M2.Utils;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
namespace C2M2.NeuronalDynamics.Simulation
{
    public class CellSolver2FullAP_Implicit : NeuronSimulation1D
    {
        //Set cell biological paramaters
        public const double res = 10.0;
        public const double gk = 36.0;
        public const double gna = 153.0;
        public const double gl = 0.3;
        public const double ek = -2.0;
        public const double ena = 112; //70;//112.0;
        public const double el = 0.6;
        public const double cap = 0.09;
        public const double ni = 0.5, mi = 0.4, hi = 0.2;       //state probabilities

        //Simulation parameters
        //public const int nT = 40000;      // Number of Time steps
        //public const double endTime = 50;  // End time value
        public const double vstart = 55;

        //Solution vectors
        private Vector U;
        private Vector M;
        private Vector N;
        private Vector H;

        // Keep track of i locally so that we know which simulation frame to send to other scripts
        private int i = -1;

        private NeuronCell myCell;


        private Timer timer = new Timer();

        internal OrderType orderType = OrderType.Identity;

        // NeuronCellSimulation handles reading the UGX file
        protected override void SetNeuronCell(Grid grid)
        {
            grid.Type = orderType;
            myCell = new NeuronCell(grid);

            //Initialize vector with all zeros
            U = Vector.Build.Dense(myCell.vertCount);
            M = Vector.Build.Dense(myCell.vertCount);
            N = Vector.Build.Dense(myCell.vertCount);
            H = Vector.Build.Dense(myCell.vertCount);

            //Set all initial state probabilities
            //M.Add(mi, M);
            //N.Add(ni, N);
            //H.Add(hi, H);
            M[0] = mi;
            N[0] = ni;
            H[0] = hi;

            //Set the initial conditions of the solution
            U.SetSubVector(0, myCell.vertCount, initialConditions(U, myCell.boundaryID));
        }

        // Secnd simulation 1D values 
        public override double[] Get1DValues()
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
            int nT;      // Number of Time steps
            double endTime = 10;  // End time value

            double k;

            double h = myCell.edgeLengths.Average();

            h = 0.027 * h;

            k = 0.026 * h * 1.15; //1.25; //for cell2h
                                  //k = 0.02 * h; // for cell 10
            nT = (int)System.Math.Floor(endTime / k);

            Debug.Log("This cell has = " + myCell.vertCount + " nodes.");
            Debug.Log("Time step k = " + k);
            Debug.Log("Time step size = " + h);
            Debug.Log("num Time steps = " + nT);

            //Debug.Log("aver edgelength = " + myCell.edgeLengths.Average());

            double alpha = 1e0; //for testing purposes only! Usually set to 1
            double a = 1e6; // I did a global node radius scaling here for testing purposes
                            // set a to 1.5 for Cell10 and set to 1 for Cell2h

            double diffConst = (1 / (2 * res * cap)) * alpha;

            double vRad;

            double cfl = diffConst * k / h;
            Debug.Log("cfl = " + cfl);

            double[,] rhsMarray = new double[myCell.vertCount, myCell.vertCount];
            double[,] lhsMarray = new double[myCell.vertCount, myCell.vertCount];

            // reaction vector
            Vector R = Vector.Build.Dense(myCell.vertCount);

            // temporary voltage vector
            Vector tempV = Vector.Build.Dense(myCell.vertCount);

            int nghbrCount;
            int nghbrInd;

            //rhsMarray[0, 0] = 1; //Set Soma coefficient to 1
            //lhsMarray[0, 0] = 1;

            //rhsMarray[myCell.vertCount - 1, myCell.vertCount - 1] = 1;
            //lhsMarray[myCell.vertCount - 1, myCell.vertCount - 1] = 1;

            //for (int p = 1; p < myCell.vertCount - 1; p++)

            Debug.Log("A node radius = " + myCell.nodeData[10].nodeRadius);


            for (int p = 0; p < myCell.vertCount; p++)
            {
                nghbrCount = myCell.nodeData[p].neighborIDs.Count;
                //vRad = myCell.nodeData[p].nodeRadius*a;
                vRad = 1e-6 * a; //radius is in micrometers
                if (nghbrCount == 1)
                {
                    rhsMarray[p, p] = 1;
                    lhsMarray[p, p] = 1;
                }
                else
                {
                    rhsMarray[p, p] = 1 - ((double)nghbrCount) * vRad * cfl / (2 * h);
                    lhsMarray[p, p] = 1 + ((double)nghbrCount) * vRad * cfl / (2 * h);
                    for (int q = 0; q < nghbrCount; q++)
                    {
                        nghbrInd = myCell.nodeData[p].neighborIDs[q];
                        rhsMarray[p, nghbrInd] = vRad * cfl / (2 * h);
                        lhsMarray[p, nghbrInd] = -vRad * cfl / (2 * h);
                    }
                }

            }

            Matrix rhsM = Matrix.Build.DenseOfArray(rhsMarray);
            Matrix lhsM = Matrix.Build.DenseOfArray(lhsMarray);
            Matrix invlshM = lhsM.Inverse();

            //GameManager.instance.DebugLogSafe("lhsM" + lhsM);
            //GameManager.instance.DebugLogSafe("rhsM" + rhsM);

            var sw = new StreamWriter("outputVoltage.txt");
            var tw = new StreamWriter("timesteps.txt");


            Timer timer = new Timer(nT);
            try
            {
                for (i = 0; i < nT; i++)
                {

                    mutex.WaitOne();
                    // GameManager.instance.DebugLogSafe("t = " + i + "\n\tU[0]:" + U[0] + "\n\tU[" + (300) + "]:" + U[300]);

                    /////////////////////////////////
                    /// Time diffusion step
                    /////////////////////////////////
                    timer.StartTimer();

                    // Diffusion solver
                    rhsM.Multiply(U, U);
                    //U = lhsM.Solve(U); //This is the solver Ax = b waaaaaay too slow, doesn't crash but is super slow!
                    invlshM.Multiply(U, U);

                    /////////////////////////////////
                    /// End diffusion step timing
                    /// /////////////////////////////
                    timer.StopTimer(i.ToString());

                    // Save voltage from diffusion step for state probabilities
                    tempV.SetSubVector(0, myCell.vertCount, U);

                    // Reaction
                    R.SetSubVector(0, myCell.vertCount, reactF(U, N, M, H, myCell.boundaryID));
                    R.Multiply(k / cap, R);

                    // This is the solution for the voltage after the reaction is included!
                    U.Add(R, U);

                    //Now update state variables using FE on M,N,H
                    N.Add(fN(tempV, N).Multiply(k), N);
                    M.Add(fM(tempV, M).Multiply(k), M);
                    H.Add(fH(tempV, H).Multiply(k), H);

                    //U.Add(U, updateBC(myCell.vertCount,myCell.boundaryID, i));
                    //U.SetSubVector(0, myCell.vertCount, setBC(U, i, k, myCell.boundaryID));

                    //Always reset to IC conditions and boundary conditions (for now)
                    U.SetSubVector(0, myCell.vertCount, boundaryConditions(U, myCell.boundaryID));
                    U[0] = 55; // 213 is soma index in not reordered grid for cell 12_a (0 is index in reorderd mesh with DFS)

                    //File.WriteAllLines(@"C:\Users\jaros_000\Desktop\myfile.txt", U.Select(d => d.ToString()));

                    for (int j = 0; j < myCell.vertCount; j++)
                    {
                        sw.Write(U[j] + " ");

                    }
                    sw.Write("\n");
                    tw.Write((k * (double)i) + " ");
                    tw.Write("\n");

                    mutex.ReleaseMutex();
                }

            }
            catch (Exception e)
            {
                GameManager.instance.DebugLogSafe(e);
            }
            finally
            { // Export timer results regardless of if simulation is successful
                timer.ExportCSV("CellSolver2FullAP_Implicit_DiffusionStep");
            }

            GameManager.instance.DebugLogSafe("Simulation Over.");
            DateTime now = DateTime.Now;

        }
        #region Local Functions

        //Function for initialize voltage on cell
        public static Vector initialConditions(Vector V, List<int> bcIndices)
        {
            //Vector ic = Vector.Build.Dense(V.Count);
            //V.Add(-15, V);
            V.SetSubVector(0, V.Count, boundaryConditions(V, bcIndices));
            //V[0] = 55;

            return V;
        }


        public static Vector boundaryConditions(Vector V, List<int> bcIndices)
        {
            for (int ind = 0; ind < bcIndices.Count; ind++)
            {
                V[bcIndices[ind]] = 0;
            }

            //V[0] = 55;
            return V;

        }

        private static Vector reactF(Vector V, Vector NN, Vector MM, Vector HH, List<int> bcIndices)
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
            output.Multiply(-1, output);

            output.SetSubVector(0, V.Count, boundaryConditions(output, bcIndices));

            return output;
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
        #endregion
    }
}