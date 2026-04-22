using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using SysMath = System.Math;
using UnityEngine;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using CSparse.Storage;
using CSparse.Double.Factorization;
using CSparse;
using C2M2.Utils;
using C2M2.NeuronalDynamics.UGX;

using System.IO;

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
    /// The solver currently uses SBDF2 (semi-implicit) Backward Difference 2
    /// The solver takes into account the non-uniform radii of the geometry and the non-uniform edgelength of the geometry
    /// 
    /// These parameters need to be made available to the user to modify for their particular simulation parameters
    /// The rate functions are defined at the end
    /// Note: Very important --> ALL UNITS FOR THE SOLVER ARE IN MKS, therefore when modifying the color bars ranges, and raycast/clamp hit values
    /// they need to be in [V] not [mV] so if you intend to use 50 [mV] it needs to be coded as 0.05 [V]
    /// 
    /// The simulation parameters are defined first, initial voltage hit value for raycasting, the endTime, time step size (k)
    /// Also other options are defined here such as having the SomaOn for clamping the soma on for voltage clamp tests (this is 
    /// mostly used for verifying the voltage output against Yale Neuron).
    /// </summary>

    //tex: Below are the Hodgkin Huxley equations
    //$$\frac{a}{2R}\frac{\partial^2V}{\partial x^2}=C\frac{\partial V}{\partial t}+\bar{g}_{K}n^4(V-V_k)+\bar{g}_{Na}m^3h(V-V_{Na})+\bar{g}_l(V-V_l)$$
    //$$\frac{dn}{dt}=\alpha_n(V)(1-n)-\beta_n(V)n$$
    //$$\frac{dm}{dt}=\alpha_m(V)(1-m)-\beta_m(V)m$$
    //$$\frac{dh}{dt}=\alpha_h(V)(1-h)-\beta_h(V)h$$

    public class SparseSolverTestv1 : NDSimulation
    {
        ///<summary>
        /// This is the voltage for the voltage clamp, this is primarily used for when we do the convergence analysis of the code using a 
        /// soma clamp at 50 [mV], the units for voltage in the solver is [V] that is why <c>vstart</c> is set to 0.05
        ///</summary>
        public double vstart = 0.050;
        ///<summary>
        /// This is the starting voltage of the cells. All indices of U (Voltage) are intialized to the startingVoltage quantity.
        /// -0.05 [V] equates to -50 mV.
        ///</summary>
        public double startingVoltage = -50.0 * 1.0E-3;
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
        /// leakConductance is used when setting the target time step. This value is updated in InitializeIonChannel 
        /// </summary>
        private double leakConductance = 0.0;
        /// <summary>
        /// These are the solution vectors for the voltage <code>U</code>
        /// </summary>
        private Vector U;
        /// <summary>
        /// This is the U that gets modified during the step before U is set to it.
        /// </summary>
        private Vector U_Active;
        /// <summary>
        /// this is for storing previous states
        /// </summary>
        private Dictionary<string, Vector> currentStates;
        /// <summary>
        /// This is for the synaptic current. It contains:
        ///     [0]: The current at the active time step.
        ///     [1]: The current at the previous time step.
        /// </summary>
        private List<Vector> Isyn;
        /// <summary>
        /// The spatial scaling at post-synaptic location for time stepping (1/(cap * area))
        /// </summary>
        private Vector surfaceArea;
        /// <summary>
        /// this is for storing previous states
        /// </summary>
        private Dictionary<string, Vector> previousStates;
        private Vector Upre;
        /// <summary>
        /// This is a vector the Reaction terms
        /// </summary>
        private Vector R;       
        /// <summary>
        /// This is an array for the right hand side of the problem Ax = b
        /// </summary>
        private double[] b;            
        /// <summary>
        /// Declaration for the list of IonChannels
        /// </summary>
        public List<IonChannel> ionChannels;
        /// <summary>
        /// Declaration for the list of active IonChannels
        /// </summary>
        public List<IonChannel> activeIonChannels;
        /// <summary>
        /// Total conductance of all channels
        /// Used in SetTargetTimestep
        /// </summary>
        public double totalConductance = 0;
        /// <summary>
        /// Temporary state vector
        /// </summary>
        private Vector tempState;
        const double R_universal = 8.31431;   // J/(mol*K)
        const double F_universal = 96485;   // C/mol

        List<double> reactConst;                            //This is for passing the reaction function constants
        List<CoordinateStorage<double>> sparse_stencils;
        CompressedColumnStorage<double> r_csc;              //This is for the rhs sparse matrix
        CompressedColumnStorage<double> l_csc;              //This is for the lhs sparse matrix
        private SparseLU SBDF_implicit_decomp;

        /// <summary>
        /// Send simulation 1D values, this send the current voltage after the solve runs 1 iteration
        /// it passes <c>curVals</c>
        /// </summary>
        /// <returns>curVals</returns>
        public override double[] Get1DValues()
        {
            /// this initialize the curVals which will be sent back to the VR simulation
            double[] curVals = null;
            /// check if this beginning of the simulation
            if (currentTimeStep > -1)
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
        public override void Set1DValues((int, double)[] newValues)
        {
            foreach ((int, double) newVal in newValues)
            {
                if (newVal.Item1 >= 0 && newVal.Item1 < Neuron.nodes.Count)
                {
                    // perform a rank1 update solve to properly update with added dirichelet boundary conditions
                    // from a raycast, or voltage clamp. This is done because with a voltage clamp you are imposing
                    // a dirichelet B.C. which requires solving an updated diffusion problem with identity rows.
                    U_Active = Vector.Build.DenseOfVector(DircheletRank1UpdateSolve(newVal));
                }
            }
        }

        /// <summary>
        /// this perform a Rank1UpdateSolve to properly adjust for added dirichelet boundary conditions.
        /// </summary>
        /// <param name="newVal"></param>
        /// <returns></returns>
        public Vector DircheletRank1UpdateSolve((int, double) newVal)
        {
            double[] bj = new double[Neuron.nodes.Count];
            double[] z = new double[Neuron.nodes.Count];
            double[] y = new double[Neuron.nodes.Count];
                        
            Vector ej = Vector.Build.Dense(Neuron.nodes.Count, 0.0);
            Vector rj = ej.Clone();
            Vector ZZ = ej.Clone();
            Vector YY = ej.Clone();

            R.At(newVal.Item1, newVal.Item2);
            ej = Vector.Build.Dense(Neuron.nodes.Count, 0.0);
            ej.At(newVal.Item1, 1.0);
            (l_csc.Transpose()).Multiply(ej.ToArray(), bj);
            rj = Vector.Build.DenseOfArray(bj);
            rj.At(newVal.Item1, rj[newVal.Item1] - 1);

            SBDF_implicit_decomp.Solve(ej.ToArray(), z);
            SBDF_implicit_decomp.Solve(R.ToArray(), y);
            
            ZZ = Vector.Build.DenseOfArray(z);
            YY = Vector.Build.DenseOfArray(y);

            return YY.Add(ZZ.Multiply(rj.DotProduct(YY) / (1 - rj.DotProduct(ZZ))));
        }

        /// <summary>
        /// Receives 1D information for synaptic communication
        /// newValues = is a list of (presynapse, postsynapse)
        /// </summary>
        /// <param name="newValues"></param>
        internal override void SetSynapseCurrent(List<(Synapse,Synapse)> newValues)
        {
            List<double> tmp = new List<double>();

            // iterate through teach (pre,post) synapse pair
            foreach ((Synapse,Synapse) newVal in newValues)
            {
                if ((newVal.Item1 != null) && (newVal.Item2 != null))
                {
                    if (newVal.Item1.FocusVert >= 0 && newVal.Item1.FocusVert < Neuron.nodes.Count && newVal.Item2.FocusVert >= 0 && newVal.Item2.FocusVert < Neuron.nodes.Count)
                    {
                        //tmp[0] is current synaptic state, and tmp[1] is previous synaptic state
                        tmp = SynapseCurrentFunction(newVal, newVal.Item1.currentModel.Value);
                        Isyn[0][newVal.Item2.FocusVert] += tmp[0];
                        Isyn[1][newVal.Item2.FocusVert] += tmp[1];
                        // compute surface area at postsynaptic location and scale for timestepping use
                        surfaceArea[newVal.Item2.FocusVert] = 1 / (cap * 2 * System.Math.PI * Neuron.nodes[newVal.Item2.FocusVert].NodeRadius * Neuron.TargetEdgeLength * 1e-12);
                    }
                }
            }
        }

        /// <summary>
        /// This computes the explicit update for the Isynaptic current
        /// the input is a tuple (presyn, postsyn) = (item1, item2) respectively
        /// each synapse contains information
        /// item1.nodeindex = index on the 1d geometry
        /// item1.voltage = voltage at that node
        /// </summary>
        /// <param name="newVal"></param>
        /// <returns></returns>
        public double SynapseExplicitSBDF((Synapse, Synapse) newVal)
        {
            double area = new double();
            List<double> Icurrs = new List<double>();

            // compute surface area at postsynaptic location
            area = 2 * System.Math.PI * Neuron.nodes[newVal.Item2.FocusVert].NodeRadius * Neuron.TargetEdgeLength * 1e-12;
            // Debug.Log($"Area = {area}");

            //Icurrs[0] is current synaptic state, and Icurrs[1] is previous synaptic state
            Icurrs = SynapseCurrentFunction(newVal, newVal.Item1.currentModel.Value);

            // If the user should use unrealistic biological parameters, this will check the current and set the current appropriately if the current goes beyond
            // biologically accurate currents
            // The upper bound has been chosen to be an arbitrarily large value of 30 nano Siemens. Since this is larger than any of the max capacitance for each synapse,
            // Current should not be greater than this under normal circumstances.
            if (Double.IsNaN(Icurrs[0]) || Double.IsNaN(Icurrs[1]) || (Icurrs[0] > 30e-9) || (Icurrs[1] > 30e-9))
            {
                Debug.Log("CURRENT OUT OF RANGE");
                Icurrs[0] = 1.0e-16; Icurrs[1] = 0.9e-16;
            }

            // this is the SBDF calculation using the Icurr of the current state, and Icurr of the previous state
            return (2.0 / 3.0) * timeStep / (cap * area) * (2.0 * Icurrs[0] - Icurrs[1]);
        }


        /// <summary>
        /// This is the synaptic current function
        /// the input is a tuple (presyn, postsyn) = (item1, item2) respectively
        /// each synapse contains information
        /// item1.nodeindex = index on the 1d geometry
        /// item1.voltage = voltage at that node
        /// </summary>
        /// <param name="newVal"></param>
        /// <returns></returns>
        public bool voltageClampMode = false;
        public bool stimClamp = false;
        double stimDelay = 50e-3;
        double stimDuration = 100e-3; // 400 ms duration
        // double stimAmplitude = 0.014e-9;
        double stimAmplitude = 0.15e-11;
        // double stimAmplitude = 0.011535e-9;
        public List<double> SynapseCurrentFunction((Synapse, Synapse) newVal, ISynapseModel model)
        {
            //List contains the current synaptic current at index 0 and previous synaptic current at index 1
            List<double> Icurrs = new List<double>();

            // Explanation of local variables:
            // newVal is the (Synapse, Synapse) pair that refers to the superstructure of synapse
            // Item1 refers to the presynaptic node, Item2 refers to the postsynaptic node
            // Calling Item1.simulation grabs the SparseSolver attached to the neuron containing the presynaptic node
            // From here, we either use .Get1DValues() for current timestep Vm array, or getUpre for previous timestep Vm array
            // newVal.Item1.FocusVert refers to the index of the node on the neuron which the pre- or postsynapse is placed
            // Since getUpre() isn't a virtual method declared in the abstract class NDSimulation.cs, the solver obtained
            // from the presynaptic neuron must be cast as a SparseSolverTestv1 class

            // get the pre-synaptic voltage at current and previous timeStep
            double presynVoltage = newVal.Item1.simulation.Get1DValues()[newVal.Item1.FocusVert];
            double presynVoltagePrev = ((SparseSolverTestv1)newVal.Item1.simulation).getUpre()[newVal.Item1.FocusVert];

            if (model.isActive(presynVoltage, presynVoltagePrev, GetSimulationTime(), newVal.Item1.ActivationTime))
            {
                newVal.Item1.ActivationTime = GetSimulationTime();
            }

            //Adds the synaptic currents for the current and previous timesteps
            Icurrs.Add(model.getModelCurrent(presynVoltage, GetSimulationTime(), newVal.Item1.ActivationTime));
            Icurrs.Add(model.getModelCurrent(presynVoltagePrev, GetSimulationTime() - timeStep, newVal.Item1.ActivationTime));


            return Icurrs;
        }

        /* ******** */
        //Test print output files
        // Track the log file path for reactF output
        // just vertex 999
        private string vertex999LogPath;
        private StreamWriter vertex999LogWriter;
        private bool enableVertex999Logging = true;
        private const int TARGET_VERTEX = 999;


        /// <summary>
        /// This is a small routine call to initialize the Neuron Cell
        /// this will initialize the solution vectors which are <c>U</c>, <c>M</c>, <c>N</c>, and <c>H</c>
        /// </summary>
        protected override void PreSolve()
        {
            GameManager g = GameManager.instance;
            // if loading, the values from file will be set in BuildVectors and Set1DValues
            if (!g.Loading) 
            {
                InitializeNeuronCell();
            }
            else BuildVectors(g.U, g.Upre, g.currentStates, g.previousStates);

            ///<c>R</c> this is the reaction vector for the reaction solve
            R = Vector.Build.Dense(Neuron.nodes.Count);

            tempState = Vector.Build.Dense(Neuron.nodes.Count, 0.0);

            // Debug.Log($"Max Radius = {Neuron.MaxRadius}, min radius = {Neuron.MinRadius}");

            double r_um = Neuron.nodes[0].NodeRadius;
            double L_um = Neuron.TargetEdgeLength;
            double A_cyl = 2.0*System.Math.PI*r_um*L_um*1e-12;
            Debug.Log($"Soma patch area (cyl) = {A_cyl} m^2, r = {r_um} µm, L = {L_um} µm");

            /// this sets the target time step size
            // timeStep = SetTargetTimeStep(cap, 2 * Neuron.MaxRadius, 2 * Neuron.MinRadius, Neuron.TargetEdgeLength, activeIonChannels, res, 1.0);
            timeStep = SetTargetTimeStep(cap, 2 * Neuron.MaxRadius,2*Neuron.MinRadius, Neuron.TargetEdgeLength, totalConductance ,leakConductance, res, 1.0);
            //UnityEngine.Debug.Log("Target Time Step = " + timeStep);

            ///<c>List<CoordinateStorage<double>> sparse_stencils = makeSparseStencils(Neuron, res, cap, k);</c> Construct sparse RHS and LHS in coordinate storage format, no zeros are stored \n
            /// <c>sparse_stencils</c> this is a list which contains only two matrices the LHS and RHS matrices for the Crank-Nicolson solve
            sparse_stencils = makeSparseStencils(Neuron, res, cap, timeStep);
            ///<c>CompressedColumnStorage</c> call Compresses the sparse matrices which are stored in <c>sparse_stencils[0]</c> and <c>sparse_stencils[1]</c>
            r_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[0]); //null;
            l_csc = CompressedColumnStorage<double>.OfIndexed(sparse_stencils[1]); //null;
            ///<c>double [] b</c> we define storage for the diffusion solve part
            b = new double[Neuron.nodes.Count];
            ///<c>var lu = SparseLU.Create(l_csc, ColumnOrdering.MinimumDegreeAtA, 0.1);</c> this creates the LU decomposition of the HINES matrix which is defined by <c>l_csc</c>
            SBDF_implicit_decomp = SparseLU.Create(l_csc, ColumnOrdering.MinimumDegreeAtA, 0.1);
            

            /* ******* */
            //test print file
            // Initialize reactF logging file
            if (enableVertex999Logging)
            {
                try
                {
                    string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                    string outputDir = Path.Combine(documentsPath, "Neuro-VISOR_Logs");
                    
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    
                    string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    vertex999LogPath = Path.Combine(outputDir, $"reactF_vertex999_{timestamp}.txt");
                    
                    vertex999LogWriter = new StreamWriter(vertex999LogPath, false);
                    vertex999LogWriter.WriteLine("TimeStep,SimulationTime,ReactF_Vertex999_Value");
                    vertex999LogWriter.Flush();
                    
                    Debug.Log($"Vertex 999 logging initialized to: {vertex999LogPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to initialize vertex 999 logging: {e.Message}");
                    enableVertex999Logging = false;
                }
            }


        }

        /// <summary>
        /// This is the main solver, it is running on it own thread.
        /// The solver using SBDF2 for time steping, the implicit part is used for the diffusion and the explicit
        /// is for the reaction terms and state variables
        /// </summary>     
        protected override void SolveStep(int t)
        {            
            U_Active.Multiply(4.0 / 3.0, R);

            //test print file
            // capture reactF output and log vertex 999

            Vector reactF_current = reactF(activeIonChannels, U_Active, currentStates, cap);
            if (enableVertex999Logging && vertex999LogWriter != null && TARGET_VERTEX < reactF_current.Count)
            {
                try
                {
                    vertex999LogWriter.WriteLine($"{t},{GetSimulationTime():E6},{reactF_current[TARGET_VERTEX]:E10}");
                    
                    // Flush every 10 timesteps
                    if (t % 10 == 0)
                    {
                        vertex999LogWriter.Flush();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error writing vertex 999 log: {e.Message}");
                }
            }


            R.Add(reactF(activeIonChannels, U_Active, currentStates, cap).Multiply((4.0 / 3.0) * timeStep), R);
            R.Add(Upre.Multiply(-1.0 / 3.0), R);
            R.Add(reactF(activeIonChannels, Upre, previousStates, cap).Multiply((-2.0 / 3.0) * timeStep), R);

            var Rsyn = Vector.Build.Dense(Neuron.nodes.Count, 0.0);
            Rsyn.Add (Isyn[0].PointwiseMultiply(surfaceArea.Multiply(timeStep)), Rsyn);
            Rsyn.Multiply (4.0 / 3.0, Rsyn);
            Rsyn.Add (Rsyn.Multiply (-1.0 / 3.0), Rsyn);
            Rsyn.Add (Isyn[1].PointwiseMultiply (surfaceArea.Multiply (-2.0 * timeStep / 3.0)), Rsyn);

            // reset synaptic source this ensures that when you remove the synapse that Isyn becomes 0; 
            // therefore, current is not being sent to postsynapse once synapse is removed
            Isyn[0].Multiply(0.0, Isyn[0]);
            Isyn[1].Multiply(0.0, Isyn[1]);
            surfaceArea.Multiply(0.0, surfaceArea);
            R.Add(Rsyn, R);
            SBDF_implicit_decomp.Solve(R.ToArray(), b);


            foreach (var channel in activeIonChannels)
            {
                foreach (var gatingVariable in channel.GatingVariables)
                {
                    if (!gatingVariable.IsInstant){
                        tempState = currentStates[gatingVariable.Name].Clone();

                        var alphaNowVec = gatingVariable.Alpha(U_Active);
                        var betaNowVec = gatingVariable.Beta(U_Active);

                        // for (int i = 0; i < alphaNowVec.Count; i++)
                        for (int i = 0; i < 2; i++)
                        {
                            double Vnow = U_Active[i];
                            double Vprev = Upre[i];

                            if (double.IsNaN(alphaNowVec[i]) || double.IsInfinity(alphaNowVec[i]))
                            {
                                double alphaNow = gatingVariable.Alpha(U_Active)[i];
                                double alphaPrev = gatingVariable.Alpha(Upre)[i];

                                Debug.LogError(
                                    $"[ALPHA-BLOWUP] GV={gatingVariable.Name}\n" +
                                    $"  V_now={Vnow},  V_prev={Vprev}\n" +
                                    $"  Alpha_now={alphaNow}, Alpha_prev={alphaPrev}"
                                );
                            }

                            if (double.IsNaN(betaNowVec[i]) || double.IsInfinity(betaNowVec[i]))
                            {
                                double betaNow = gatingVariable.Beta(U_Active)[i];
                                double betaPrev = gatingVariable.Beta(Upre)[i];

                                Debug.LogError(
                                    $"[BETA-BLOWUP] GV={gatingVariable.Name}\n" +
                                    $"  V_now={Vnow},  V_prev={Vprev}\n" +
                                    $"  Beta_now={betaNow}, Beta_prev={betaPrev}"
                                );
                            }
                        }

                        // for (int i = 0; i < alphaNowVec.Count; i++)
                        // {
                            
                        //     if (double.IsNaN(alphaNowVec[i]) || double.IsInfinity(alphaNowVec[i]))
                        //     {
                        //         Debug.LogError($"[ALPHA-BLOWUP] in gating variable {gatingVariable.Name}, V={U_Active[i]}, " + $"expTerm1={1.0 / (1.0 + SysMath.Exp((U_Active[i] + Vx + 81.0) / 4.0))}, " + $"expTerm2={(30.8 + 211.4 + SysMath.Exp((U_Active[i] + Vx + 113.2) / 5.0)) / (3.7 * (1.0 + SysMath.Exp((U_Active[i] + Vx + 84.0) / 3.2)))}");
                        //     }
                        //     if (double.IsNaN(betaNowVec[i]) || double.IsInfinity(betaNowVec[i]))
                        //     {
                        //         Debug.LogError($"[BETA-BLOWUP] in gating variable {gatingVariable.Name}, V={U_Active[i]}, " + $"expTerm1={1.0 / (1.0 + SysMath.Exp((U_Active[i] + Vx + 81.0) / 4.0))}, " + $"expTerm2={(30.8 + 211.4 + SysMath.Exp((U_Active[i] + Vx + 113.2) / 5.0)) / (3.7 * (1.0 + SysMath.Exp((U_Active[i] + Vx + 84.0) / 3.2)))}");
                        //     }
                        // }

                        // Use stateexplicitSBDF2 to update the gating variable
                        stateexplicitSBDF2(
                            currentStates[gatingVariable.Name],
                            previousStates[gatingVariable.Name],
                            fS(currentStates[gatingVariable.Name], gatingVariable.Alpha(U_Active), gatingVariable.Beta(U_Active)),
                            fS(previousStates[gatingVariable.Name], gatingVariable.Alpha(Upre), gatingVariable.Beta(Upre)),
                            timeStep
                        );

                        // Update previous state for the next time step
                        previousStates[gatingVariable.Name] = tempState.Clone();
                    }
                }
            }
            Upre = U_Active.Clone();

            U_Active.SetSubVector(0, Neuron.nodes.Count, Vector.Build.DenseOfArray(b));
                       
        }

        // test print
        //Helper method to log reactF output
        /*
        private void LogReactFOutput(int timeStep, double simulationTime, Vector reactFOutput)
        {
            if (reactFLogWriter == null) return;
            
            try
            {
                reactFLogWriter.Write($"{timeStep},{simulationTime:E6}");
                double[] outputArray = reactFOutput.ToArray();
                foreach (double value in outputArray)
                {
                    reactFLogWriter.Write($",{value:E10}");
                }
                reactFLogWriter.WriteLine();
                
                // Flush every 10 timesteps to ensure data is written
                if (timeStep % 10 == 0)
                {
                    reactFLogWriter.Flush();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error writing to reactF log: {e.Message}");
            }
        }
        
        // NEW: Cleanup method - call this when simulation ends
        public void CloseReactFLog()
        {
            if (reactFLogWriter != null)
            {
                try
                {
                    reactFLogWriter.Flush();
                    reactFLogWriter.Close();
                    reactFLogWriter.Dispose();
                    Debug.Log($"ReactF log file closed: {reactFLogPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error closing reactF log: {e.Message}");
                }
            }
        }
        */

        //test print file
        // cleanup method - call this when simulation ends

        public void CloseVertex999Log()
        {
            if (vertex999LogWriter != null)
            {
                try
                {
                    vertex999LogWriter.Flush();
                    vertex999LogWriter.Close();
                    vertex999LogWriter.Dispose();
                    Debug.Log($"Vertex 999 log file closed: {vertex999LogPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error closing vertex 999 log: {e.Message}");
                }
            }
        }

        internal override void SetOutputValues()
        { lock (visualizationValuesLock) U = U_Active.Clone(); }
		
        /// <summary>
        /// This function sets the target time step size, below is the formula for the conduction speed of the action potential (wave speed)
        ///
        /// \f[v = \frac{1}{C}\sqrt{\frac{d}{R_a R_{mem}}}]
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

        public static double SetTargetTimeStep(double cap, double maxDiameter, double minDiameter, double edgeLength, double totalConductance, double gl, double res, double cfl)
        {
            /// here we set the minimum time step size and maximum time step size
            /// the dtmin is based on prior numerical experiments that revealed that for each refinement level the 
            /// voltage profiles were visually accurate when compared to Yale Neuron for delta t at least 2 microseconds
            /// we want to avoid using dtmin; therefore I compute the upper bound (and lower bound for reference)
            // double dtmin = 2e-6;
            // double dtmax = 50e-7;
            double dtmax = 30e-6;
            // double dtmax = 50e-6;
            double dt;

            double gll = gl; double scf = 1E-6; // to convert to micrometer of edgelengths and radii don't forget this!!!!

            // what happens if the leak conductance is 0
            if (gll == 0.0) { gll = 1.0; }
            // double upper_bound = 100;
            double upper_bound = cap * edgeLength*scf * System.Math.Sqrt(res / (gll*minDiameter*scf));
            // double lower_bound = cap * edgeLength*scf * System.Math.Sqrt(res / totalConductance * maxDiameter*scf);
            // some cells may have an upper bound that is too large for the solver, so choose the smaller of the two dtmax or upper_bound
            dt = System.Math.Min(upper_bound,dtmax);
            //GameManager.instance.DebugLogSafe("lower_bound = " + lower_bound.ToString());
            // Debug.Log("dt = " + dt);
            return dt;       
        }

        public void InitializeIonChannel()
        {
            ionChannels = new List<IonChannel>();
            activeIonChannels = new List<IonChannel>();

            // The following lines are adding all channels from IonChannelModels.cs

            var channelSettings = new Dictionary<string,bool>()
            {
                { "Potassium Channel", true },
                { "Sodium Channel", true },  // true to activate chanenl in simulation
                { "Calcium Channel", false },  // false to deactive channel in simulation
                { "Leakage Channel", false },
                { "Low Threshold Calcium Channel", false },
                { "Slow Potassium Channel", false },
            };


            var channelMethods = typeof(IonChannelModels).GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in channelMethods)
            {
                if (method.ReturnType == typeof(IonChannel))
                {
                    // add all channels to the ion channel list
                    object channelObj = null;
                    var parms = method.GetParameters();
                    try
                    {
                        if (parms.Length == 1)
                        {
                            channelObj = method.Invoke(null, new object[] { Neuron.nodes.Count });
                        }
                        else if (parms.Length == 2)
                        {
                            channelObj = method.Invoke(null, new object[] { Neuron.nodes.Count, startingVoltage });
                        }
                        else
                        {
                            // unexpected signature; skip
                            continue;
                        }
                    }
                    catch (TargetParameterCountException)
                    {
                        // signature mismatch; skip
                        continue;
                    }
                    var channel = (IonChannel)channelObj;
                    ionChannels.Add(channel);
                    
                    // add active channels to the simulation
                    if (channelSettings.TryGetValue(channel.Name, out bool enabled) && enabled)
                    {
                        activeIonChannels.Add(channel);
                        // Update leakConductance for timestep
                        if (channel.Name.Contains("Leak")) leakConductance = channel.Conductance;
                    }

                }
            }
        }

        /// <summary>
        /// This function initializes the voltage vector <c>U</c> and the state vectors of gating variables
        /// The input <c>Neuron.vertCount</c> is the vertex count of the neuron geometry \n
        /// <c>U</c> is initialized to startingVoltage [V] for the entire cell \n
        /// Gating variables are initialized to their initial probabilities
        /// </summary>
        private void InitializeNeuronCell()
        {
            lock (visualizationValuesLock)
            {
                U = Vector.Build.Dense(Neuron.nodes.Count, 0.0); // Here is where initial voltage is set, i.e. -0.07 implies a start voltage of -70 mV for all vectors
                U_Active = U.Clone();
            }
            Upre = U_Active.Clone();

            Isyn = new List<Vector>();
            for (int i = 0; i < 2; i++)
            {
                // Create a dense vector for each node
                var synVector = Vector.Build.Dense(Neuron.nodes.Count, 0.0);
                Isyn.Add(synVector);
            }
            surfaceArea = Vector.Build.Dense(Neuron.nodes.Count, 0.0);

            // Initialize ion channels
            InitializeIonChannel();
            totalConductance = activeIonChannels.Sum(ch => ch.Conductance);

            currentStates = new Dictionary<string, Vector>();
            previousStates = new Dictionary<string, Vector>();

            foreach (var channel in activeIonChannels)
            {
                foreach (var gatingVariable in channel.GatingVariables)
                {
                    // Use the probability from the gating variable to initialize current and previous states
                    currentStates[gatingVariable.Name] = Vector.Build.Dense(Neuron.nodes.Count, gatingVariable.Probability);
                    previousStates[gatingVariable.Name] = currentStates[gatingVariable.Name].Clone();
                }
            }
        }
        // / <summary>
        // / This is for constructing the lhs and rhs of system matrix \n
        // / This will construct a HINES matrix (symmetric), it should be tridiagonal with some off
        // / diagonal entries corresponding to a branch location in the neuron graph \n
        // / The entries are defined by the following:
        // / \f[
        // / \left(-\sum_{k\in\mathcal{N}_j}\eta_kV_k^{n+1}\right)+\omega_jV_j^{n+1}=\left(\sum_{k\in\mathcal{N}_j}\eta_kV_k^{n}\right)+\bar{\omega}_jV_j^{n}
        // / \f]
        // / where
        // / \f[\eta_k = \frac{\gamma_{k, j}\Delta t}{ 2}\f]
        // / and
        // / \f[\omega_j = 1+\frac{\theta_j\Delta t}{ 2} = 1 +\frac{\Delta t\sum_{ p\in\mathcal{ N} _j}\gamma_{ p,j} }{ 2}\f]
        // / and \f$\gamma_{ k,j}\f$ is defined as
        // / \f[\gamma_{k, j}:=\frac{ 1}{ C_mR_a a_j\widetilde{\Delta x_j} }\cdot \frac{ 1}{\left(\frac{ 1} { a_{ k} ^2} +\frac{ 1} { a_j ^ 2}\right)\Delta x_{ { k},j} }\f]
        // / </summary>
        // / <param name="myCell"></param> this is the <c>Neuron</c> that contains all the information about the cell geometry
        // / <param name="res"></param> this is the axial resistance
        // / <param name="cap"></param> this is the membrane capacitance
        // / <param name="k"></param> this is the fixed time step size
        // / <returns>LHS,RHS</returns> the function returns the LHS, RHS stencil matrices for the diffusion solve in sparse format, it is compressed in the main solver routine.
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
        /// <param name="activeIonChannels"></param> this is the list of ion channels active in the simulation
        /// <param name="gatingStates"></param> this is the list of state vectors for all gating variables
        /// <param name="cap"></param> this is the capacitance
        /// <returns></returns>

        private static Vector reactF(List<IonChannel> activeIonChannels, Vector V, Dictionary<string, Vector> gatingStates, double cap)
        {
            Vector output = Vector.Build.Dense(V.Count, 0.0);
            
            foreach (var channel in activeIonChannels)
            {

                Vector prod = Vector.Build.Dense(V.Count, 1.0);
                // Only apply leak current directly
                if (channel.Name.Contains("Leak"))
                {
                    output.Add(V.Subtract(channel.ReversalPotential).Multiply(channel.Conductance), output);
                }
                else
                {
                    foreach (var gatingVariable in channel.GatingVariables)
                    {
                        Vector state = gatingStates[gatingVariable.Name];
                        // Adding a state to Gating Variables specifically for Low Threshold Calcium From Pospischil_Minimal_HH_2008 (s-instantaneous gating variable).
                        // A better solution may be available. For my current use case this is sufficient.
                        if (gatingVariable.IsInstant) {
                            state = gatingVariable.Alpha(V);
                        }
                        // Calculate contribution for this channel and gating variable
                        prod.SetSubVector(0, V.Count, state.PointwisePower(gatingVariable.Exponent).PointwiseMultiply(prod));
                    }
                    prod.SetSubVector(0, V.Count, V.Subtract(channel.ReversalPotential).PointwiseMultiply(prod));
                    output.Add(prod.Multiply(channel.Conductance), output);
                }

            }
            output.Multiply(-1.0 / cap, output);

            /*
            string path = Application.persistentDataPath + "/Log.txt";

            using (StreamWriter writer = new StreamWriter (path, true))
            {
                writer.WriteLine (output);
            }
            Debug.Log ("Append to: " + path);
            */


            return output;
        }

        private void stateexplicitSBDF2(Vector S, Vector Spre, Vector F, Vector Fpre, double dt)
        {
            S.Add(F.Multiply(dt), S); S.Multiply(4.0 / 3.0, S);
            S.Add(Spre.Multiply(-1.0 / 3.0), S); S.Add(Fpre.Multiply(-2.0 * dt / 3.0), S);
        }

        /// <summary>
        /// This is the function for the right hand side of the ODE on state S, which is given by:
        /// \f[\frac{dS}{dt}=\alpha_S(V)(1-S)-\beta_S(V)S\f]
        /// </summary>
        /// <param name="a"></param> this is the rate vector
        /// <param name="b"></param> this is the rate vector
        /// <param name="S"></param> this is the current vector of state S for the geometry
        /// <returns>f(V,N)</returns> the function returns the right hand side of the state N ODE.
        private static Vector fS(Vector S, Vector a, Vector b) { return a.PointwiseMultiply(1 - S) - b.PointwiseMultiply(S); }
       
        // used by save/load functions in Menu.cs

        // Returns a map from each gating-variable name to its current values [V.Count]
        // returns kvp (key-value pair)
        public Dictionary<string, double[]> getCurrentStates() { 
            return currentStates.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value.AsArray()
            );
        }

        // Returns a map from each gating-variable name to its current values [V.Count]
        // returns kvp (key-value pair)
        public Dictionary<string, double[]> getPreviousStates() { 
            return previousStates.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value.AsArray()
            );
        }

        public double[] getUpre() { return Upre.AsArray(); }
        public void BuildVectors(double[] u, double[] upre, 
        Dictionary<string, double[]> currStates, Dictionary<string, double[]> prevStates)
        {
            lock (visualizationValuesLock) U = Vector.Build.DenseOfArray(u);
            lock (visualizationValuesLock) U_Active = U.Clone();
            Upre = Vector.Build.DenseOfArray(upre);

            // convert every double[] -> Vector<double> 

            currentStates  = currStates.ToDictionary(
                         kvp => kvp.Key,
                         kvp => Vector.Build.DenseOfArray(kvp.Value));

            previousStates = prevStates.ToDictionary(
                         kvp => kvp.Key,
                         kvp => Vector.Build.DenseOfArray(kvp.Value));

            Isyn = new List<Vector>();
            for (int i = 0; i < 2; i++)
            {
                // Create a dense vector for each node
                var synVector = Vector.Build.Dense(Neuron.nodes.Count, 0.0);
                Isyn.Add(synVector);
            }
            surfaceArea = Vector.Build.Dense(Neuron.nodes.Count, 0.0);
        }
    }
}
