using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System;
using System.Diagnostics;
using UnityEngine;

// These are the MathNet Numerics Libraries needed
// They need to dragged and dropped into the Unity assets plugins folder!
//using SparseMatrix = MathNet.Numerics.LinearAlgebra.Double.SparseMatrix;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Data.Text;

// These are for the sparse solving
using CSparse.Storage;
using CSparse.Double.Factorization;
using CSparse;

using C2M2.Utils;
using C2M2.NeuronalDynamics.UGX;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
namespace C2M2.NeuronalDynamics.Simulation
{
    public class sparse_cholesky_time_test : HHSimulation
    {
        //Simulation parameters
        [Header("Simulation Parameters")]
        [Range(0.0f, 65.0f)]
        public double vstart = 55;                        // 55 [mV]
        public double endTime = 50;                       // End time value
        [Range(0.01f, 0.50f)]
        public double h = 0.27;                           // User enters spatial step size
        [Range(0.0001f, 0.01f)]
        public double k = 0.0027;                         // User enters time step size
        public bool HK_auto = true;                       // auto choose H and K
        public bool SomaOn = true;                        // set soma to be clamped to vstart

        [Header("Print Output files")]
        public int numRuns = 0;                           // Number of Runs
        public bool printMatrices = false;                // turn on/off printing of system matrices
        public bool printCellInfo = false;                // turn on/off printing of cell info
        public bool printVolt_time = false;               // turn on/off printing of voltage and times

        //Set cell biological paramaters
        [Header("Biological Parameters")]
        [Range(0.0f, 50.0f)]
        public double res = 10.0;
        [Range(0.00f, 1.00f)]
        public double cap = 0.09;                         // [uF/cm^2]
        [Range(0.00f, 1.00f)]
        public double ni = 0.5, mi = 0.4, hi = 0.2;       //state probabilities, unitless

        // Turn On/Off Potassium
        public bool k_ONOFF = true;
        [Range(0.0f, 100.0f)]
        public double gk = 36.0;                          // [mS/cm^2]
        [Range(-120.0f, 120.0f)]
        public double ek = -2.0;                          // [mV]

        // Turn On/Off Sodium
        public bool na_ONOFF = true;
        [Range(0.0f, 200.0f)]
        public double gna = 153.0;                        // [mS/cm^2]
        [Range(-120.0f, 120.0f)]
        public double ena = 112;                          // [mV]

        // Turn On/Off Leak
        public bool leak_ONOFF = true;
        [Range(0.0f, 10.0f)]
        public double gl = 0.3;                           // [mS/cm^2]
        [Range(-120.0f, 120.0f)]
        public double el = 0.6;                           // [mV]


        //Solution vectors
        // all in units [mV]
        private Vector U;
        private Vector M;
        private Vector N;
        private Vector H;

        // Keep track of i locally so that we know which simulation frame to send to other scripts
        private int i = -1;

        private NeuronCell myCell;

        //private TimeUtilities.Timer timer = new TimeUtilities.Timer();

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
            M[0] = mi;
            N[0] = ni;
            H[0] = hi;

            //Set the initial conditions of the solution
            U.SetSubVector(0, myCell.vertCount, initialConditions(U, myCell.boundaryID));
            if (SomaOn) { U.SetSubVector(0, myCell.vertCount, setSoma(U, myCell.somaID, vstart)); }
        }

        // Send simulation 1D values 
        protected override double[] Get1DValues()
        {
            double[] curVals = null;
            try
            {
                mutex.WaitOne();

                if (i > -1)
                {
                    Vector curTimeSlice = U.SubVector(0, myCell.vertCount);
                    curTimeSlice.Multiply(1, curTimeSlice);
                    curVals = curTimeSlice.ToArray();
                }

                mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                GameManager.instance.DebugLogSafe(e);
            }
            return curVals;
        }

        // Receive new simulation 1D index/value pairings
        protected override void Set1DValues(Tuple<int, double>[] newValues)
        {
            try
            {
                mutex.WaitOne();
                foreach (Tuple<int, double> newVal in newValues)
                {
                    int j = newVal.Item1;
                    double val = newVal.Item2 * vstart * 0.25;
                    U[j] += val;
                }
                mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                GameManager.instance.DebugLogSafe(e);
            }
        }

