using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
/// These libraries are for using the Vector data type
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
/// These are for the sparse solving functionality
using CSparse.Storage;
using CSparse.Double.Factorization;
using CSparse;
using C2M2.Utils;
using C2M2.NeuronalDynamics.UGX;
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
        private double res = 300.0 * 1.0E-2;
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
        private double Rmemscf = 1.0;
        /// <summary>
        /// this is the scale factor for increasing the time step size if it is unnecessarily small
        /// </summary>
        private double cfl = 1.0;        

        /// <summary>
        /// These are the solution vectors for the voltage <code>U</code>
        /// the state <c>M</c>, state <c>N</c>, and state <c>H</c>
        /// </summary>
        private Vector U;
        /// <summary>
        /// This is the U that gets modified during the step before U is set to it. TODO remove this when U is moved to NDSimulation
        /// </summary>
        private Vector U_Active;
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

        private Vector Upre, Npre, Mpre, Hpre;
        private Vector ej, rj, ZZ, YY;
        private double[] z, y;
        private double[] bj;

        /// <summary>
        /// Send simulation 1D values, this send the current voltage after the solve runs 1 iteration
        /// it passes <c>curVals</c>
        /// </summary>
        /// <returns>curVals</returns>
        public override double[] Get1DValues()
        {
            /// this initialize the curvals which will be sent back to the VR simulation
            double[] curVals = null;
            /// check if this beginning of the simulation
            if (curentTimeStep > -1)
            {
                Vector curTimeSlice;
                lock (visualizationValuesLock)
                {
                    /// define the current time slice to send and initialize it to the correct size which is the number of vertices in the geometry
                    /// initialize it to the current state of the voltage, this is the voltage we are sending back to vr simulation
                    curTimeSlice = U.SubVector(0, Neuron.nodes.Count);
                }
                //curTimeSlice.Multiply(1, curTimeSlice);

                curVals = curTimeSlice.ToArray();
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
            foreach (Tuple<int, double> newVal in newValues)
            {
                if (newVal != null)
                {
                    
                    if (newVal.Item1 >= 0 && newVal.Item1 < Neuron.nodes.Count)
                    {
                        bj = new double[Neuron.nodes.Count];
                        z = new double[Neuron.nodes.Count];
                        y = new double[Neuron.nodes.Count];

                        R.At(newVal.Item1, newVal.Item2);
                        ej = Vector.Build.Dense(Neuron.nodes.Count, 0.0);
                        ej.At(newVal.Item1, 1.0);
                        (l_csc.Transpose()).Multiply(ej.ToArray(), bj);
                        rj = Vector.Build.DenseOfArray(bj);
                        rj.At(newVal.Item1, rj[newVal.Item1] - 1);

                        lu.Solve(ej.ToArray(), z);
                        lu.Solve(R.ToArray(), y);

                        ZZ = Vector.Build.DenseOfArray(z);
                        YY = Vector.Build.DenseOfArray(y);

                        YY.Add(ZZ.Multiply(rj.DotProduct(YY) / (1 - rj.DotProduct(ZZ))), U_Active);
                    }
                }
            }
        }

        /// <summary>
        /// Receives 1D information for synaptic communication
        /// newValues =(index, and current value in Amps)
        /// </summary>
        /// <param name="newValues"></param>
        public override void SetSynapseCurrent(Tuple<int, double>[] newValues)
        {
            double area = new float();

            foreach (Tuple<int, double> newVal in newValues)
            {
                if (newVal != null)
                {
                    if (newVal.Item1 >= 0 && newVal.Item1 < Neuron.nodes.Count)
                    {
                        area = 2 * System.Math.PI * Neuron.nodes[newVal.Item1].NodeRadius * Neuron.TargetEdgeLength * 1e-12;

                        U_Active[newVal.Item1] = U_Active[newVal.Item1] + timeStep * newVal.Item2 / (cap * area);
                    }
                }
            }
        }

        private Vector R;                                 //This is a vector for the reaction solve 
        private double[] b;                               //This is the right hand side vector when solving Ax = b
        private Vector tempState;
        List<double> reactConst;                            //This is for passing the reaction function constants
        List<CoordinateStorage<double>> sparse_stencils;    
        CompressedColumnStorage<double> r_csc;              //This is for the rhs sparse matrix
        CompressedColumnStorage<double> l_csc;              //This is for the lhs sparse matrix
        private SparseLU lu;                                //Initialize the LU factorizaation

        /// <summary>
        /// This is a small routine call to initialize the Neuron Cell
        /// this will initialize the solution vectors which are <c>U</c>, <c>M</c>, <c>N</c>, and <c>H</c>
        /// </summary>
        protected override void PreSolve()
        {
            InitializeNeuronCell();
            ///<c>R</c> this is the reaction vector for the reaction solve
            R = Vector.Build.Dense(Neuron.nodes.Count);
            ej = Vector.Build.Dense(Neuron.nodes.Count);
            rj = Vector.Build.Dense(Neuron.nodes.Count);
            ZZ = Vector.Build.Dense(Neuron.nodes.Count);
            YY = Vector.Build.Dense(Neuron.nodes.Count);
            tempState = Vector.Build.Dense(Neuron.nodes.Count, 0);
            ///<c>reactConst</c> this is a small list for collecting the conductances and reversal potential which is sent to the reaction solve routine
            reactConst = new List<double> { gk, gna, gl, ek, ena, el };

            /// this sets the target time step size
            timeStep = SetTargetTimeStep(cap, 2 * Neuron.MaxRadius,2*Neuron.MinRadius, Neuron.TargetEdgeLength, gna, gk,gl, res, Rmemscf, 1.0);
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
            U_Active.Multiply(4.0 / 3.0, R);
            R.Add(reactF(reactConst, U_Active, N, M, H, cap).Multiply((4.0 / 3.0) * timeStep), R);
            R.Add(Upre.Multiply(-1.0 / 3.0), R);
            R.Add(reactF(reactConst, Upre, Npre, Mpre, Hpre, cap).Multiply((-2.0 / 3.0) * timeStep), R);

            lu.Solve(R.ToArray(), b);

            tempState = N.Clone();
            N.Add(fN(U_Active, N).Multiply(timeStep), N); N.Multiply(4.0 / 3.0, N);
            N.Add(Npre.Multiply(-1.0 / 3.0), N); N.Add(fN(Upre, Npre).Multiply(-2.0 * timeStep / 3.0), N);
            Npre = tempState.Clone();

            tempState = M.Clone();
            M.Add(fM(U_Active, M).Multiply(timeStep), M); M.Multiply(4.0 / 3.0, M);
            M.Add(Mpre.Multiply(-1.0 / 3.0), M); M.Add(fM(Upre, Mpre).Multiply(-2.0 * timeStep / 3.0), M);
            Mpre = tempState.Clone();

            tempState = H.Clone();
            H.Add(fH(U_Active, H).Multiply(timeStep), H); H.Multiply(4.0 / 3.0, H);
            H.Add(Hpre.Multiply(-1.0 / 3.0), H); H.Add(fH(Upre, Hpre).Multiply(-2.0 * timeStep / 3.0), H);
            Hpre = tempState.Clone();

            Upre = U_Active.Clone();
            U_Active.SetSubVector(0, Neuron.nodes.Count, Vector.Build.DenseOfArray(b));
        }

        internal override void SetOutputValues()
        {
            lock (visualizationValuesLock) U = U_Active.Clone();

        }

        #region Local Functions

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
        public static double SetTargetTimeStep(double cap, double maxDiameter, double minDiameter,double edgeLength ,double gna, double gk, double gl, double res, double Rmemscf, double cfl)
        {
            /// here we set the minimum time step size and maximum time step size
            /// the dtmin is based on prior numerical experiments that revealed that for each refinement level the 
            /// voltage profiles were visually accurate when compared to Yale Neuron for delta t at least 2 microseconds
            /// we want to avoid using dtmin; therefore I compute the upper bound (and lower bound for reference)
            //double dtmin = 2e-6;  
            double dtmax = 50e-6;
            double dt;

            double gll = gl; double scf = 1E-6; // to convert to micrometer of edgelengths and radii don't forget this!!!!

            // what happens if the leak conductance is 0
            if (gll == 0.0) { gll = 1.0; }
            
            double upper_bound = cap * edgeLength*scf * System.Math.Sqrt(res / (gll*minDiameter*scf));
            //double lower_bound = cap * edgeLength*scf * System.Math.Sqrt(res / (gna + gk + gl) * maxDiameter*scf);
            //GameManager.instance.DebugLogSafe("upper_bound = " + upper_bound.ToString());

            // some cells may have an upper bound that is too large for the solver, so choose the smaller of the two dtmax or upper_bound
            dt = System.Math.Min(upper_bound,dtmax);
            //GameManager.instance.DebugLogSafe("lower_bound = " + lower_bound.ToString());
            return dt;       
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
            lock (visualizationValuesLock) U = Vector.Build.Dense(Neuron.nodes.Count, 0);
            U_Active = U.Clone();
            Upre = U_Active.Clone();

            M = Vector.Build.Dense(Neuron.nodes.Count, mi);
            N = Vector.Build.Dense(Neuron.nodes.Count, ni);
            H = Vector.Build.Dense(Neuron.nodes.Count, hi);
            Mpre = M.Clone(); Npre = N.Clone(); Hpre = H.Clone();
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
                /// this is BE method, no oscillations but not as accurate!
                rhs.At(j, j, 1.0);
                lhs.At(j, j, 1 + ((2.0/3.0)*k * sumRecip) / (1.0 * res * cap * avgEdgeLengths));

                /// This is for CN method, this will cause oscillations and flickering!
                //rhs.At(j, j, 1 - (k * sumRecip) / (2.0 * res * cap * avgEdgeLengths));
                //lhs.At(j, j, 1 + (k * sumRecip) / (2.0 * res * cap * avgEdgeLengths));
                /// set off diagonal entries by going through the neighbor list, and using <c>rhs.At()</c>
                for (int p = 0; p < nghbrLen; p++)
                {
                    // this is for CN method, notice the factor of 2
                    //rhs.At(j, nghbrlist[p], k / (2 * res * cap * tempRadius* avgEdgeLengths * edgelengths[p] * ((1 / (myCell.nodes[nghbrlist[p]].NodeRadius*scf * myCell.nodes[nghbrlist[p]].NodeRadius*scf)) + (1 / (tempRadius * tempRadius)))));
                    lhs.At(j, nghbrlist[p], -1.0*(2.0/3.0) * k / (1.0 * res * cap * tempRadius * avgEdgeLengths * edgelengths[p] * ((1 / (myCell.nodes[nghbrlist[p]].NodeRadius*scf * myCell.nodes[nghbrlist[p]].NodeRadius*scf)) + (1 / (tempRadius * tempRadius)))));
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
        #endregion
    }
}