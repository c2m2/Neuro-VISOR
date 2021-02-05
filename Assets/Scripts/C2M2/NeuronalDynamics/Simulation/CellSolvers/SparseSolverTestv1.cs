using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System;
using System.Diagnostics;
using UnityEngine;
using C2M2.NeuronalDynamics.Interaction;

// These are the MathNet Numerics Libraries needed
// They need to dragged and dropped into the Unity assets plugins folder!
// using SparseMatrix = MathNet.Numerics.LinearAlgebra.Double.SparseMatrix;
// using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;

using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Data.Text;

// These are for the sparse solving
using CSparse.Storage;
using CSparse.Double.Factorization;
//using Vector=CSparse.Double.Vector;
using CSparse;

using C2M2.Utils;
using C2M2.NeuronalDynamics.UGX;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
namespace C2M2.NeuronalDynamics.Simulation
{
    /// <summary>
    /// This is the sparse solver class for solving the Hodgkin-Huxley equations for the propagation of action potentials.
    /// The main solver begins with the function call <c>Solve()</c> prior to each iteration of <c>Solve()</c> the new voltage
    /// values <c>U</c> are sent to this class and then new <c>U</c> are sent out from <c>Solve()</c>
    /// 
    /// The solver currently uses Forward Euler time stepping and the spatial solve is done using Crank-Nicolson in 1D
    /// The solver takes into account the non-uniform radii of the geometry and the non-uniform edgelength of the geometry
    /// 
    /// The first part of the code is labeled with 'Simulation Parameters' and 'Biological Parameters'
    /// These parameters need to be made available to the user to modify for their particular simulation parameters
    /// The rate functions are defined at the end
    /// Note: Very important --> ALL UNITS FOR THE SOLVER ARE IN MKS, therefore when modifying the color bars ranges, and raycast/clamp hit values
    /// they need to be in [V] not [mV] so if you intend to use 50 [mV] it needs to be coded as 0.05 [V]
    /// </summary>
    /// 

    //tex: Below are the Hodgkin Huxley equations
    //$$\frac{a}{2R}\frac{\partial^2V}{\partial x^2}=C\frac{\partial V}{\partial t}+\bar{g}_{K}n^4(V-V_k)+\bar{g}_{Na}m^3h(V-V_{Na})+\bar{g}_l(V-V_l)$$
    //$$\frac{dn}{dt}=\alpha_n(V)(1-n)-\beta_n(V)n$$
    //$$\frac{dm}{dt}=\alpha_m(V)(1-m)-\beta_m(V)m$$
    //$$\frac{dh}{dt}=\alpha_h(V)(1-h)-\beta_h(V)h$$

    public class SparseSolverTestv1 : NDSimulation
    {
        /// <summary>
        /// These are the simulation parameters, initial voltage hit value for raycasting, the endTime, time step size (k)
        /// Also other options are defined here such as having the SomaOn for clamping the soma on for voltage clamp tests (this is 
        /// mostly used for verifying the voltage output against Yale Neuron).
        /// Another set of options that need to be incorporated is to select whether to output voltage data
        /// 
        /// TODO: define default time step size
        /// TODO: get rid of end time user should signal off in vr, this will require changing the loop structure in the solver
        /// to a while loop()
        /// </summary>
        /// 
        ///<param><c>vstart</c> voltage for soma clamp in    [V]     </param> 
        ///<param><c>endTime</c> endTime of the simulation in [s]     </param>
        ///<param><c>k</c> User enters time step size   [s]     </param> 
        ///<param><c>SomaOn</c> This is for turning the soma on/off  </param>
        ///<param><c>saveMatrices</c> send LHS and RHS matrices to output  </param> 
        [Header("Simulation Parameters")]
        public double vstart = 0.050;           
        public double endTime = 0.1;            
        public double k = 0.0025 * 1.0E-3;      
        public bool SomaOn = false;             
        private bool saveMatrices = false;

        ///<summary>
        /// These are the biological parameters that can also be set by the user, right now they are set to private
        /// but we want the user to be able to modify these prior to the start of the simulation
        /// 
        /// TODO: have the inputs for these parameters be shown inspector for easy changing --> as sliders
        /// </summary>
        ///<param><c>res</c> [ohm.m] resistance.length</param> 
        ///<param><c>cap</c> [F/m2] capacitance per unit area</param>
        ///<param><c>gk</c>  [S/m2] potassium conductance per unit area</param> 
        ///<param><c>gna</c> [S/m2] sodium conductance per unit area</param>
        ///<param><c>gl</c>  [S/m2] leak conductance per unit area</param>
        ///<param><c>ek</c>  [V] potassium reversal potential</param>
        ///<param><c>ena</c> [V] sodium reversal potential</param> 
        ///<param><c>el</c>  [V] leak reversal potential</param> 
        ///<param><c>ni</c>  [] potassium channel state probability, unitless</param> 
        ///<param><c>mi</c>  [] sodium channel state probability, unitless</param> 
        ///<param><c>hi</c>  [] sodium channel state probability, unitless</param>   
        private double res = 2500.0 * 1.0E-2;     
        private double cap = 1.0 * 1.0E-2;        
        private double gk = 5.0 * 1.0E1;          
        private double gna = 50.0 * 1.0E1;        
        private double gl = 0.0 * 1.0E1;          
        private double ek = -90.0 * 1.0E-3;        
        private double ena = 50.0 * 1.0E-3;       
        private double el = -70.0 * 1.0E-3;       
        private double ni = 0.0376969;            
        private double mi = 0.0147567;            
        private double hi = 0.9959410;                                 

