using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using C2M2.NeuronalDynamics.Interaction;
/// These libraries are for using the Vector data type
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Data.Text;
/// These are for the sparse solving functionality
using CSparse.Storage;
using CSparse.Double.Factorization;
using CSparse;
using C2M2.Utils;
using C2M2.NeuronalDynamics.UGX;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
using Debug = UnityEngine.Debug;
//Hi
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
        /// This is for turning the soma on/off, this option is primarily used for testing purposes for the convergence analysis, for a soma clamp experiment
        /// </summary>
        public bool SomaOn = false;            
        ///<summary>
        /// [ohm.m] resistance.length, this is the axial resistence of the neuron, increasing this value has the effect of making the AP waves more localized and slower conduction speed
        /// decreasing this value has the effect of make the AP waves larger and have a faster conduction speed
        /// </summary>
        private double res = 5000.0 * 1.0E-2;
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
        /// the membrane resistance does not hit the theoretical maximum, it is approximately 65%
        /// but this may change depending on the rate functions that are used
        /// </summary>
        private double Rmemscf = 0.65;
        /// <summary>
        /// this is the scale factor for increasing the time step size if it is unnecessarily small
        /// </summary>
        private double cfl = 1.5;        

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
                if (time > -1)
                {
                    /// define the current time slice to send and initialize it to the correct size which is the number of vertices in the geometry
                    /// initialize it to the current state of the voltage, this is the voltage we are sending back to vr simulation
                    Vector curTimeSlice = U.SubVector(0, Neuron.nodes.Count);
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
                    if (newVal != null)
                    {
                        /// here we set the voltage at the location, notice that we multiply by 0.0001 to convert to volts [V] 
                        if (newVal.Item1 >= 0 && newVal.Item1 < Neuron.nodes.Count)
                        {
                            //   UnityEngine.Debug.Log("U[" + newVal.Item1 + "] = " + newVal.Item2);
                            U[newVal.Item1] = newVal.Item2;
                        }
                    }
                }
                mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                GameManager.instance.DebugLogErrorThreadSafe(e);
                mutex.ReleaseMutex();
            }
        }

        private Vector R;                                 //This is a vector for the reaction solve 
        private int mthd = 2;                                 // numerical method to use 0 = FE; 1 = BE; 2 = HEUN; 3 = RK4;
        private double[] b;                               //This is the right hand side vector when solving Ax = b
        List<double> reactConst;                            //This is for passing the reaction function constants
        List<CoordinateStorage<double>> sparse_stencils;    
        CompressedColumnStorage<double> r_csc;              //This is for the rhs sparse matrix
        CompressedColumnStorage<double> l_csc;              //This is for the lhs sparse matrix
        private SparseLU lu;                                //Initialize the LU factorizaation

        // Start some threads???
        //private Thread thrN1, thrN2;
        //private Thread thrM1, thrM2;
        //private Thread thrH1, thrH2;

        //private ManualResetEvent resetEvent = new ManualResetEvent(false);
        private AutoResetEvent done = new AutoResetEvent(false);
        private List<List<int>> partindset = new List<List<int>>();
        private int [] indsets;
        private int nthreads=4;

        private Stopwatch stopwatch;
        private TimeSpan ts;
        private string path = @"\Users\jaros\Desktop\timelogs\times.txt";
        private StreamWriter sw;
        /// <summary>
        /// This is a small routine call to initialize the Neuron Cell
        /// this will initialize the solution vectors which are <c>U</c>, <c>M</c>, <c>N</c>, and <c>H</c>
        /// </summary>
        protected override void PreSolve()
        {
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path)) { } ;
            }
            sw = File.AppendText(path);

            InitializeNeuronCell();
            

            ThreadPool.SetMaxThreads(14, 14);
            ThreadPool.SetMinThreads(12, 12);

            indsets = Enumerable.Range(0, U.Count).ToArray();
  
            partindset = partition(indsets, nthreads);

            Debug.Log("Length = " + partindset.Count());

            ///<c>R</c> this is the reaction vector for the reaction solve
            R = Vector.Build.Dense(Neuron.nodes.Count);
            ///<c>reactConst</c> this is a small list for collecting the conductances and reversal potential which is sent to the reaction solve routine
            reactConst = new List<double> { gk, gna, gl, ek, ena, el };

            /// this sets the target time step size
            //timeStep = SetTargetTimeStep(cap, 2 * Neuron.MaxRadius, Neuron.TargetEdgeLength, gna, gk, res, Rmemscf,cfl);
            ///UnityEngine.Debug.Log("Target Time Step = " + timeStep);
            
            ///<c>List<CoordinateStorage<double>> sparse_stencils = makeSparseStencils(Neuron, res, cap, k);</c> Construct sparse RHS and LHS in coordinate storage format, no zeros are stored \n
            /// <c>sparse_stencils</c> this is a list which contains only two matrices the LHS and RHS matrices for the Crank-Nicolson solve
            sparse_stencils = makeSparseStencils(Neuron, res, cap, timeStep);
            ///<c>CompressedColumnStorage</c> call Compresses the sparse matrices which are stored in <c>sparse_stencils[0]</c> and <c>sparse_stencils[1]</c>
            r_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[0]); //null;
            l_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[1]); //null;
            ///<c>double [] b</c> we define storage for the diffusion solve part
            b = new double[Neuron.nodes.Count];
            ///<c>var lu = SparseLU.Create(l_csc, ColumnOrdering.MinimumDegreeAtA, 0.1);</c> this creates the LU decomposition of the HINES matrix which is defined by <c>l_csc</c>
            lu = SparseLU.Create(l_csc, ColumnOrdering.MinimumDegreeAtA, 0.1);
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
        protected override void SolveStep(int t)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            if (mthd == 0)
            {
                // FE on State Variables at half time step
                stateFE(U, N, timeStep / 2, fN);
                stateFE(U, M, timeStep / 2, fM);
                stateFE(U, H, timeStep / 2, fH);

                // FE on reaction term at half time step
                R.SetSubVector(0, Neuron.nodes.Count, reactF(reactConst, U, N, M, H, cap));
                R.Multiply(timeStep/2, R);
                U.Add(R, U);
            }
            else if (mthd == 1)
            {
                // BE on State Variables and Reaction Solve for half timestep
                stateBE(U, N, timeStep / 2, an, bn);
                stateBE(U, M, timeStep / 2, am, bm);
                stateBE(U, H, timeStep / 2, ah, bh);
                // BE on Reaction term
                reactBE(U, N, M, H, timeStep / 2, cap, reactConst);
            }
            else if (mthd ==2)
            {

                // Heun method on state variables
                //stateHEUN(U, N, timeStep / 2, an, bn);
                //stateHEUN(U, M, timeStep / 2, am, bm);
                //stateHEUN(U, H, timeStep / 2, ah, bh);
                                
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[0][0], partindset[0].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[1][0], partindset[1].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[2][0], partindset[2].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[3][0], partindset[3].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[4][0], partindset[4].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[5][0], partindset[5].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[6][0], partindset[6].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[7][0], partindset[7].Count()); done.Set(); }), null);

                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[0][0], partindset[0].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[1][0], partindset[1].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[2][0], partindset[2].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[3][0], partindset[3].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[4][0], partindset[4].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[5][0], partindset[5].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[6][0], partindset[6].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[7][0], partindset[7].Count()); done.Set(); }), null);

                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[0][0], partindset[0].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[1][0], partindset[1].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[2][0], partindset[2].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[3][0], partindset[3].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[4][0], partindset[4].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[5][0], partindset[5].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[6][0], partindset[6].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[7][0], partindset[7].Count()); done.Set(); }), null);


                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN(U, N, timeStep / 2, an, bn); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN(U, M, timeStep / 2, am, bm); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN(U, H, timeStep / 2, ah, bh); done.Set(); }), null);
                done.WaitOne();

                // Heun method on state variables
                //thrN1 = new Thread(() => stateHEUN(U, N, timeStep / 2, an, bn));
                //thrM1 = new Thread(() => stateHEUN(U, M, timeStep / 2, am, bm));
                //thrH1 = new Thread(() => stateHEUN(U, H, timeStep / 2, ah, bh));

                //thrN1.Start(); thrM1.Start(); thrH1.Start();
                //done.WaitOne();
                //thrN1.Abort(); thrM1.Abort(); thrH1.Abort();

                // HEUN method on Reaction term
                reactHEUN(U, N, M, H, timeStep / 2, cap, reactConst);
            }
            else
            {
                // RK4 methd on State variables
                stateRK4(U, N, timeStep / 2, fN);
                stateRK4(U, M, timeStep / 2, fM);
                stateRK4(U, H, timeStep / 2, fH);

                
                // HEUN method on Reaction term
                reactHEUN(U, N, M, H, timeStep / 2, cap, reactConst);
            }

            ///<c>if ((i * k >= 0.015) && SomaOn) { U[0] = vstart; }</c> this checks of the somaclamp is on and sets the soma location to <c>vstart</c>
            ///if ((t * k >= 0.015) && SomaOn) { U[0] = vstart; }
            ///This part does the diffusion solve \n
            /// <c>r_csc.Multiply(U.ToArray(), b);</c> the performs the RHS*Ucurr and stores it in <c>b</c> \n
            /// <c>lu.Solve(b, b);</c> this does the forward/backward substitution of the LU solve and sovles LHS = b \n
            /// <c>U.SetSubVector(0, Neuron.vertCount, Vector.Build.DenseOfArray(b));</c> this sets the U vector to the voltage at the end of the diffusion solve
            r_csc.Multiply(U.ToArray(), b);
            lu.Solve(b, b);
            U.SetSubVector(0, Neuron.nodes.Count, Vector.Build.DenseOfArray(b));
            /// this part solves the reaction portion of the operator splitting \n
            /// <c>R.SetSubVector(0, Neuron.vertCount, reactF(reactConst, U, N, M, H, cap));</c> this first evaluates at the reaction function \f$r(V)\f$ \n
            /// <c>R.Multiply(k, R); </c> this multiplies by the time step size \n
            /// <c>U.add(R,U)</c> adds it back to U to finish off the operator splitting

            if (mthd == 0)
            {
                // FE on State Variables and Reaction Solve for half timestep
                R.SetSubVector(0, Neuron.nodes.Count, reactF(reactConst, U, N, M, H, cap));
                R.Multiply(timeStep / 2, R);
                U.Add(R, U);

                // FE on State Variables
                stateFE(U, N, timeStep/2, fN);
                stateFE(U, M, timeStep/2, fM);
                stateFE(U, H, timeStep/2, fH);                
            }
            else if (mthd == 1)
            {
                // BE on Reaction term
                reactBE(U, N, M, H, timeStep / 2, cap, reactConst);
                // BE method on State Variables
                stateBE(U, N, timeStep / 2, an, bn);
                stateBE(U, M, timeStep / 2, am, bm);
                stateBE(U, H, timeStep / 2, ah, bh);
            }
            else if (mthd ==2)
            {
                
                // HEUN method on Reaction term
                reactHEUN(U, N, M, H, timeStep / 2, cap, reactConst);

                // Heun method on state variables
                //stateHEUN(U, N, timeStep / 2, an, bn);
                //stateHEUN(U, M, timeStep / 2, am, bm);
                //stateHEUN(U, H, timeStep / 2, ah, bh);

                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[0][0], partindset[0].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[1][0], partindset[1].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[2][0], partindset[2].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[3][0], partindset[3].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[4][0], partindset[4].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[5][0], partindset[5].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[6][0], partindset[6].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, N, timeStep / 2, an, bn, partindset[7][0], partindset[7].Count()); done.Set(); }), null);

                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[0][0], partindset[0].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[1][0], partindset[1].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[2][0], partindset[2].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[3][0], partindset[3].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[4][0], partindset[4].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[5][0], partindset[5].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[6][0], partindset[6].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, M, timeStep / 2, am, bm, partindset[7][0], partindset[7].Count()); done.Set(); }), null);

                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[0][0], partindset[0].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[1][0], partindset[1].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[2][0], partindset[2].Count()); done.Set(); }), null);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[3][0], partindset[3].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[4][0], partindset[4].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[5][0], partindset[5].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[6][0], partindset[6].Count()); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN_POOL(U, H, timeStep / 2, ah, bh, partindset[7][0], partindset[7].Count()); done.Set(); }), null);

                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN(U, N, timeStep / 2, an, bn); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN(U, M, timeStep / 2, am, bm); done.Set(); }), null);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state) { stateHEUN(U, H, timeStep / 2, ah, bh); done.Set(); }), null);
                done.WaitOne();

                // Heun method on state variables
                //thrN2 = new Thread(() => stateHEUN(U, N, timeStep / 2, an, bn));
                //thrM2 = new Thread(() => stateHEUN(U, M, timeStep / 2, am, bm));
                //thrH2 = new Thread(() => stateHEUN(U, H, timeStep / 2, ah, bh));

                //thrN2.Start(); thrM2.Start(); thrH2.Start();
                //done.WaitOne();
                //thrN2.Abort(); thrM2.Abort(); thrH2.Abort();
            }
            else
            {
                // HEUN method on Reaction term
                reactHEUN(U, N, M, H, timeStep / 2, cap, reactConst);

                // RK4 methd on State variables
                stateRK4(U, N, timeStep / 2, fN);
                stateRK4(U, M, timeStep / 2, fM);
                stateRK4(U, H, timeStep / 2, fH);
                                
            }

            stopWatch.Stop();
            //ts = stopWatch.Elapsed;
            //Debug.Log("Solve Step time: " + ts.Milliseconds);
            long microseconds = stopWatch.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));
            sw.WriteLine((microseconds).ToString());
            ///<c>if ((i * k >= 0.015) && SomaOn) { U[0] = vstart; }</c> this checks of the somaclamp is on and sets the soma location to <c>vstart</c>
            ///if ((t * k >= 0.015) && SomaOn) { U[0] = vstart; }      
        }

        #region Local Functions
        static void reactHEUN(Vector V, Vector N, Vector M, Vector H, double k, double cap, List<double> reactConst)
        {
            // temporary vectors
            Vector R3 ,R2, R1, R0;
            R3 = Vector.Build.Dense(V.Count);
            R2 = Vector.Build.Dense(V.Count);
            R1 = Vector.Build.Dense(V.Count);
            R0 = Vector.Build.Dense(V.Count);

            double ek, ena, el, gk, gna, gl;
            /// this sets the constants for the conductances \n
            /// <c>gk = reactConst[0]; gna = reactConst[1]; gl = reactConst[2];</c>
            gk = reactConst[0]; gna = reactConst[1]; gl = reactConst[2];
            /// this sets constants for reversal potentials \n
            /// <c>ek = reactConst[3]; ena = reactConst[4]; el = reactConst[5];</c>
            ek = reactConst[3]; ena = reactConst[4]; el = reactConst[5];

            N.PointwisePower(4, R1); R1.Multiply(-1 * gk / cap, R1);
            M.PointwisePower(3, R2); R2.PointwiseMultiply(H, R2); R2.Multiply(-1 * gna / cap, R2);
            R1.Add(R2, R1); R1.Add(-1 * gl / cap, R1);

            N.PointwisePower(4, R0); R0.Multiply(gk * ek / cap, R0);
            M.PointwisePower(3, R2); R2.PointwiseMultiply(H, R2); R2.Multiply(gna * ena / cap, R2);
            R0.Add(R2, R0); R0.Add(gl * el / cap, R0);

            ((R1.Multiply(-1 * k / 2)).Add(1)).PointwisePower(-1, R2);
            (R1.Multiply(k / 2)).Add(1, R3);
            R3.PointwiseMultiply(V, R3); R3.Add(R0.Multiply(k), R3);
            R3.PointwiseMultiply(R2, V);
        }
        static void stateFE(Vector V,Vector S,double k, Func<Vector,Vector,Vector> f)
        {
            S.Add(f(V, S).Multiply(k), S);
        }

        static void stateBE(Vector V,Vector S,double k, Func<Vector,Vector> a, Func<Vector,Vector> b)
        {
            // temporary vectors
            Vector T1,T2;
            T1 = Vector.Build.Dense(V.Count);
            T2 = Vector.Build.Dense(V.Count);

            b(V).Add(a(V), T1); T1.Multiply(k, T1); T1.Add(1, T1);
            T1.PointwisePower(-1, T1);
            a(V).Multiply(k, T2); T2.Add(S, T2); T1.PointwiseMultiply(T2, S);
        }

        static void reactBE(Vector V, Vector N, Vector M, Vector H, double k,double cap, List<double> reactConst)
        {
            Vector T1, T2, T3, T4;
            T1 = Vector.Build.Dense(V.Count);
            T2 = Vector.Build.Dense(V.Count);
            T3 = Vector.Build.Dense(V.Count);
            T4 = Vector.Build.Dense(V.Count);

            double ek, ena, el, gk, gna, gl;
            /// this sets the constants for the conductances \n
            /// <c>gk = reactConst[0]; gna = reactConst[1]; gl = reactConst[2];</c>
            gk = reactConst[0]; gna = reactConst[1]; gl = reactConst[2];
            /// this sets constants for reversal potentials \n
            /// <c>ek = reactConst[3]; ena = reactConst[4]; el = reactConst[5];</c>
            ek = reactConst[3]; ena = reactConst[4]; el = reactConst[5];

            // BE on Reaction term
            N.PointwisePower(4, T1); T1.Multiply(-1 * gk / cap, T1);
            M.PointwisePower(3, T2); T2.PointwiseMultiply(H, T2); T2.Multiply(-1 * gna / cap, T2);
            T1.Add(T2, T3); T3.Add(-1 * gl / cap, T3); T3.Multiply(-1 * k, T3); T3.Add(1, T3); T3.PointwisePower(-1, T3);
            T1.Multiply(ek, T4); T4.Add(T2.Multiply(ena), T4); T4.Add((-1 * gl / cap) + el, T4);
            T4.Multiply(-1 * k, T4); T4.Add(V, T4);
            T4.PointwiseMultiply(T3, V);
        }

        static void stateHEUN(Vector V, Vector S, double k, Func<Vector, Vector> a, Func<Vector,Vector> b)
        {
            Vector T1, T2;
            T1 = Vector.Build.Dense(V.Count);
            T2 = Vector.Build.Dense(V.Count);

            a(V).Add(b(V), T1); T1.Multiply(k / 2, T1); T1.Add(-1, T1); T1.PointwiseMultiply(S, T1); T1.Multiply(-1, T1);
            a(V).Multiply(k, T2); T1.Add(T2, T1);

            a(V).Add(b(V), T2); T2.Multiply(k / 2, T2); T2.Add(1, T2); T2.PointwisePower(-1, T2);
            T1.PointwiseMultiply(T2, S);

            //Thread thread = Thread.CurrentThread;
            //string message = $"Background: {thread.IsBackground}, Thread Pool: {thread.IsThreadPoolThread}, Thread ID: {thread.ManagedThreadId}";
            //Debug.Log(message);
        }

        static void stateHEUN_POOL(Vector V, Vector S, double k, Func<Vector, Vector> a, Func<Vector, Vector> b, int index, int count)
        {
            Vector Vtmp, Stmp;
            Vector T1, T2;
            T1 = Vector.Build.Dense(count);
            T2 = Vector.Build.Dense(count);

            Vtmp = V.SubVector(index, count);
            Stmp = S.SubVector(index, count);

            a(Vtmp).Add(b(Vtmp), T1); T1.Multiply(k / 2, T1); T1.Add(-1, T1); T1.PointwiseMultiply(Stmp, T1); T1.Multiply(-1, T1);
            a(Vtmp).Multiply(k, T2); T1.Add(T2, T1);

            a(Vtmp).Add(b(Vtmp), T2); T2.Multiply(k / 2, T2); T2.Add(1, T2); T2.PointwisePower(-1, T2);
            T1.PointwiseMultiply(T2, Stmp);

            S.SetSubVector(index, count, Stmp);

            //Thread thread = Thread.CurrentThread;
            //string message = $"Background: {thread.IsBackground}, Thread Pool: {thread.IsThreadPoolThread}, Thread ID: {thread.ManagedThreadId}";
            //Debug.Log(message);
        }



        static void stateRK4(Vector V, Vector S, double k, Func<Vector, Vector, Vector> f)
        {
            Vector Y1, Y2, Y3;
            Y1 = Vector.Build.Dense(V.Count);
            Y2 = Vector.Build.Dense(V.Count);
            Y3 = Vector.Build.Dense(V.Count);

            S.Add(f(V, S).Multiply(k / 2), Y1);
            S.Add(f(V, Y1).Multiply(k / 2), Y2);
            S.Add(f(V, Y2).Multiply(k), Y3);

            ((((f(V, S).Add(f(V, Y1).Multiply(2))).Add(f(V, Y2).Multiply(2))).Add(f(V, Y3))).Multiply(k / 2)).Add(S, S);
        }
        /// <summary>
        /// This function sets the target time step size, below is the formula for the conduction speed of the action potential (wave speed)
        ///
        /// \f[v = \frac{1}{C}\sqrt{\frac{d}{R_a R_{mem}}}]
        ///
        /// where
        /// \f[R_{mem} = \frac{1}{g_{Na}m^3 h + g_K n^4}]
        /// 
        /// and n,m,h are the probability states which vary in time and depend on the voltage
        /// Notice that
        /// 
        /// \f[\frac{1}{R_{mem}}\leq g_{Na}+g_K=G_{mem-theor-max}]
        /// 
        /// therefore we define the max conduction speed vmax as
        /// 
        /// v_{max} = \frac{1}{C}\sqrt{\frac{d_max\cdot (g_{Na}+g_K)}{R_a}}
        /// 
        /// then we solve for our target \f[Delta t] by computing
        /// 
        /// \f[\Delta t = \frac{\Delta x}{v_{max}}]
        /// 
        /// where \f[Delta x] is the median edge length, in this case we use the average this is because our geometries are regularize and there
        /// are not excessively small edges in the graph geometry.
        ///  
        /// </summary>
        /// <param name="cap"></param> this is the capacitance
        /// <param name="maxDiameter"></param> this is the maximum diameter
        /// <param name="edgeLength"></param> this is the target edge length of the graph geometry
        /// <param name="gna"></param> this is the sodium conductance
        /// <param name="gk"></param> this is the potassium conductance
        /// <param name="res"></param> this is the axial resistance
        /// <param name="Rmemscf"></param> this is membrane resistance scale factor, since this is only a fraction of theoretical maximum
        /// <returns></returns>
        public static double SetTargetTimeStep(double cap, double maxDiameter, double edgeLength ,double gna, double gk, double res, double Rmemscf, double cfl)
        {
            /// here we set the minimum time step size and maximum time step size
            /// the dtmin is based on prior numerical experiments that revealed that for each refinement level the 
            /// voltage profiles were visually accurate when compared to Yale Neuron for delta t at least 2 microseconds
            double dtmin = 2e-6;  
            double dtmax = 32e-6;        
            /// this is where we compute the maximum conduction speed (wave speed) of the ap wave
            /// set the maxDiameter to [m] by multiplying by 1e-6
            double vmax = (1 / cap) * System.Math.Sqrt(maxDiameter * (1e-6) *Rmemscf* (gna + gk) / (res));
            /// this is the target time step size, set to seconds by multiplying by 1e-6
            double tstep = (edgeLength / vmax) * (1e-6);            
            /// we use the loop incase the time step if it is unnecessarily small
            while(tstep < dtmin){ tstep = tstep * cfl;}
            /// this avoid making the time step too big will cause numerical instability
            if (tstep > dtmax) { tstep = tstep * 0.5; }
            return tstep;
        }

        /// <summary>
        /// This function initializes the voltage vector <c>U</c> and the state vectors
        /// <c>M</c>, <c>N</c>, and <c>H</c> \n
        /// The input <c>Neuron.vertCount</c> is the vertex count of the neuron geometry \n
        /// <c>U</c> is initialized to 0 [V] for the entire cell \n
        /// <c>M</c> is initialized to \f$m_i\f$ which is set by <c>mi</c> \n
        /// <c>N</c> is initialized to \f$n_i\f$ which is set by <c>ni</c> \n
        /// <c>H</c> is initialized to \f$h_i\f$ which is set by <c>hi</c>
        /// </summary>
        private void InitializeNeuronCell()
        {
            U = Vector.Build.Dense(Neuron.nodes.Count, 0);
            M = Vector.Build.Dense(Neuron.nodes.Count, mi);
            N = Vector.Build.Dense(Neuron.nodes.Count, ni);
            H = Vector.Build.Dense(Neuron.nodes.Count, hi);
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
        /// <param name="myCell"></param> this is the <c>Neuron</c> that contains all the information about the cell geometry
        /// <param name="res"></param> this is the axial resistance
        /// <param name="cap"></param> this is the membrane capacitance
        /// <param name="k"></param> this is the fixed time step size
        /// <returns>LHS,RHS</returns> the function returns the LHS, RHS stencil matrices for the diffusion solve in sparse format, it is compressed in the main solver routine.
        public static List<CoordinateStorage<double>> makeSparseStencils(Neuron myCell, double res, double cap, double k)
        {
            /// send output matrices as a list {rhs, lhs}\n
            /// <c>List<CoordinateStorage<double>> stencils = new List<CoordinateStorage<double>>();</c> initializes empty list storage for the stencil matrices \n
            /// in this case they are of type <c>CoordinateStorage</c> \n
            List<CoordinateStorage<double>> stencils = new List<CoordinateStorage<double>>();

            /// initialize new coordinate storage \n
            /// <c>var rhs = new CoordinateStorage<double>(myCell.vertCount, myCell.vertCount, myCell.vertCount * myCell.vertCount);</c> this initializes our empty coordinate storage matrices <c>rhs</c> and <c>lhs</c>
            var rhs = new CoordinateStorage<double>(myCell.nodes.Count, myCell.nodes.Count, myCell.nodes.Count * myCell.nodes.Count);
            var lhs = new CoordinateStorage<double>(myCell.nodes.Count, myCell.nodes.Count, myCell.nodes.Count * myCell.nodes.Count);

            /// for keeping track of the neighbors of a node \n
            /// <c>List<int> nghbrlist;</c> this is for collecting the neighbor indices of the current node 
            List<int> nghbrlist;
            int nghbrLen;

            double tempEdgeLen, tempRadius, avgEdgeLengths;
            /// <c>double sumRecip = 0;</c> this is for adding the sum of reciprocals which is in our stencil scheme \n
            double sumRecip = 0;
            double scf = 1E-6;  /// 1e-6 scale factor to convert to micrometers for radii and edge length \n

            for (int j = 0; j < myCell.nodes.Count; j++)
            {
                List<double> edgelengths = new List<double>();
                /// <c>nghbrlist = myCell.nodes[j].AdjacencyList.Keys.ToList();</c> this gets the current neighbor list for node j \n
                nghbrlist = myCell.nodes[j].AdjacencyList.Keys.ToList();
                /// <c>nghbrLen = nghbrlist.Count;</c> this is the length of the neighbor list \n
                nghbrLen = nghbrlist.Count;
                sumRecip = 0;
                /// <c>tempRadius = myCell.nodeData[j].nodeRadius*scf;</c> get the current radius at node j \n
                tempRadius = myCell.nodes[j].NodeRadius*scf;

                /// in this loop we collect the edgelengths that go to node j, and we compute the coefficient given in our paper \n
                foreach (int nghbrIds in nghbrlist)
                {
                    /// <c>tempEdgeLen = myCell.nodes[j].AdjacencyList[nghbrIds]*scf;</c> get the edge length at current node j, to node neighbor p, scale to micro meters \n
                    tempEdgeLen = myCell.nodes[j].AdjacencyList[nghbrIds]*scf;
                    /// <c>edgelengths.Add(tempEdgeLen);</c> put the edge length in the list, this list of edges will have length equal to length of neighbor list \n
                    edgelengths.Add(tempEdgeLen);
                    sumRecip = sumRecip + 1 / (tempEdgeLen * tempRadius * ((1 / (myCell.nodes[nghbrIds].NodeRadius*scf* myCell.nodes[nghbrIds].NodeRadius*scf)) + (1 / (tempRadius * tempRadius))));
                }
                /// get the average edge lengths of neighbors \n
                avgEdgeLengths = edgelengths.Average();
                /// set main diagonal entries using <c>rhs.At()</c>
                rhs.At(j, j, 1 - (k * sumRecip) / (2.0 * res * cap * avgEdgeLengths));
                lhs.At(j, j, 1 + (k * sumRecip) / (2.0 * res * cap * avgEdgeLengths));
                /// set off diagonal entries by going through the neighbor list, and using <c>rhs.At()</c>
                for (int p = 0; p < nghbrLen; p++)
                {
                    rhs.At(j, nghbrlist[p], k / (2 * res * cap * tempRadius* avgEdgeLengths * edgelengths[p] * ((1 / (myCell.nodes[nghbrlist[p]].NodeRadius*scf * myCell.nodes[nghbrlist[p]].NodeRadius*scf)) + (1 / (tempRadius * tempRadius)))));
                    lhs.At(j, nghbrlist[p], -1.0 * k / (2 * res * cap * tempRadius * avgEdgeLengths * edgelengths[p] * ((1 / (myCell.nodes[nghbrlist[p]].NodeRadius*scf * myCell.nodes[nghbrlist[p]].NodeRadius*scf)) + (1 / (tempRadius * tempRadius)))));
                }
            }
            //rhs.At(0, 0, 1);
            //lhs.At(0, 0, 1);

            /// <c>stencil.Add()</c> this adds the completed stencil matrices to the output list
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
            /// initialize the output vector and prod vector these will be used to assemble the different parts of the reaction calculation \n
            /// <c>Vector output = Vector.Build.Dense(V.Count, 0.0);</c> initializes the output vector of length equation to number of entries in voltage vector, initialized to 0 \n
            /// <c>Vector prod = Vector.Build.Dense(V.Count, 0.0);</c> initializes the product vector of length equation to number of entries in voltage vector, initialized to 0 \n
            /// <c>double ek, ena, el, gk, gna, gl; </c> these are the conductances and reversal potentials that we need to assign using <c>reactConst</c> parameter that is sent \n
            Vector output = Vector.Build.Dense(V.Count, 0.0);
            Vector prod = Vector.Build.Dense(V.Count, 0.0);
            double ek, ena, el, gk, gna, gl;
            /// this sets the constants for the conductances \n
            /// <c>gk = reactConst[0]; gna = reactConst[1]; gl = reactConst[2];</c>
            gk = reactConst[0]; gna = reactConst[1]; gl = reactConst[2];
            /// this sets constants for reversal potentials \n
            /// <c>ek = reactConst[3]; ena = reactConst[4]; el = reactConst[5];</c>
            ek = reactConst[3]; ena = reactConst[4]; el = reactConst[5];
            /// <c>output.Add(prod.Multiply(gk), output);</c> this adds current due to potassium
            prod.SetSubVector(0, V.Count, NN.PointwisePower(4.0));
            prod.SetSubVector(0, V.Count, (V.Subtract(ek)).PointwiseMultiply(prod));
            output.Add(prod.Multiply(gk), output);
            /// <c>output.Add(prod.Multiply(gna), output);</c> this adds current due to sodium
            prod.SetSubVector(0, V.Count, MM.PointwisePower(3.0));
            prod.SetSubVector(0, V.Count, HH.PointwiseMultiply(prod)); prod.SetSubVector(0, V.Count, (V.Subtract(ena)).PointwiseMultiply(prod));
            output.Add(prod.Multiply(gna), output);
            /// <c>output.Add((V.Subtract(el)).Multiply(gl), output);</c> this adds leak current
            output.Add((V.Subtract(el)).Multiply(gl), output);
            /// Return the negative of the total
            output.Multiply(-1.0 / cap, output);

            return output;
        }
        /// <summary>
        /// This is the function for the right hand side of the ODE on state N, which is given by:
        /// \f[\frac{dn}{dt}=\alpha_n(V)(1-n)-\beta_n(V)n\f]
        /// </summary>
        /// <param name="V"></param> this is the current input voltage for the geometry
        /// <param name="N"></param> this is the current vector of state N for the geometry
        /// <returns>f(V,N)</returns> the function returns the right hand side of the state N ODE.
        private static Vector fN(Vector V, Vector N) { return an(V).PointwiseMultiply(1 - N) - bn(V).PointwiseMultiply(N); }
        /// <summary>
        /// This is the function for the right hand side of the ODE on state M, which is given by:
        /// \f[\frac{dm}{dt}=\alpha_m(V)(1-m)-\beta_m(V)m\f]
        /// </summary>
        /// <param name="V"></param> this is the current input voltage for the geometry
        /// <param name="M"></param> this is the current vector of state M for the geometry
        /// <returns>f(V,M)</returns> the function returns the right hand side of the state M ODE.
        private static Vector fM(Vector V, Vector M) { return am(V).PointwiseMultiply(1 - M) - bm(V).PointwiseMultiply(M); }
        /// <summary>
        /// This is the function for the right hand side of the ODE on state H, which is given by:
        /// \f[\frac{dh}{dt}=\alpha_h(V)(1-h)-\beta_h(V)h\f]
        /// </summary>
        /// <param name="V"></param> this is the current input voltage for the geometry
        /// <param name="H"></param> this is the current vector of state H for the geometry
        /// <returns>f(V,H)</returns> the function returns the right hand side of the state H ODE.
        private static Vector fH(Vector V, Vector H) { return ah(V).PointwiseMultiply(1 - H) - bh(V).PointwiseMultiply(H); }
       
        /// <summary>
        /// This is \f$\alpha_n\f$ rate function, the rate functions take the form of
        /// \f[
        /// \frac{a_p(V-B_p)}{\exp(\frac{V-B_p}{C_p})-D_p}
        /// \f]
        /// the constants \f$a_p,B_p,C_p,D_p\f$ are manually coded in for this version of the simulation\n
        /// TODO: come up with an implementation where the user can enter in their own parameters (Yale Neuron has this capability)
        /// </summary>
        /// <param name="V"></param> this is the input voltage
        /// <returns>an</returns> this function returns the rate at the given voltage
        private static Vector an(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.032) * (15.0 - Vin).PointwiseDivide(((15.0 - Vin) / 5.0).PointwiseExp() - 1.0);
        }
        /// <summary>
        /// This is \f$\beta_n\f$ rate function, the rate functions take the form of
        /// \f[
        /// \frac{a_p(V-B_p)}{\exp(\frac{V-B_p}{C_p})-D_p}
        /// \f]
        /// the constants \f$a_p,B_p,C_p,D_p\f$ are manually coded in for this version of the simulation\n
        /// TODO: come up with an implementation where the user can enter in their own parameters (Yale Neuron has this capability)
        /// </summary>
        /// <param name="V"></param> this is the input voltage
        /// <returns>bn</returns> this function returns the rate at the given voltage
        private static Vector bn(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.5) * ((10.0 - Vin) / 40.0).PointwiseExp();
        }
        /// <summary>
        /// This is \f$\alpha_m\f$ rate function, the rate functions take the form of
        /// \f[
        /// \frac{a_p(V-B_p)}{\exp(\frac{V-B_p}{C_p})-D_p}
        /// \f]
        /// the constants \f$a_p,B_p,C_p,D_p\f$ are manually coded in for this version of the simulation\n
        /// TODO: come up with an implementation where the user can enter in their own parameters (Yale Neuron has this capability)
        /// </summary>
        /// <param name="V"></param> this is the input voltage
        /// <returns>am</returns> this function returns the rate at the given voltage
        private static Vector am(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.32) * (13.0 - Vin).PointwiseDivide(((13.0 - Vin) / 4.0).PointwiseExp() - 1.0);
        }
        /// <summary>
        /// This is \f$\beta_m\f$ rate function, the rate functions take the form of
        /// \f[
        /// \frac{a_p(V-B_p)}{\exp(\frac{V-B_p}{C_p})-D_p}
        /// \f]
        /// the constants \f$a_p,B_p,C_p,D_p\f$ are manually coded in for this version of the simulation\n
        /// TODO: come up with an implementation where the user can enter in their own parameters (Yale Neuron has this capability)
        /// </summary>
        /// <param name="V"></param> this is the input voltage
        /// <returns>bm</returns> this function returns the rate at the given voltage
        private static Vector bm(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.28) * (Vin - 40.0).PointwiseDivide(((Vin - 40.0) / 5.0).PointwiseExp() - 1.0);
        }
        /// <summary>
        /// This is \f$\alpha_h\f$ rate function, the rate functions take the form of
        /// \f[
        /// \frac{a_p(V-B_p)}{\exp(\frac{V-B_p}{C_p})-D_p}
        /// \f]
        /// the constants \f$a_p,B_p,C_p,D_p\f$ are manually coded in for this version of the simulation\n
        /// TODO: come up with an implementation where the user can enter in their own parameters (Yale Neuron has this capability)
        /// </summary>
        /// <param name="V"></param> this is the input voltage
        /// <returns>ah</returns> this function returns the rate at the given voltage
        private static Vector ah(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.128) * ((17.0 - Vin) / 18.0).PointwiseExp();
        }
        /// <summary>
        /// This is \f$\beta_h\f$ rate function, the rate functions take the form of
        /// \f[
        /// \frac{a_p(V-B_p)}{\exp(\frac{V-B_p}{C_p})-D_p}
        /// \f]
        /// the constants \f$a_p,B_p,C_p,D_p\f$ are manually coded in for this version of the simulation\n
        /// TODO: come up with an implementation where the user can enter in their own parameters (Yale Neuron has this capability)
        /// </summary>
        /// <param name="V"></param> this is the input voltage
        /// <returns>bh</returns> this function returns the rate at the given voltage
        private static Vector bh(Vector V)
        {
            Vector Vin = Vector.Build.DenseOfVector(V);
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * 4.0 / (((40.0 - Vin) / 5.0).PointwiseExp() + 1.0);
        }

        private static List<List<int>> partition(int [] array,int nparts)
        {
            List<List<int>> parlist = new List<List<int>>();
            List<int> tlist = new List<int>();

            int psize = (int)(array.Count() / nparts);
            for(int i=0; i<array.Count(); i++)
            {
                tlist.Add(array[i]);
                if (array[i] == 0){ continue;}
                if( (array[i]%psize) == 0 || (i==array.Count()-1) )
                {                    
                    parlist.Add(tlist);
                    tlist = new List<int>();   
                }   
            }
            return parlist;
        }
        #endregion
    }
}