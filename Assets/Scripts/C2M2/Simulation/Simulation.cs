using UnityEngine;
using System.Threading;
using System;
using C2M2.Interaction;
using UnityEngine.Profiling;
using System.Collections;
using System.Threading.Tasks;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.Simulation
{
    /// <summary>
    /// Tracks the initialization phase of a Simulation to enforce correct ordering
    /// </summary>
    public enum InitState
    {
        Uninitialized,
        PreInitializing,
        BuildingVisualization,
        BuildingInteraction,
        PostInitializing,
        Initialized
    }

    /// <summary>
    /// Provides an base interface for simulations using a general data type T
    /// </summary>
    /// <typeparam name="ValueType">Type of simulation values</typeparam>
    /// <typeparam name="VizType"></typeparam>
    /// <typeparam name="RaycastType"></typeparam>
    /// <typeparam name="GrabType"></typeparam>
    public abstract class Simulation<ValueType, VizType, RaycastType, GrabType> : Interactable
    {
        /// <summary>
        /// Current initialization state of this simulation
        /// </summary>
        public InitState CurrentInitState { get; private set; } = InitState.Uninitialized;
        [Tooltip("Run simulation code without visualization or interaction features")]
        /// <summary>
        /// Run solve code without visualization or interaction
        /// </summary>
        public bool dryRun = false;

        /// <summary>
        /// Cancellation token for thread
        /// </summary>
        private CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// Percentage of minTime taken to run a time step
        /// </summary>
        public float resourceUsage = 0;

        /// <summary>
        /// Provides mutual exclusion to derived classes for whatever values are being used for visualization
        /// </summary>
        private protected readonly object visualizationValuesLock = new object();

        /// <summary>
        /// Thread that runs simulation code
        /// </summary>
        private Thread solveThread = null;
        protected CustomSampler solveStepSampler = null;
        public RaycastEventManager raycastEventManager = null;
        public RaycastPressEvents defaultRaycastEvent = null;

        /// <summary>
        /// Minimum time for each time step to run in seconds
        /// </summary>
        readonly float minTimeStep = .01f;

        /// <summary>
        /// How often the visualization should be updated in seconds. Should never be less than minTime
        /// </summary>
        readonly float visualizationTimeStep = .02f;

        /// <summary>
        /// Require derived classes to make simulation values available
        /// </summary>
        public abstract ValueType GetValues();


        /// <summary>
        /// Simulations must know how to build their visualization and what type the visualization is
        /// </summary>
        /// <remarks>
        /// See SurfaceSimulation & NeuronSimulation1D or PositionFieldSimulation for examples.
        /// </remarks>
        protected abstract VizType BuildVisualization();

        public VizType Viz { get; protected set; }

        /// <summary>
        /// Update the visualization. This will be called once per Update() call
        /// </summary>
        /// <remarks>
        /// See SurfaceSimulation & NeuronSimulation1D or PositionFieldSimulation for examples.
        /// </remarks>
        protected abstract void UpdateVisualization(in ValueType newValues);

        /// <summary>
        /// Method containing simulation code
        /// </summary>
        /// <remarks>
        /// Launches in its own thread
        /// </remarks>
        protected abstract void SolveStep(int t);

        /// <summary>
        /// PreSolveStep is called once per simulation frame, before SolveStep() 
        /// </summary>
        protected virtual void PreSolveStep(int t) { }

        /// <summary>
        /// PostSolveStep is called once per simulation frame, after SolveStep() 
        /// </summary>
        protected virtual void PostSolveStep(int t) { }

        #region Unity Methods
        public void Initialize()
        {
            CurrentInitState = InitState.PreInitializing;
            OnAwakePre();

            if (!dryRun)
            {
                CurrentInitState = InitState.BuildingVisualization;
                Viz = BuildVisualization();

                CurrentInitState = InitState.BuildingInteraction;
                BuildInteraction();
            }

            CurrentInitState = InitState.PostInitializing;
            OnAwakePost(Viz);

            CurrentInitState = InitState.Initialized;
            StartCoroutine("UpdateVisualizationStep");
            return;

            void BuildInteraction()
            {
                /// Add event child object for interaction scripts to find
                GameObject child = new GameObject("DirectRaycastInteractionEvent");
                child.transform.parent = transform;

                // Attach hit events to an event manager
                raycastEventManager = gameObject.AddComponent<RaycastEventManager>();
                // Create hit events
                defaultRaycastEvent = child.AddComponent<RaycastPressEvents>();

                defaultRaycastEvent.OnHoldPress.AddListener((hit) => SetValues(hit));

                raycastEventManager.LRTrigger = defaultRaycastEvent;

                OnStart();

                StartSimulation();
            }
        }

        IEnumerator UpdateVisualizationStep()
        {
            while (!dryRun)
            {
                if (!GameManager.instance.simulationManager.Paused)
                {
                    ValueType simulationValues = GetValues();
                    if (simulationValues != null) UpdateVisualization(simulationValues);
                }
                yield return new WaitForSeconds(visualizationTimeStep);

            }

        }

        // Allow derived classes to run code in Awake/Start if they choose
        protected virtual void OnAwakePre() { }
        protected virtual void OnAwakePost(VizType viz) { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        protected virtual async void WriteCSV()
        {   
        }

        protected virtual void StopCSV()
        {
        }

        protected void OnDestroy()
        {
            if (GameManager.isQuitting) return;

            StopCoroutine("UpdateVisualizationStep");
            StopSimulation();

            // Remove this simulation from the active simulations list
            if (GameManager.instance != null)
            {
                lock (GameManager.instance.activeSimsLock)
                {
                    GameManager.instance.activeSims.Remove(this);
                }
            }
        }
        #endregion

        public int currentTimeStep = -1;
        public double timeStep = 0.008 * 1e-3;
        public double endTime = 1.0;
        public int nT => (int)(endTime / timeStep);

        /// <summary>
        /// Launch Solve thread
        /// </summary>
        public void StartSimulation()
        {
            if (CurrentInitState != InitState.Initialized && CurrentInitState != InitState.BuildingInteraction)
            {
                Debug.LogError("StartSimulation called during invalid init state: " + CurrentInitState);
                return;
            }

            solveStepSampler = CustomSampler.Create("SolveStep");

            solveThread = new Thread(Solve) { IsBackground = true };
            solveThread.Start();
            Debug.Log("Solve() launched on thread " + solveThread.ManagedThreadId);
        }

        private async void Solve()
        {
            Profiler.BeginThreadProfiling("Solve Threads", "Solve Thread");

            PreSolve();

            GameManager.instance.solveBarrier.AddParticipant();
            try
            {
                DateTime startStepTime = DateTime.Now;
                currentTimeStep = 0;
                while (currentTimeStep < nT)
                {
                    if (!GameManager.instance.simulationManager.Paused && !GameManager.instance.Loading)
                    {
                        PreSolveStep(currentTimeStep);

                        solveStepSampler.Begin();
                        SolveStep(currentTimeStep);
                        solveStepSampler.End();

                        PostSolveStep(currentTimeStep);
                        WriteCSV();
                        StopCSV();

                        currentTimeStep++;
                    }

                    GameManager.instance.solveBarrier.SignalAndWait();
                    float timeChange = (float)(DateTime.Now - startStepTime).TotalSeconds;
                    resourceUsage = timeChange / minTimeStep;
                    if (resourceUsage < 1)
                    {
                        int millisecondsToWait = (int)(1000 * (minTimeStep-timeChange));
                        await Task.Delay(millisecondsToWait);
                    }
                    if (cts.Token.IsCancellationRequested) break;
                    startStepTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                GameManager.instance.DebugLogErrorSafe("Solver thread crashed: " + ex.ToString());
            }
            finally
            {
                GameManager.instance.solveBarrier.RemoveParticipant();
                cts.Dispose();
            }

            PostSolve();

            Profiler.EndThreadProfiling();

            solveThread = null;
        }

        public sealed override float GetSimulationTime() => currentTimeStep * (float)timeStep;

        /// <summary>
        /// Called on the solve thread before the simulation for loop is launched
        /// </summary>
        protected abstract void PreSolve();
        /// <summary>
        /// Called on the solve thread after the simulation for loop is completed
        /// </summary>
        protected virtual void PostSolve() { Debug.Log("PostSolve"); }
        /// <summary>
        /// Stop current Solve thread
        /// </summary>
        public void StopSimulation()
        {
            if (solveThread != null) cts.Cancel();
        }

        /// <summary>
        /// Given a raycast hit, find the hit 3D vertices
        /// </summary>
        public int[] HitToVertices(RaycastHit hit)
        {
            // We will have 3 new index/value pairings
            Tuple<int, double>[] newValues = new Tuple<int, double>[3];

            // Translate hit triangle index so we can index into triangles array
            int triInd = hit.triangleIndex * 3;
            MeshFilter mf = hit.transform.GetComponentInParent<MeshFilter>();
            // Get mesh vertices from hit triangle
            int v1 = mf.mesh.triangles[triInd];
            int v2 = mf.mesh.triangles[triInd + 1];
            int v3 = mf.mesh.triangles[triInd + 2];

            return new int[] { v1, v2, v3 };
        }
    }

    public class SimulationNotFoundException : Exception
    {
        public SimulationNotFoundException() : base() { }
        public SimulationNotFoundException(string message) : base(message) { }
        public SimulationNotFoundException(string message, Exception inner) : base(message, inner) { }
    }

}