        /// <summary>
        /// These are the solution vectors for the voltage <code>U</code>
        /// the state <c>M</c>, state <c>N</c>, and state <c>H</c>
        /// </summary>
        private Vector U;
        private Vector M;
        private Vector N;
        private Vector H;

        /// <summary>
        /// This keeps track of which simulation frame to send to the other scripts
        /// <c>i</c> gets track of the time step number
        /// </summary>
        private int i = -1;

        /// <summary>
        /// This sends the current time to the simulation timer
        /// Carefully notice that it has to be multiplied by 1000, this is because the solver is in MKS
        /// and the simulation timer object uses [ms]!
        /// </summary>
        /// <returns>i*(float)1000*(float) k</returns>
        public override float GetSimulationTime() => i*(float)1000*(float) k;

        /// <summary>
        /// Send simulation 1D values, this send the current voltage after the solve runs 1 iteration
        /// it passes <c>curVals</c>
        /// </summary>
        /// <returns>curVals</returns>
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
            catch (Exception e)
            {
                GameManager.instance.DebugLogErrorThreadSafe(e);
                mutex.ReleaseMutex();
            }
            return curVals;
        }

        /// <summary>
        /// Receive new simulation 1D index/value pairings
        /// Carefully, notice that <c>val</c> needs to be multiplied by 0.001 this is because
        /// the hit value is in [mV] and the solver uses [V]
        /// </summary>
        /// <param name="newValues"></param>
        public override void Set1DValues(Tuple<int, double>[] newValues)
        {
            try
            {
                mutex.WaitOne();
                foreach (Tuple<int, double> newVal in newValues)
                {
                    int j = newVal.Item1;
                    double val = newVal.Item2;
                    U[j] += val*(1E-3);
                }
                mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                GameManager.instance.DebugLogErrorThreadSafe(e);
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// This is a small routine call to initialize the Neuron Cell
        /// this will initialize the solution vectors which are <c>U</c>, <c>M</c>, <c>N</c>, and <c>H</c>
        /// </summary>
        protected override void PreSolve()
        {
            InitializeNeuronCell();
        }

        /// <summary>
        /// This is the main solver, it is running on it own thread.
        /// Inside this solver it 
        /// 1. initialize the stencil matrix (only once) for the diffusion solve of the operator splitting
        /// 2. there is a for-loop which is controled by <c>i</c>
        /// 3. Inside the for-loop we do the diffusion solve first, then reaction solve, and then updated the state ODEs
        /// </summary>
        /// 
        //tex:
        //$$A(V):=\frac{a}{2RC}\frac{\partial ^ 2V}{\partial x^2}$$
        //$$r(V):= -\frac{\bar{ g} _{ K} }{ C}n ^ 4(V - V_k) -\frac{\bar{ g} _{ Na} }{ C}m ^ 3h(V - V_{ Na})-\frac{\bar{ g} _l}{ C} (V - V_l)$$
        // then we solve in two separate steps
        //$$\frac{dV}{dt}=A(V)+r(V),$$
        //where $A(V)$ is the second order differential operator on $V$ and $r(V)$ is the reaction term on $V$. We employ a Lie Splitting by first solving
        //$$\frac{ dV ^ *}{ dt}= A(V ^ *)$$
        //with initial condition $V_0^*=V(t_n)= V_n$ at the beginning of the time step to get the intermediate solution $V^*$. Then we solve
        //$\frac{dV^{**}}{dt}=r(V^{**})$ with initial condition $V_0^{**}=V^*$ to get $V^{**}$, and $V_{n+1}=V(t_{n+1})=V^{**}$ the voltage at the end of the time step.
        //For equation the diffusion we use a Crank-Nicolson scheme

        protected override void Solve()
        {       
            
            int nT;                                                             // Number of Time steps
           
            // Number of time steps
            nT = (int)System.Math.Floor(endTime / k);
               
            // reaction vector
            Vector R = Vector.Build.Dense(NeuronCell.vertCount);
            List<double> reactConst = new List<double> { gk, gna, gl, ek, ena, el };
                        
            // Construct sparse RHS and LHS in coordinate storage format, no zeros are stored
            List<CoordinateStorage<double>> sparse_stencils = makeSparseStencils(NeuronCell, res, cap, k);

            // Compress the sparse matrices
            CompressedColumnStorage<double> r_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[0]); //null;
            CompressedColumnStorage<double> l_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[1]); //null;
                                             
            double[] b = new double[NeuronCell.vertCount];

            var lu = SparseLU.Create(l_csc, ColumnOrdering.MinimumDegreeAtA, 0.1);

            try
            {
                for (i = 0; i < nT; i++)
                {                       
                    mutex.WaitOne();
                    if ((i * k >= 0.015) && SomaOn) { U[0] = vstart; }

                    r_csc.Multiply(U.ToArray(), b);         // Peform b = rhs * U_curr 
                    lu.Solve(b, b);
                    U.SetSubVector(0, NeuronCell.vertCount, Vector.Build.DenseOfArray(b));
                                       
                    R.SetSubVector(0, NeuronCell.vertCount, reactF(reactConst, U, N, M, H, cap));
                    R.Multiply(k, R);

                    // This is the solution for the voltage after the reaction is included!
                    U.Add(R, U);

                    //Now update state variables using FE on M,N,H
                    N.Add(fN(U, N).Multiply(k), N);
                    M.Add(fM(U, M).Multiply(k), M);
                    H.Add(fH(U, H).Multiply(k), H);

                    if ((i * k >= 0.015) && SomaOn) { U[0] = vstart; }

                    // Apply clamp voltages
                    if (clamps != null && clamps.Count > 0)
                    {
                        foreach (NeuronClamp clamp in clamps)
                        {
                            if (clamp != null && clamp.focusVert != -1 && clamp.clampLive)
                            {
                                U[clamp.focusVert] = (1E-03)*clamp.clampPower;
                            }
                        }
                    }

                    mutex.ReleaseMutex();
                }
                  
            }
            catch (Exception e)
            {
                GameManager.instance.DebugLogErrorThreadSafe(e);
                mutex.ReleaseMutex();
            }
                
            GameManager.instance.DebugLogSafe("Simulation Over.");
            
        }

