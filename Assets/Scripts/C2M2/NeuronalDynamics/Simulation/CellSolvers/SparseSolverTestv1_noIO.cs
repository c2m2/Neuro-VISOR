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
    public class SparseSolverTestv1_noIO : NDSimulation
    {
        //Simulation parameters
        
        private double vstart = 55;                         // 55 [mV]
        [Header("Simulation Parameters")]
        public double endTime = 100;                      // End time value [msec]
        private double h;                                   // User enters spatial step size
        private double k;                                   // User enters time step size
        private bool HK_auto = true;                        // auto choose H and K
        public bool SomaOn = true;                         // set soma to be clamped to vstart
        private int numRuns = 0;                            // Number of Runs
        private bool randomPermute = false;                 // Randomly permute the order of the solution entries

        //Set cell biological paramaters
        private double res = 0.3;                          // Ohm.cm
        private double cap = 0.3;                          // [uF/cm^2]
        private double ni = 0.317677, mi = 0.0529325, hi = 0.596121;       //state probabilities, unitless

        // Turn On/Off Potassium
        private bool k_ONOFF = true;
        private double gk = 36.0;                           // [m.mho/cm^2]
        private double ek = -2.0;                           // [mV]
        // Turn On/Off Sodium
        private bool na_ONOFF = true;
        private double gna = 153.0;                         // [m.mho/cm^2]
        private double ena = 120.0;                          // [mV]
        // Turn On/Off Leak
        private bool leak_ONOFF = true;
        private double gl = 0.3;                            // [m.mho/cm^2]
        private double el = -10.0;                          // [mV]

        // Solution vectors
        private Vector U;                                   // [mV]
        private Vector M; Vector N; Vector H;               // no units

        public override float GetSimulationTime() => i * (float)k;
        // Keep track of i locally so that we know which simulation frame to send to other scripts
        private int i = -1;

        // Send simulation 1D values 
        public override double[] Get1DValues()
        {
            double[] curVals = null;
            try
            {
                mutex.WaitOne();

                if (i > -1)
                {
                    Vector curTimeSlice = U.SubVector(0, NeuronCell.vertCount);
                    curTimeSlice.Multiply(1, curTimeSlice);
                    curVals = curTimeSlice.ToArray();
                }

                mutex.ReleaseMutex();
            }
            catch (Exception e){UnityEngine.Debug.LogError(e);}
            return curVals;
        }

        // Receive new simulation 1D index/value pairings
        public override void Set1DValues(Tuple<int, double>[] newValues)
        {
            try
            {
                // Wait for access to U, incase Solve() is using it
                mutex.WaitOne();

                foreach (Tuple<int, double> newVal in newValues)
                {
                    int j = newVal.Item1;
                    double val = newVal.Item2 * vstart * 0.75;
                    U[j] += val;
                }

                mutex.ReleaseMutex();
            }
            catch (Exception e){UnityEngine.Debug.LogError(e);}
        }

        protected override void Solve()
        {
            InitNeuronCell();
            for (int kSim = 0; kSim <= numRuns; kSim++)
            {
                int nT;                                                             // Number of Time steps
                List<bool> channels = new List<bool> { false, false, false };       // For adding/removing channels

                // TODO: NEED TO DO THIS BETTER
                if (HK_auto)
                {
                    h = 0.1 * NeuronCell.edgeLengths.Average();
                    if (h <= 1) { k = h / 130; }                                    // 0 refine       
                    if (h <= 0.5) { k = h / 65; }                                   // 1 refine
                    if (h <= 0.25) { k = h / 32.5; }                                // 2 refine
                    if (h <= 0.12) { k = h / 18; }                                  // 3 refine
                    if (h <= 0.06) { k = h / 9; }                                   // 4 refine
                    if (h <= 0.03) { k = h / 5; }
                }

                // Number of time steps
                nT = (int)System.Math.Floor(endTime / k);

                // set some constants for the HINES matrix
                double diffConst = (1 / (2 * res * cap));
                double cfl = diffConst * k / h;
                // reaction vector
                Vector R = Vector.Build.Dense(NeuronCell.vertCount);
                List<double> reactConst = new List<double> { gk, gna, gl, ek, ena, el };
                // temporary voltage vector
                Vector tempV = Vector.Build.Dense(NeuronCell.vertCount);

                // Construct sparse RHS and LHS in coordinate storage format, no zeros are stored
                List<CoordinateStorage<double>> sparse_stencils = makeSparseStencils(NeuronCell, h, k, diffConst);
                // Compress the sparse matrices
                CompressedColumnStorage<double> r_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[0]); //null;
                CompressedColumnStorage<double> l_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[1]); //null;

                // Permutation matrix----------------------------------------------------------------------//
                int[] p = new int[NeuronCell.vertCount];
                if (randomPermute) { p = Permutation.Create(NeuronCell.vertCount, 1); }else { p = Permutation.Create(NeuronCell.vertCount, 0); }
                CompressedColumnStorage<double> Id_csc = CompressedColumnStorage<double>.CreateDiagonal(NeuronCell.vertCount, 1);
                Id_csc.PermuteRows(p);
                //--------------------------------------------------------------------------------------------//

                // for solving Ax = b problem
                double[] b = new double[NeuronCell.vertCount];

                // Create Cholesky factorization setup
                var chl = SparseCholesky.Create(l_csc, p);
                //var chl = SparseCholesky.Create(l_csc, order);
               
                try
                {
                    for (i = 0; i < nT; i++)
                    {
                        // Wait for access to U, in case the main thread is requesting values
                        mutex.WaitOne();

                        if (SomaOn) { U.SetSubVector(0, NeuronCell.vertCount, setSoma(U, NeuronCell.somaID, vstart)); }
                        
                        r_csc.Multiply(U.ToArray(), b);         // Peform b = rhs * U_curr 
                        // Diffusion solver
                        chl.Solve(b, b);

                        // Set U_next = b
                        U.SetSubVector(0, NeuronCell.vertCount, Vector.Build.DenseOfArray(b));
                        // Save voltage from diffusion step for state probabilities
                        tempV.SetSubVector(0, NeuronCell.vertCount, U);

                        // Reaction
                        channels[0] = na_ONOFF; channels[1] = k_ONOFF; channels[2] = leak_ONOFF;
                        R.SetSubVector(0, NeuronCell.vertCount, reactF(reactConst, U, N, M, H, channels, NeuronCell.boundaryID));
                        R.Multiply(k / cap, R);

                        // This is the solution for the voltage after the reaction is included!
                        U.Add(R, U);
                        //Now update state variables using FE on M,N,H
                        N.Add(fN(tempV, N).Multiply(k), N);
                        M.Add(fM(tempV, M).Multiply(k), M);
                        H.Add(fH(tempV, H).Multiply(k), H);
                        //Always reset to IC conditions and boundary conditions (for now)
                        U.SetSubVector(0, NeuronCell.vertCount, boundaryConditions(U, NeuronCell.boundaryID));
                        if (SomaOn) { U.SetSubVector(0, NeuronCell.vertCount, setSoma(U, NeuronCell.somaID, vstart)); }

                        // Release access to U
                        mutex.ReleaseMutex();
                    }

                }
                catch (Exception e){GameManager.instance.DebugLogThreadSafe(e);}
                GameManager.instance.DebugLogSafe("Simulation Over.");
            }
        }

        #region Local Functions
        private void InitNeuronCell()
        {
            //Initialize vector with all zeros
            U = Vector.Build.Dense(NeuronCell.vertCount, 0);
            M = Vector.Build.Dense(NeuronCell.vertCount, mi);
            N = Vector.Build.Dense(NeuronCell.vertCount, ni);
            H = Vector.Build.Dense(NeuronCell.vertCount, hi);

            //Set the initial conditions of the solution
            U.SetSubVector(0, NeuronCell.vertCount, initialConditions(U, NeuronCell.boundaryID));
            if (SomaOn) { U.SetSubVector(0, NeuronCell.vertCount, setSoma(U, NeuronCell.somaID, vstart)); }
        }
        // This is for constructing the lhs and rhs of system matrix
        // This will construct a HINES matrix (symmetric), it should be tridiagonal with some off
        // diagonal entries corresponding to a branch location in the neuron graph
        public static List<CoordinateStorage<double>> makeSparseStencils(NeuronCell myCell, double h, double k, double diffConst)
        {
            List<CoordinateStorage<double>> stencils = new List<CoordinateStorage<double>>();

            var rhs = new CoordinateStorage<double>(myCell.vertCount, myCell.vertCount, myCell.vertCount * myCell.vertCount);
            var lhs = new CoordinateStorage<double>(myCell.vertCount, myCell.vertCount, myCell.vertCount * myCell.vertCount);

            // for keeping track of the neighbors of a node
            int nghbrCount;
            int nghbrInd;

            // need cfl coefficient
            double cfl = diffConst * k / h;
            double vRad= 0.14;

            for (int p = 0; p < myCell.vertCount; p++)
            {
                nghbrCount = myCell.nodeData[p].neighborIDs.Count;
                //vRad = myCell.nodeData[p].nodeRadius;
                //if (vRad >= 1) { vRad = 1; }

                // set main diagonal entries
                rhs.At(p, p, 1 - ((double)nghbrCount) * vRad * cfl / (2 * h));
                lhs.At(p, p, 1 + ((double)nghbrCount) * vRad * cfl / (2 * h));

                // this inner loop is for setting the off diagonal entries which correspond
                // to the neighbors of each node in the branch structure
                for (int q = 0; q < nghbrCount; q++)
                {
                    nghbrInd = myCell.nodeData[p].neighborIDs[q];

                    // should I be using the neighbor radii here or same as main node?
                    //vRad = myCell.nodeData[myCell.nodeData[p].neighborIDs[q]].nodeRadius;

                    // for off diagonal entries
                    rhs.At(p, nghbrInd, vRad * cfl / (4 * h));
                    rhs.At(nghbrInd, p, vRad * cfl / (4 * h));

                    // for off diagonal entries
                    lhs.At(p, nghbrInd, -vRad * cfl / (4 * h));
                    lhs.At(nghbrInd, p, -vRad * cfl / (4 * h));
                }

            }

            //
            int bcInd;
            for (int p = 0; p < myCell.boundaryID.Count; p++)
            {
                bcInd = myCell.boundaryID[p];
                rhs.At(bcInd, bcInd, 1 - (1 * vRad * cfl / (2 * h)));
                lhs.At(bcInd, bcInd, 1 + (1 * vRad * cfl / (2 * h)));
            }

            //

            stencils.Add(rhs);
            stencils.Add(lhs);
            return stencils;
        }

        // this sets the soma voltage for testing
        // the soma maybe a set of indices, not just one.
        public static Vector setSoma(Vector V, List<int> somaID, double voltage){V[0] = voltage; return V;}

        //Function for initializes voltage on cell
        public static Vector initialConditions(Vector V, List<int> bcIndices)
        {
            // only set boundary conditions
            V.SetSubVector(0, V.Count, boundaryConditions(V, bcIndices));
            return V;
        }

        // initialize the ends of dendrites to be clamped at 0 mV
        public static Vector boundaryConditions(Vector V, List<int> bcIndices){return V;}

        // this is the reaction term of the HH equation
        private static Vector reactF(List<double> reactConst, Vector V, Vector NN, Vector MM, Vector HH, List<bool> channels, List<int> bcIndices)
        {
            Vector output = Vector.Build.Dense(V.Count);
            Vector prod = Vector.Build.Dense(V.Count);
            double ek, ena, el, gk, gna, gl;

            // set constants for voltage
            gk = reactConst[0]; gna = reactConst[1]; gl = reactConst[2];
            // set constants for conductances
            ek = reactConst[3]; ena = reactConst[4]; el = reactConst[5];

            // Add current due to potassium
            if (channels[0]){prod = NN.PointwisePower(4); prod = (V - ek).PointwiseMultiply(prod); output = gk * prod;}
            // Add current due to sodium
            if (channels[1]){prod = MM.PointwisePower(3); prod = HH.PointwiseMultiply(prod); prod = (V - ena).PointwiseMultiply(prod); output = output + gna * prod;}        
            // Add leak current
            if (channels[2]){output = output + gl * (V - el);}
            // Return the negative of the total
            output.Multiply(-1, output);
            output.SetSubVector(0, V.Count, boundaryConditions(output, bcIndices));
            return output;
        }
        // The following functions are for the state variable ODEs on M,N,H
        private static Vector fN(Vector V, Vector N){return an(V).PointwiseMultiply(1 - N) - bn(V).PointwiseMultiply(N);}
        private static Vector fM(Vector V, Vector M){return am(V).PointwiseMultiply(1 - M) - bm(V).PointwiseMultiply(M);}
        private static Vector fH(Vector V, Vector H){return ah(V).PointwiseMultiply(1 - H) - bh(V).PointwiseMultiply(H);}
        //The following functions are for the state variable ODEs on M,N,H
        private static Vector an(Vector V){return 0.01 * (10-V).PointwiseDivide(((10-V) / 10).PointwiseExp() - 1);}
        private static Vector bn(Vector V){return 0.125 * (-1 * V / 80).PointwiseExp();}
        private static Vector am(Vector V){return 0.1 * (25-V).PointwiseDivide(((25-V) / 10).PointwiseExp() - 1);}
        private static Vector bm(Vector V){return 4 * (-1 * V / 18).PointwiseExp();}
        private static Vector ah(Vector V){return 0.07 * (-1 * V / 20).PointwiseExp();}
        private static Vector bh(Vector V){return 1 / (((30-V) / 10).PointwiseExp() + 1);}
        #endregion
    }
}
