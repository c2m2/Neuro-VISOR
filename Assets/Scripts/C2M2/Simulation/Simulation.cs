using UnityEngine;
using System.Threading;
using System;
using C2M2.Interaction;
using UnityEngine.Profiling;

namespace C2M2.Simulation
{

    /// <summary>
    /// Provides an base interface for simulations using a general data type T
    /// </summary>
    /// <typeparam name="ValueType">Type of simulation values</typeparam>
    /// <typeparam name="VizType"></typeparam>
    /// <typeparam name="RaycastType"></typeparam>
    /// <typeparam name="GrabType"></typeparam>
    public abstract class Simulation<ValueType, VizType, RaycastType, GrabType> : Interactable
    {
        [Tooltip("Run simulation code without visualization or interaction features")]
        /// <summary>
        /// Run solve code without visualization or interaction
        /// </summary>
        public bool dryRun = false;

        public bool paused = false;

        /// <summary>
        /// Cancellation token for thread
        /// </summary>
        private CancellationTokenSource cts = new CancellationTokenSource();

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
        /// Minimum time for each time step to run in milliseconds
        /// </summary>
        public int minTime = 1;

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
            // We should move away from using OnAwakePre, OnAwakePost
            OnAwakePre(); //this is a mess!! :(

            if (!dryRun)
            {
                Viz = BuildVisualization();
                BuildInteraction();
            }

            // Run child awake methods first
            OnAwakePost(Viz);

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

        public void Update()
        {
            if (!paused && !dryRun)
            {
                ValueType simulationValues = GetValues();

                if (simulationValues != null) UpdateVisualization(simulationValues);
            }
        }
        
        // Allow derived classes to run code in Awake/Start if they choose
        protected virtual void OnAwakePre() { }
        protected virtual void OnAwakePost(VizType viz) { }
        protected virtual void OnStart() { }

        // Don't allow threads to keep running when application pauses or quits
        private void OnApplicationPause(bool pause)
        {
            OnPause(pause);
            if (pause) paused = true;
        }
        private void OnApplicationQuit()
        {
            OnQuit();
            StopSimulation();
        }

        protected void OnDestroy()
        {
            OnDest();
            StopSimulation();
        }
        // Use OnPause and OnQuit to wrap up I/O or other processes if the application pauses or quits during solve code.
        protected virtual void OnPause(bool pause) { }
        protected virtual void OnQuit() { }
        protected virtual void OnDest() { }
        #endregion

        public int time = -1;
        public double timeStep = 0.008 * 1e-3;
        public double endTime = 1.0;
        public int nT => (int)(endTime / timeStep);

        /// <summary>
        /// Launch Solve thread
        /// </summary>
        public void StartSimulation()
        {
            solveStepSampler = CustomSampler.Create("SolveStep");
            
            solveThread = new Thread(Solve) { IsBackground = true };
            solveThread.Start();
            Debug.Log("Solve() launched on thread " + solveThread.ManagedThreadId);
        }

        private void Solve()
        {
            Profiler.BeginThreadProfiling("Solve Threads", "Solve Thread");

            PreSolve();

            GameManager.instance.solveBarrier.AddParticipant();
            var watch = new System.Diagnostics.Stopwatch();
            for (time = 0; time < nT; time++)
            {
                watch.Start();
                PreSolveStep(time);

                solveStepSampler.Begin();
                SolveStep(time);
                solveStepSampler.End();

                PostSolveStep(time);
                watch.Stop();
                if (watch.ElapsedMilliseconds < minTime)
                {
                    Thread.Sleep(minTime-(int)watch.ElapsedMilliseconds);
                }
                watch.Reset();
                if (cts.Token.IsCancellationRequested) break;
                GameManager.instance.solveBarrier.SignalAndWait();
            }
            GameManager.instance.solveBarrier.RemoveParticipant();
            cts.Dispose();

            PostSolve();

            Profiler.EndThreadProfiling();
        }

        public sealed override float GetSimulationTime() => time * (float)timeStep;

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
            if (solveThread != null)
            {
                cts.Cancel();
                solveThread = null;             
            }
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