        #region Local Functions

        private void InitializeNeuronCell()
        {
            //Initialize vector with all zeros
            U = Vector.Build.Dense(NeuronCell.vertCount, 0);
            M = Vector.Build.Dense(NeuronCell.vertCount, mi);
            N = Vector.Build.Dense(NeuronCell.vertCount, ni);
            H = Vector.Build.Dense(NeuronCell.vertCount, hi);
        }

        // This is for constructing the lhs and rhs of system matrix
        // This will construct a HINES matrix (symmetric), it should be tridiagonal with some off
        // diagonal entries corresponding to a branch location in the neuron graph
        public static List<CoordinateStorage<double>> makeSparseStencils(NeuronCell myCell, double res, double cap, double k)
        {
            // send output matrices as a list {rhs, lhs}
            List<CoordinateStorage<double>> stencils = new List<CoordinateStorage<double>>();

            // initialize new coordinate storage
            var rhs = new CoordinateStorage<double>(myCell.vertCount, myCell.vertCount, myCell.vertCount * myCell.vertCount);
            var lhs = new CoordinateStorage<double>(myCell.vertCount, myCell.vertCount, myCell.vertCount * myCell.vertCount);

            // for keeping track of the neighbors of a node
            List<int> nghbrlist;
            int nghbrLen;

            // make an empty list to collect edgelengths
            List<double> edgelengths = new List<double>();
            double tempEdgeLen, tempRadius, aveEdgeLengths;
            double sumRecip = 0;
            double scf = 1E-6;  //scale factor to convert to micrometers for radii and edge length

            for (int j = 0; j < myCell.vertCount; j++)
            {
                // this gets the current neighbor list for node j
                nghbrlist = myCell.nodeData[j].neighborIDs;

                // this is the length of the neighbor list
                nghbrLen = nghbrlist.Count();

                edgelengths.Clear();
                sumRecip = 0;

                // get the current radius at node j
                tempRadius = myCell.nodeData[j].nodeRadius*scf;

                // in this loop we collect the edgelengths that 
                // go to node j, and we compute the coefficient given in 
                // our paper
                for (int p = 0; p < nghbrLen; p++)
                {
                    // get the edge length at current node j, to node neighbor p, scale to micro meters
                    tempEdgeLen = myCell.GetEdgeLength(j, nghbrlist[p])*scf;

                    // put the edge length in the list, this list of edges will have length equal to length of neighbor list
                    edgelengths.Add(tempEdgeLen);
                    sumRecip = sumRecip + 1 / (tempEdgeLen * tempRadius * ((1 / (myCell.nodeData[nghbrlist[p]].nodeRadius*scf* myCell.nodeData[nghbrlist[p]].nodeRadius*scf)) + (1 / (tempRadius * tempRadius))));
                }
                // get the average edge lengths of neighbors
                aveEdgeLengths = 0;
                foreach (double val in edgelengths)
                {
                    aveEdgeLengths = aveEdgeLengths + val;
                }
                aveEdgeLengths = aveEdgeLengths / edgelengths.Count;
                //GameManager.instance.DebugLogSafe("ave = " + aveEdgeLengths);

                // set main diagonal entries
                rhs.At(j, j, 1 - (k * sumRecip) / (2.0 * res * cap * aveEdgeLengths));
                lhs.At(j, j, 1 + (k * sumRecip) / (2.0 * res * cap * aveEdgeLengths));

                // set off diagonal entries
                for (int p = 0; p < nghbrLen; p++)
                {
                    rhs.At(j, nghbrlist[p], k / (2 * res * cap * tempRadius* aveEdgeLengths * edgelengths[p] * ((1 / (myCell.nodeData[nghbrlist[p]].nodeRadius*scf * myCell.nodeData[nghbrlist[p]].nodeRadius*scf)) + (1 / (tempRadius * tempRadius)))));
                    lhs.At(j, nghbrlist[p], -1.0 * k / (2 * res * cap * tempRadius * aveEdgeLengths * edgelengths[p] * ((1 / (myCell.nodeData[nghbrlist[p]].nodeRadius*scf * myCell.nodeData[nghbrlist[p]].nodeRadius*scf)) + (1 / (tempRadius * tempRadius)))));
                }

            }
            //rhs.At(0, 0, 1);
            //lhs.At(0, 0, 1);

            stencils.Add(rhs);
            stencils.Add(lhs);
            return stencils;
        }

