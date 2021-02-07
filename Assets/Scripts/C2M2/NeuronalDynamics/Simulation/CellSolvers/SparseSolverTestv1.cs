﻿using System.Collections;
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
    /// This is the sparse solver class for solving the Hodgkin-Huxley equations for the propagation of action potentials. Below are the HH equations
    /// this is a system of partial differential equations (PDE), the first equation is for the membrane potential (voltage) it is time spatially dependent
    /// and the remaining equations are the ODE equations for the state variables n,m,h (these are unitless). The system is non-trivial in that the PDE equation on Voltage
    /// is coupled to the ODE equations using n,m,h and these ODE equations are also non-linear.
    /// 
    ///\f[\frac{a}{2R}\frac{\partial^2V}{\partial x^2}=C\frac{\partial V}{\partial t}+\bar{g}_{K}n^4(V-V_k)+\bar{g}_{Na}m^3h(V-V_{Na})+\bar{g}_l(V-V_l)\f]
    ///
    ///\f[\frac{dn}{dt}=\alpha_n(V)(1-n)-\beta_n(V)n\f]
    ///
    ///\f[\frac{dm}{dt}=\alpha_m(V)(1-m)-\beta_m(V)m\f]
    ///
    ///\f[\frac{dh}{dt}=\alpha_h(V)(1-h)-\beta_h(V)h\f]
    ///
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
    /// 
    /// The simulation parameters are defined first, initial voltage hit value for raycasting, the endTime, time step size (k)
    /// Also other options are defined here such as having the SomaOn for clamping the soma on for voltage clamp tests (this is 
    /// mostly used for verifying the voltage output against Yale Neuron).
    /// Another set of options that need to be incorporated is to select whether to output voltage data
    /// 
    /// 1) TODO: define default time step size
    /// 
    /// 2) TODO: get rid of end time user should signal off in vr, this will require changing the loop structure in the solver to a while loop()
    /// 
    /// 3) These are the biological parameters that can also be set by the user, right now they are set to private but we want the user to be able to modify these prior to the start of the simulation
    /// 
    /// 4) TODO: have the inputs for these parameters be shown inspector for easy changing --> as sliders
    /// </summary>

    //tex: Below are the Hodgkin Huxley equations
    //$$\frac{a}{2R}\frac{\partial^2V}{\partial x^2}=C\frac{\partial V}{\partial t}+\bar{g}_{K}n^4(V-V_k)+\bar{g}_{Na}m^3h(V-V_{Na})+\bar{g}_l(V-V_l)$$
    //$$\frac{dn}{dt}=\alpha_n(V)(1-n)-\beta_n(V)n$$
    //$$\frac{dm}{dt}=\alpha_m(V)(1-m)-\beta_m(V)m$$
    //$$\frac{dh}{dt}=\alpha_h(V)(1-h)-\beta_h(V)h$$

    public class SparseSolverTestv1 : NDSimulation
    {
        [Header("Simulation Parameters")]
        ///<summary>
        /// This is the voltage for the voltage clamp, this is primarily used for when we do the convergence analysis of the code using a 
        /// soma clamp at 50 [mV], the units for voltage in the solver is [V] that is why <c>vstart</c> is set to 0.05
        ///</summary>
        public double vstart = 0.050;     
        /// <summary>
        /// [s] endTime of the simulation. This is the endtime of the simulation which is measured in seconds, this is a parameter that the user can set for now
        /// 
        /// TODO: we would like the simulation to run without an endtime and the use clicks an 'end button' to terminate the vr-simulation
        /// </summary>
        public double endTime = 0.1;      
        /// <summary>
        /// User enters the time step size [s], this is the time step size of the simulation, this needs to be chosen carefully as too large of 
        /// a time step size may cause numerical instability for the solver. Notice that that this is in [s] therefore 0.0025[ms] = 0.0025e-3 [s]
        /// 
        /// TODO: need to formulate a default time step size given a refinement level of geometry
        /// </summary>
        public double k = 0.0025 * 1.0E-3;    
        /// <summary>
        /// This is for turning the soma on/off, this option is primarily used for testing purposes for the convergence analysis, for a soma clamp experiment
        /// </summary>
        public bool SomaOn = false;            
        /// <summary>
        /// send LHS and RHS stencil matrices to output file for saving. This option is available if the user would like to check the sparsity pattern of the HINES matrix,
        /// this is a sparse matrix which corresponds to the sparsity nature of the stencil matrix. The stencil matrix is used for the diffusion solve of the PDE equation when
        /// we perform the operator splitting.
        /// </summary>
        private bool saveMatrices = false;
        ///<summary>
        /// [ohm.m] resistance.length, this is the axial resistence of the neuron, increasing this value has the effect of making the AP waves more localized and slower conduction speed
        /// decreasing this value has the effect of make the AP waves larger and have a faster conduction speed
        /// </summary>
        private double res = 2500.0 * 1.0E-2;
        /// <summary>
        /// [F/m2] capacitance per unit area, this is the plasma membrane capacitance, this a standard value for the capacitance
        /// </summary>
        private double cap = 1.0 * 1.0E-2;
        /// <summary>
        /// [S/m2] potassium conductance per unit area, this is the Potassium conductance per unit area, it is used in this term
        /// \f[\bar{g}_{K}n^4(V-V_k)\f]
        /// where \f$n\f$ is the state variable, and \f$V_k\f$ is the reversal potential.
        /// </summary>
        private double gk = 5.0 * 1.0E1;
        /// <summary>
        /// [S/m2] sodium conductance per unit area, this is the Sodium conductance per unit area, it is used in this term
        /// \f[\bar{g}_{Na}m^3h(V-V_{Na})\f]
        /// where \f$m,h\f$ are the state variables, and \f$V_{Na}\f$ is the reversal potential for sodium.
        /// </summary>
        private double gna = 50.0 * 1.0E1;
        /// <summary>
        /// [S/m2] leak conductance per unit area, this is the leak conductance per unit area, it is used in this term
        /// \f[\bar{g}_{l}(V-V_l)\f]
        /// \f$V_l\f$ is the leak reversal potential.
        /// </summary>
        private double gl = 0.0 * 1.0E1;
        /// <summary>
        /// [V] potassium reversal potential
        /// </summary>
        private double ek = -90.0 * 1.0E-3;
        /// <summary>
        /// [V] sodium reversal potential
        /// </summary>
        private double ena = 50.0 * 1.0E-3;
        /// <summary>
        /// [V] leak reversal potential
        /// </summary>
        private double el = -70.0 * 1.0E-3;
        /// <summary>
        /// [] potassium channel state probability, unitless
        /// </summary>
        private double ni = 0.0376969;
        /// <summary>
        /// [] sodium channel state probability, unitless
        /// </summary>
        private double mi = 0.0147567;
        /// <summary>
        /// [] sodium channel state probability, unitless  
        /// </summary>
        private double hi = 0.9959410;            

        /// <summary>
        /// These are the solution vectors for the voltage <code>U</code>
        /// the state <c>M</c>, state <c>N</c>, and state <c>H</c>
        /// </summary>
        private Vector U;
        /// <summary>
        /// These are the solution vectors for the voltage <code>U</code>
        /// the state <c>M</c>, state <c>N</c>, and state <c>H</c>
        /// </summary>
        private Vector M;
        /// <summary>
        /// These are the solution vectors for the voltage <code>U</code>
        /// the state <c>M</c>, state <c>N</c>, and state <c>H</c>
        /// </summary>
        private Vector N;
        /// <summary>
        /// These are the solution vectors for the voltage <code>U</code>
        /// the state <c>M</c>, state <c>N</c>, and state <c>H</c>
        /// </summary>
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
        public override float GetSimulationTime() => i*(float) k;

        /// <summary>
        /// Send simulation 1D values, this send the current voltage after the solve runs 1 iteration
        /// it passes <c>curVals</c>
        /// </summary>
        /// <returns>curVals</returns>
        public override double[] Get1DValues()
        {
            /// this initialize the curvals which will be sent back to the VR simulation
            double[] curVals = null;
            try
            {
                mutex.WaitOne();
                /// check if this beginning of the simulation
                if (i > -1)
                {
                    /// define the current time slice to send and initialize it to the correct size which is the number of vertices in the geometry
                    /// initialize it to the current state of the voltage, this is the voltage we are sending back to vr simulation
                    Vector curTimeSlice = U.SubVector(0, NeuronCell.vertCount);
                    curTimeSlice.Multiply(1, curTimeSlice);   
                    /// convert the time slice to an Array
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
                    /// here we set the voltage at the location, notice that we multiply by 0.0001 to convert to volts [V] 
                    U[j] = val*(1E-3);
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
        /// We make the following definitions: \n
        ///\f[A(V):=\frac{a}{2RC}\frac{\partial ^ 2V}{\partial x^2}\f] \n
        ///\f[r(V):= -\frac{\bar{ g} _{ K} }{ C}n ^ 4(V - V_k) -\frac{\bar{ g} _{ Na} }{ C}m ^ 3h(V - V_{ Na})-\frac{\bar{ g} _l}{ C} (V - V_l)\f] \n
        /// then we solve in two separate steps \n
        ///\f[\frac{dV}{dt}=A(V)+r(V),\f] \n
        /// where \f$A(V)\f$ is the second order differential operator on \f$V\f$ and \f$r(V)\f$ is the reaction part.
        /// We employ a Lie Splitting by first solving \n
        /// \f[\frac{ dV ^ *}{ dt}= A(V ^ *)\f] \n
        /// with initial conditions \f$V_0^*=V(t_n)= V_n\f$ at the beginning of the time step to get the intermediate solution \f$V^*\f$
        /// Then we solve \f$\frac{dV^{**}}{dt}=r(V^{**})\f$ with initial condition \f$V_0^{**}=V^*\f$ to get \f$V^{**}\f$, and \f$V_{n+1}=V(t_{n+1})=V^{**}\f$ the voltage at the end of the time step.
        /// For equation the diffusion we use a Crank-Nicolson scheme
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
            int nT;                                                                        
            ///<c>int nT</c> is the Number of time steps
            nT = (int)System.Math.Floor(endTime / k);
               
            ///<c>R</c> this is the reaction vector for the reaction solve
            Vector R = Vector.Build.Dense(NeuronCell.vertCount);
            ///<c>reactConst</c> this is a small list for collecting the conductances and reversal potential which is sent to the reaction solve routine
            List<double> reactConst = new List<double> { gk, gna, gl, ek, ena, el };

            ///<c>List<CoordinateStorage<double>> sparse_stencils = makeSparseStencils(NeuronCell, res, cap, k);</c> Construct sparse RHS and LHS in coordinate storage format, no zeros are stored \n
            /// <c>sparse_stencils</c> this is a list which contains only two matrices the LHS and RHS matrices for the Crank-Nicolson solve
            List<CoordinateStorage<double>> sparse_stencils = makeSparseStencils(NeuronCell, res, cap, k);
            ///<c>CompressedColumnStorage</c> call Compresses the sparse matrices which are stored in <c>sparse_stencils[0]</c> and <c>sparse_stencils[1]</c>
            CompressedColumnStorage<double> r_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[0]); //null;
            CompressedColumnStorage<double> l_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[1]); //null;
            ///<c>double [] b</c> we define storage for the diffusion solve part
            double[] b = new double[NeuronCell.vertCount];
            ///<c>var lu = SparseLU.Create(l_csc, ColumnOrdering.MinimumDegreeAtA, 0.1);</c> this creates the LU decomposition of the HINES matrix which is defined by <c>l_csc</c>
            var lu = SparseLU.Create(l_csc, ColumnOrdering.MinimumDegreeAtA, 0.1);

            try
            {
                for (i = 0; i < nT; i++)
                {                       
                    mutex.WaitOne();
                    ///<c>if ((i * k >= 0.015) && SomaOn) { U[0] = vstart; }</c> this checks of the somaclamp is on and sets the soma location to <c>vstart</c>
                    if ((i * k >= 0.015) && SomaOn) { U[0] = vstart; }
                    ///This part does the diffusion solve \n
                    /// <c>r_csc.Multiply(U.ToArray(), b);</c> the performs the RHS*Ucurr and stores it in <c>b</c> \n
                    /// <c>lu.Solve(b, b);</c> this does the forward/backward substitution of the LU solve and sovles LHS = b \n
                    /// <c>U.SetSubVector(0, NeuronCell.vertCount, Vector.Build.DenseOfArray(b));</c> this sets the U vector to the voltage at the end of the diffusion solve
                    r_csc.Multiply(U.ToArray(), b);
                    lu.Solve(b, b);
                    U.SetSubVector(0, NeuronCell.vertCount, Vector.Build.DenseOfArray(b));
                    /// this part solves the reaction portion of the operator splitting \n
                    /// <c>R.SetSubVector(0, NeuronCell.vertCount, reactF(reactConst, U, N, M, H, cap));</c> this first evaluates at the reaction function \f$r(V)\f$ \n
                    /// <c>R.Multiply(k, R); </c> this multiplies by the time step size \n
                    /// <c>U.add(R,U)</c> adds it back to U to finish off the operator splitting
                    /// For the reaction solve we are solving
                    /// \f[\frac{U_{next}-U_{curr}}{k} = R(U_{curr})\f]
                    R.SetSubVector(0, NeuronCell.vertCount, reactF(reactConst, U, N, M, H, cap));
                    R.Multiply(k, R);
                    U.Add(R, U);
                    /// this part solve the state variables using Forward Euler
                    /// the general rule is \f$N_{next} = N_{curr}+k\cdot f_N(U_{curr},N_{curr})\f$
                    N.Add(fN(U, N).Multiply(k), N);
                    M.Add(fM(U, M).Multiply(k), M);
                    H.Add(fH(U, H).Multiply(k), H);
                    ///<c>if ((i * k >= 0.015) && SomaOn) { U[0] = vstart; }</c> this checks of the somaclamp is on and sets the soma location to <c>vstart</c>
                    if ((i * k >= 0.015) && SomaOn) { U[0] = vstart; }

                    ///<c>if (clamps != null && clamps.Count > 0)</c> this if statement is where we apply voltage clamps
                    if (clamps != null && clamps.Count > 0)
                    {
                        foreach (NeuronClamp clamp in clamps)
                        {
                            if (clamp != null && clamp.focusVert != -1 && clamp.clampLive)
                            {
                                ///<c>U[clamp.focusVert] = (1E-03)*clamp.clampPower;</c> notice we multiply by 1e-3 since the hit value is in [mV] we need to convert to [V], volts.
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
        /// <summary>
        /// This function initializes the voltage vector <c>U</c> and the state vectors
        /// <c>M</c>, <c>N</c>, and <c>H</c> \n
        /// The input <c>NeuronCell.vertCount</c> is the vertex count of the neuron geometry \n
        /// <c>U</c> is initialized to 0 [V] for the entire cell \n
        /// <c>M</c> is initialized to \f$m_i\f$ which is set by <c>mi</c> \n
        /// <c>N</c> is initialized to \f$n_i\f$ which is set by <c>ni</c> \n
        /// <c>H</c> is initialized to \f$h_i\f$ which is set by <c>hi</c>
        /// </summary>
        private void InitializeNeuronCell()
        {
            U = Vector.Build.Dense(NeuronCell.vertCount, 0);
            M = Vector.Build.Dense(NeuronCell.vertCount, mi);
            N = Vector.Build.Dense(NeuronCell.vertCount, ni);
            H = Vector.Build.Dense(NeuronCell.vertCount, hi);
        }
        /// <summary>
        /// This is for constructing the lhs and rhs of system matrix \n
        /// This will construct a HINES matrix (symmetric), it should be tridiagonal with some off
        /// diagonal entries corresponding to a branch location in the neuron graph \n
        /// The entries are defined by the following:
        /// \f[
        /// \left(-\sum_{k\in\mathcal{N}_j}\eta_kV_k^{n+1}\right)+\omega_jV_j^{n+1}=\left(\sum_{k\in\mathcal{N}_j}\eta_kV_k^{n}\right)+\bar{\omega}_jV_j^{n}
        /// \f]
        /// where
        /// \f[\eta_k = \frac{\gamma_{k, j}\Delta t}{ 2}\f]
        /// and
        /// \f[\omega_j = 1+\frac{\theta_j\Delta t}{ 2} = 1 +\frac{\Delta t\sum_{ p\in\mathcal{ N} _j}\gamma_{ p,j} }{ 2}\f]
        /// and \f$\gamma_{ k,j}\f$ is defined as
        /// \f[\gamma_{k, j}:=\frac{ 1}{ C_mR_a a_j\widetilde{\Delta x_j} }\cdot \frac{ 1}{\left(\frac{ 1} { a_{ k} ^2} +\frac{ 1} { a_j ^ 2}\right)\Delta x_{ { k},j} }\f]
        /// </summary>
        /// <param name="myCell"></param> this is the <c>NeuronCell</c> that contains all the information about the cell geometry
        /// <param name="res"></param> this is the axial resistance
        /// <param name="cap"></param> this is the membrane capacitance
        /// <param name="k"></param> this is the fixed time step size
        /// <returns>LHS,RHS</returns> the function returns the LHS, RHS stencil matrices for the diffusion solve in sparse format, it is compressed in the main solver routine.
        public static List<CoordinateStorage<double>> makeSparseStencils(NeuronCell myCell, double res, double cap, double k)
        {
            /// send output matrices as a list {rhs, lhs}\n
            /// <c>List<CoordinateStorage<double>> stencils = new List<CoordinateStorage<double>>();</c> initializes empty list storage for the stencil matrices \n
            /// in this case they are of type <c>CoordinateStorage</c>
            List<CoordinateStorage<double>> stencils = new List<CoordinateStorage<double>>();

            /// initialize new coordinate storage
            /// <c>var rhs = new CoordinateStorage<double>(myCell.vertCount, myCell.vertCount, myCell.vertCount * myCell.vertCount);</c> this initializes our empty coordinate storage matrices <c>rhs</c> and <c>lhs</c>
            var rhs = new CoordinateStorage<double>(myCell.vertCount, myCell.vertCount, myCell.vertCount * myCell.vertCount);
            var lhs = new CoordinateStorage<double>(myCell.vertCount, myCell.vertCount, myCell.vertCount * myCell.vertCount);

            /// for keeping track of the neighbors of a node
            /// <c>List<int> nghbrlist;</c> this is for collecting the neighbor indices of the current node 
            List<int> nghbrlist;
            int nghbrLen;

            /// make an empty list to collect edgelengths
            /// <c>List<double> edgelengths = new List<double>();</c> initialize empty list for collecting edgelengths
            List<double> edgelengths = new List<double>();
            double tempEdgeLen, tempRadius, aveEdgeLengths;
            /// <c>double sumRecip = 0;</c> this is for adding the sum of reciprocals which is in our stencil scheme
            double sumRecip = 0;
            double scf = 1E-6;  /// 1e-6 scale factor to convert to micrometers for radii and edge length

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

        /// <summary>
        /// This is the reaction term of the HH equation which is defined by
        /// \f[ r(V):=-\frac{\bar{g}_{K}}{C}n^4(V-V_k)-\frac{\bar{g}_{Na}}{C}m^3h(V-V_{Na})-\frac{\bar{g}_l}{C}(V-V_l) \f]
        /// </summary>
        /// <param name="reactConst"></param> these are the conductances and reversal potentials defined by <c>List<double> reactConst = new List<double> { gk, gna, gl, ek, ena, el };</c>
        /// <param name="V"></param> this is the voltage vector
        /// <param name="NN"></param> this is the state vector n
        /// <param name="MM"></param> this is the state vector m
        /// <param name="HH"></param> this is the state vector h
        /// <param name="cap"></param> this is the capacitance
        /// <returns></returns>
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