        protected override void Solve()
        {
            for (int kSim = 0; kSim <= numRuns; kSim++)
            {
                int nT;                 // Number of Time steps
                List<bool> channels = new List<bool> { false, false, false };       // For adding/removing channels

                // TODO: NEED TO DO THIS BETTER
                if (HK_auto)
                {
                    h = 0.1 * myCell.edgeLengths.Average();
                    if (h > 0.29) { h = 0.29; }
                    if (h <= 1) { k = h / 100; }          // For cell 13_0ref
                    if (h <= 0.15) { k = h / 50; }        // For cell 13_1ref
                    if (h <= 0.08) { k = h / 25; }        // For cell 13_2ref
                    if (h <= 0.04) { k = h / 12.5; }      // For cell 13_3ref
                    if (h <= 0.02) { k = h / 5.5; }       // For cell 13_4ref
                }

                // for timing data
                DateTime now = DateTime.Now;

                // setup up paths for writing output
                string strPath = Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
                string subPath = strPath + @"\VR_Simulations";
                bool exists = System.IO.Directory.Exists(subPath);

                // check if directory exists
                if (!exists) { System.IO.Directory.CreateDirectory(subPath); }

                // set the path for writing
                strPath = subPath;
                DirectoryInfo di = Directory.CreateDirectory(strPath + @"\SimulationRun" + "_" + kSim);
                strPath = strPath + @"\SimulationRun" + "_" + kSim;

                // Number of time steps
                nT = (int)System.Math.Floor(endTime / k);

                // set some constants for the HINES matrix
                double diffConst = (1 / (2 * res * cap));
                double cfl = diffConst * k / h;

                //TODO: JAMES GET RID OF ARRAYS GO STRAIGHT FOR SPARSE ALLOCATION, THIS IS SILLY! --> QUEISSER/SEIBOLD!
                // pre-allocate 2d arrays, not jagged, maybe incorporate jagged arrays later? --> NO!
                double[,] rhsMarray = new double[myCell.vertCount, myCell.vertCount];
                double[,] lhsMarray = new double[myCell.vertCount, myCell.vertCount];

                // reaction vector
                Vector R = Vector.Build.Dense(myCell.vertCount);
                List<double> reactConst = new List<double> { gk, gna, gl, ek, ena, el };

                // temporary voltage vector
                Vector tempV = Vector.Build.Dense(myCell.vertCount);

                // TODO: turn make stencils function to use sparse allocation!! --> QUEISSER/SEIBOLD!
                // Make the system stencil matrices for diffusion solve
                List<double[,]> stencils = makeStencils(myCell, h, k, diffConst);
                rhsMarray = stencils[0];
                lhsMarray = stencils[1];

                // Write rhsM and lhsM to file
                if (printMatrices)
                {
                    printMatrix(rhsMarray, lhsMarray, strPath, kSim);
                }

                // for allocation of the sparse matrices
                CompressedColumnStorage<double> r_csc = null;
                CompressedColumnStorage<double> l_csc = null;

                // makes the rhs, lhs in sparse format
                r_csc = Converter.ToCompressedColumnStorage(Converter.FromDenseArray(rhsMarray));
                l_csc = Converter.ToCompressedColumnStorage(Converter.FromDenseArray(lhsMarray));

                // for solving Ax = b problem
                double[] b = new double[myCell.vertCount];

                // TODO: UNDERSTAND WHAT THIS IS DOING! --> QUEISSER/SEIBOLD!
                // Apply column ordering to A to reduce fill-in.
                var order = ColumnOrdering.MinimumDegreeAtPlusA;

                // TODO: GO INTO Source code and print the L matrix
                //       Time the reordering steps? --> Ask Queisser again about this!
                // Create Cholesky factorization setup
                Timer timer = new Timer(1);
                timer.StartTimer();
                var chl = SparseCholesky.Create(l_csc, order);
                timer.StopTimer("MatSetup t= ");
                timer.ExportCSV("matrixSetup");

                // Print cell info to a text file
                if (printCellInfo)
                {
                    printCell(myCell, h, k, nT, endTime, cfl, strPath, kSim);
                }

                // For printing voltage data and time steps
                var sw = new StreamWriter(strPath + @"\outputVoltage_" + kSim + ".txt", true);
                var tw = new StreamWriter(strPath + @"\timesteps_" + kSim + ".txt", true);

                timer = new Timer(nT);
                try
                {
                    for (i = 0; i < nT; i++)
                    {
                        if (SomaOn) { U.SetSubVector(0, myCell.vertCount, setSoma(U, myCell.somaID, vstart)); }

                        mutex.WaitOne();

                        r_csc.Multiply(U.ToArray(), b);         // Peform b = rhs * U_curr  
                                                                // Diffusion solver
                        timer.StartTimer();
                        chl.Solve(b, b);
                        timer.StopTimer(i.ToString());

                        // Set U_next = b
                        U.SetSubVector(0, myCell.vertCount, Vector.Build.DenseOfArray(b));

                        // Save voltage from diffusion step for state probabilities
                        tempV.SetSubVector(0, myCell.vertCount, U);

                        // Reaction
                        channels[0] = na_ONOFF;
                        channels[1] = k_ONOFF;
                        channels[2] = leak_ONOFF;
                        R.SetSubVector(0, myCell.vertCount, reactF(reactConst, U, N, M, H, channels, myCell.boundaryID));
                        R.Multiply(k / cap, R);

                        // This is the solution for the voltage after the reaction is included!
                        U.Add(R, U);

                        //Now update state variables using FE on M,N,H
                        N.Add(fN(tempV, N).Multiply(k), N);
                        M.Add(fM(tempV, M).Multiply(k), M);
                        H.Add(fH(tempV, H).Multiply(k), H);

                        //Always reset to IC conditions and boundary conditions (for now)
                        U.SetSubVector(0, myCell.vertCount, boundaryConditions(U, myCell.boundaryID));
                        if (SomaOn) { U.SetSubVector(0, myCell.vertCount, setSoma(U, myCell.somaID, vstart)); }

                        if (printVolt_time)
                        {
                            for (int j = 0; j < myCell.vertCount; j++)
                            {
                                sw.Write(U[j] + " ");

                            }
                            sw.Write("\n");
                            tw.Write((k * (double)i) + " ");
                            tw.Write("\n");
                        }

                        mutex.ReleaseMutex();
                    }

                    sw.Close();
                    tw.Close();
                }
                catch (Exception e)
                {
                    GameManager.instance.DebugLogSafe(e);
                }
                finally
                {
                    timer.ExportCSV("diffusionTimes");
                    sw.Close();
                    tw.Close();
                }
                GameManager.instance.DebugLogSafe("Simulation Over.");
            }
        }