        // this is the reaction term of the HH equation
        // this is the reaction term of the HH equation
        private static Vector reactF(List<double> reactConst, Vector V, Vector NN, Vector MM, Vector HH, double cap)
        {
            Vector output = Vector.Build.Dense(V.Count, 0.0);
            Vector prod = Vector.Build.Dense(V.Count, 0.0);
            double ek, ena, el, gk, gna, gl;

            // set constants for voltage
            gk = reactConst[0]; gna = reactConst[1]; gl = reactConst[2];
            // set constants for conductances
            ek = reactConst[3]; ena = reactConst[4]; el = reactConst[5];

            // Add current due to potassium
            prod.SetSubVector(0, V.Count, NN.PointwisePower(4.0));
            prod.SetSubVector(0, V.Count, (V.Subtract(ek)).PointwiseMultiply(prod));
            output.Add(prod.Multiply(gk), output);

            // Add current due to sodium
            prod.SetSubVector(0, V.Count, MM.PointwisePower(3.0));
            prod.SetSubVector(0, V.Count, HH.PointwiseMultiply(prod)); prod.SetSubVector(0, V.Count, (V.Subtract(ena)).PointwiseMultiply(prod));
            output.Add(prod.Multiply(gna), output);

            // Add leak current
            output.Add((V.Subtract(el)).Multiply(gl), output);
            // Return the negative of the total
            output.Multiply(-1.0 / cap, output);

            return output;
        }
        // The following functions are for the state variable ODEs on M,N,H
        private static Vector fN(Vector V, Vector N) { return an(V).PointwiseMultiply(1 - N) - bn(V).PointwiseMultiply(N); }
        private static Vector fM(Vector V, Vector M) { return am(V).PointwiseMultiply(1 - M) - bm(V).PointwiseMultiply(M); }
        private static Vector fH(Vector V, Vector H) { return ah(V).PointwiseMultiply(1 - H) - bh(V).PointwiseMultiply(H); }
        //The following functions are for the state variable ODEs on M,N,H

        //The following functions are for the state variable ODEs on M,N,H
        private static Vector an(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);

            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.032) * (15.0 - Vin).PointwiseDivide(((15.0 - Vin) / 5.0).PointwiseExp() - 1.0);
        }

        private static Vector bn(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);

            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.5) * ((10.0 - Vin) / 40.0).PointwiseExp();
        }
        private static Vector am(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);

            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.32) * (13.0 - Vin).PointwiseDivide(((13.0 - Vin) / 4.0).PointwiseExp() - 1.0);
        }
        private static Vector bm(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);

            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.28) * (Vin - 40.0).PointwiseDivide(((Vin - 40.0) / 5.0).PointwiseExp() - 1.0);
        }
        private static Vector ah(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);

            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.128) * ((17.0 - Vin) / 18.0).PointwiseExp();
        }
        private static Vector bh(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);

            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * 4.0 / (((40.0 - Vin) / 5.0).PointwiseExp() + 1.0);
        }


        #endregion
    }
}