        #region Local Functions

        // This is the thomas_algorithm
        // Input: D = diagonal of LHS
        //        u = upper diagonal of LHS
        //        b = this is the vector in the equation LHS*x = b
        // Output: vector x which is solution to LHS*x = b
        public static Vector thomas_algorithm(Vector D, Vector u, Vector b)
        {
            int size = b.Count;
            double f;

            // Loop in thomas algorithm
            for (int j = size - 1; j-- > 1;)
            {
                f = u[j - 1] / D[j];
                D[j - 1] = D[j - 1] - f * u[j - 1];
                b[j - 1] = b[j - 1] - f * b[j];
            }
            b[0] = b[0] / D[0];

            //typo in paper
            for (int j = 1; j < size - 1; j++)
            {
                b[j] = (b[j] - u[j] * b[j - 1]) / D[j];
            }

            return b;
        }

        // This prints the general cell and simulation information
        // average edge lengths, spatial step size, time step size, number of time steps
        // the cfl number, time to setup cholesky solver, soma indices
        public void printCell(NeuronCell myCell, double h, double k, double nT, double endTime, double cfl, string strPath, int kSim)
        {
            var cellinfo = new StreamWriter(strPath + @"\cellinfo_" + kSim + ".txt", true);
            var neigbors = new StreamWriter(strPath + @"\nodeNeigbors_" + kSim + ".txt", true);
            cellinfo.Write("aver edgelength = " + myCell.edgeLengths.Average() + "\n");
            cellinfo.Write("max edgelength = " + myCell.edgeLengths.Max() + "\n");
            cellinfo.Write("min edgelength = " + myCell.edgeLengths.Min() + "\n");
            cellinfo.Write("This cell has = " + myCell.vertCount + " nodes." + "\n");
            cellinfo.Write("Time step k = " + k + "\n");
            cellinfo.Write("Spatial step size = " + h + "\n");
            cellinfo.Write("Number of Time steps = " + nT + "\n");
            cellinfo.Write("Soma Indices = " + String.Join(", ", myCell.somaID) + "\n");
            cellinfo.Write("cfl = " + cfl + "\n");
            cellinfo.Close();

            // Print the node neighbors to output file
            for (int p = 0; p < myCell.vertCount; p++)
            {
                for (int q = 0; q < myCell.nodeData[p].neighborIDs.Count; q++)
                {
                    neigbors.Write(myCell.nodeData[p].neighborIDs[q] + ", ");
                }
                neigbors.Write("\n");
            }

            neigbors.Close();
        }

        // TODO: Do this to print sparse pattern
        // this prints the right hand side and left hand side system stencil matrices
        public void printMatrix(double[,] rhsMarray, double[,] lhsMarray, string strPath, int kSim)
        {
            var rhsMatrix = new StreamWriter(strPath + @"\rhsMatrix_" + kSim + ".txt", true);
            var lhsMatrix = new StreamWriter(strPath + @"\lhsMatrix_" + kSim + ".txt", true);

            for (int q = 0; q < myCell.vertCount; q++)
            {
                for (int j = 0; j < myCell.vertCount; j++)
                {
                    rhsMatrix.Write(rhsMarray[q, j] + " ");
                    lhsMatrix.Write(lhsMarray[q, j] + " ");
                }
                rhsMatrix.Write("\n");
                lhsMatrix.Write("\n");
            }
            rhsMatrix.Close();
            lhsMatrix.Close();
        }

        // TODO: DO THIS IN SPARSE FORMAT, NO MORE ARRAYS!! --> QUEISSER/SEIBOLD!
        // This is for constructing the lhs and rhs of system matrix
        // This will construct a HINES matrix (symmetric), it should be tridiagonal with some off
        // diagonal entries corresponding to a branch location in the neuron graph
        public static List<double[,]> makeStencils(NeuronCell myCell, double h, double k, double diffConst)
        {
            List<double[,]> stencils = new List<double[,]>();

            // preallocate arrays
            double[,] rhsMarray = new double[myCell.vertCount, myCell.vertCount];
            double[,] lhsMarray = new double[myCell.vertCount, myCell.vertCount];

            // for keeping track of the neighbors of a node
            int nghbrCount;
            int nghbrInd;

            // need cfl coefficient
            double cfl = diffConst * k / h;
            double vRad = 1;    // need to use radius data attached to geometry, TODO: move inside the loop and access for each node

            for (int p = 0; p < myCell.vertCount; p++)
            {
                nghbrCount = myCell.nodeData[p].neighborIDs.Count;

                // set main diagonal entries
                rhsMarray[p, p] = 1 - ((double)nghbrCount) * vRad * cfl / (2 * h);
                lhsMarray[p, p] = 1 + ((double)nghbrCount) * vRad * cfl / (2 * h);

                // this inner loop is for setting the off diagonal entries which correspond
                // to the neighbors of each node in the branch structure
                for (int q = 0; q < nghbrCount; q++)
                {
                    nghbrInd = myCell.nodeData[p].neighborIDs[q];

                    // for off diagonal entries
                    rhsMarray[p, nghbrInd] = vRad * cfl / (2 * h);
                    rhsMarray[nghbrInd, p] = vRad * cfl / (2 * h);

                    // for off diagonal entries
                    lhsMarray[p, nghbrInd] = -vRad * cfl / (2 * h);
                    lhsMarray[nghbrInd, p] = -vRad * cfl / (2 * h);
                }

            }

            // set output
            stencils.Add(rhsMarray);
            stencils.Add(lhsMarray);

            return stencils;
        }

        // this sets the soma voltage for testing
        // the soma maybe a set of indices, not just one.
        public static Vector setSoma(Vector V, List<int> somaID, double voltage)
        {

            for (int ind = 0; ind < somaID.Count; ind++)
            {
                V[somaID[ind]] = voltage;
            }

            return V;
        }

        //Function for initializes voltage on cell
        public static Vector initialConditions(Vector V, List<int> bcIndices)
        {
            // only set boundary conditions
            V.SetSubVector(0, V.Count, boundaryConditions(V, bcIndices));

            return V;
        }

        // initialize the ends of dendrites to be clamped at 0 mV
        public static Vector boundaryConditions(Vector V, List<int> bcIndices)
        {
            for (int ind = 0; ind < bcIndices.Count; ind++)
            {
                V[bcIndices[ind]] = 0;
            }

            return V;

        }

        // this is the reaction term of the HH equation
        private static Vector reactF(List<double> reactConst, Vector V, Vector NN, Vector MM, Vector HH, List<bool> channels, List<int> bcIndices)
        {
            Vector output = Vector.Build.Dense(V.Count);
            Vector prod = Vector.Build.Dense(V.Count);
            double ek, ena, el, gk, gna, gl;

            // set constants for voltage
            gk = reactConst[0];
            gna = reactConst[1];
            gl = reactConst[2];

            // set constants for conductances
            ek = reactConst[3];
            ena = reactConst[4];
            el = reactConst[5];

            if (channels[0])
            {
                // Add current due to potassium
                prod = NN.PointwisePower(4);
                prod = (V - ek).PointwiseMultiply(prod);

                output = gk * prod;
            }

            if (channels[1])
            {
                // Add current due to sodium
                prod = MM.PointwisePower(3);
                prod = HH.PointwiseMultiply(prod);
                prod = (V - ena).PointwiseMultiply(prod);
                output = output + gna * prod;
            }

            if (channels[2])
            {
                // Add leak current
                output = output + gl * (V - el);
            }

            // Return the negative of the total
            output.Multiply(-1, output);

            output.SetSubVector(0, V.Count, boundaryConditions(output, bcIndices));

            return output;
        }

        // The following functions are for the state variable ODEs on M,N,H
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