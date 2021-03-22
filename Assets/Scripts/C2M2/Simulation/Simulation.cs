using UnityEngine;
using System.Threading;
using System;
using C2M2.Interaction;
using C2M2.Visualization;
using UnityEngine.Profiling;

namespace C2M2.Simulation
{
    /// <summary>
    /// Provides an base interface for simulations using a general data type T
    /// </summary>
    /// <typeparam name="ValueType"> Type of simulation values </typeparam>
    public abstract class Simulation<ValueType, VizType, RaycastType, GrabType> : Interactable
    {
        [Tooltip("Run simulation code without visualization or interaction features")]
        /// <summary>
        /// Run solve code without visualization or interaction
        /// </summary>
        public bool dryRun = false;

        /// <summary>
        /// Should the simulation start itself in Awake?
        /// </summary>
        public bool startOnAwake = true; // TODO: Move away from using this

        public double raycastHitValue = 55;
        public Tuple<int, double>[] raycastHits = new Tuple<int, double>[0];

        /// <summary>
        /// Provide mutual exclusion to derived classes
        /// </summary>
        protected Mutex mutex = new Mutex();

        /// <summary>
        /// Thread that runs simulation code
        /// </summary>
        private Thread solveThread = null;
        protected CustomSampler solveStepSampler = null;
        public RaycastEventManager raycastEventManager = null;
        public RaycastPressEvents defaultRaycastEvent = null;

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

        public VizType viz { get; protected set; }

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

        #region Unity Methods
        public void Initialize()
        {
            // We should move away from using OnAwakePre, OnAwakePost
            OnAwakePre(); //this is a mess!! :(

            if (!dryRun)
            {
                viz = BuildVisualization();
                BuildInteraction();
            }

            // Run child awake methods first
            OnAwakePost(viz);

            return;

            void BuildInteraction()
            {
                switch (interactionType)
                {
                    case (InteractionType.Discrete):
                        Heater = gameObject.AddComponent<RaycastSimHeaterDiscrete>();
                        ((RaycastSimHeaterDiscrete)Heater).value = raycastHitValue;
                        break;
                    case (InteractionType.Continuous): Heater = gameObject.AddComponent<RaycastSimHeaterContinuous>(); break;
                }

                /// Add event child object for interaction scripts to find
                GameObject child = new GameObject("DirectRaycastInteractionEvent");
                child.transform.parent = transform;

                // Attach hit events to an event manager
                raycastEventManager = gameObject.AddComponent<RaycastEventManager>();
                // Create hit events
                defaultRaycastEvent = child.AddComponent<RaycastPressEvents>();
                // TODO: Get rid of RaycastSimHeater, just add Simulation.AddValue here
                defaultRaycastEvent.OnHoldPress.AddListener((hit) => SetValues(hit));
                defaultRaycastEvent.OnEndPress.AddListener((hit) => ResetRacyastHits(hit));

                raycastEventManager.LRTrigger = defaultRaycastEvent;

                OnStart();

                if (startOnAwake) StartSimulation();
            }
        }
        public void FixedUpdate()
        {
            OnUpdate();

            if (!dryRun)
            {
                ValueType simulationValues = GetValues();

                if (simulationValues != null) UpdateVisualization(simulationValues);
            }
        }

        protected virtual void OnAwakePre() { }
        // Allow derived classes to run code in Awake/Start/Update if they choose
        protected virtual void OnAwakePost(VizType viz) { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }

        // Don't allow threads to keep running when application pauses or quits
        private void OnApplicationPause(bool pause)
        {
            OnPause();
            if (pause) StopSimulation();
        }
        private void OnApplicationQuit()
        {
            OnQuit();
            StopSimulation();
        }

        private void OnDestroy()
        {
            OnDest();
            StopSimulation();
        }
        // Use OnPause and OnQuit to wrap up I/O or other processes if the application pauses or quits during solve code.
        protected virtual void OnPause() { }
        protected virtual void OnQuit() { }
        protected virtual void OnDest() { }
        #endregion

        public int time = -1;
        public double k = 0.008 * 1e-3;
        public double endTime = 1.0;
        public int nT { get; private set; } = -1;
        /// <summary>
        /// Launch Solve thread
        /// </summary>
        public void StartSimulation()
        {
            // Stop previous simulation, if any
            StopSimulation();

            solveStepSampler = CustomSampler.Create("SolveStep");
            
            solveThread = new Thread(Solve) { IsBackground = true };
            solveThread.Start();
            Debug.Log("Solve() launched on thread " + solveThread.ManagedThreadId);
        }

        private void Solve()
        {
            Profiler.BeginThreadProfiling("Solve Threads", "Solve Thread");

            PreSolve();

            nT = (int)(endTime / k);
            
            for (time = 0; time < nT; time++)
            {
                // mutex guarantees mutual exclusion over simulation values
                mutex.WaitOne();

                PreSolveStep();

                solveStepSampler.Begin();
                SolveStep(time);
                solveStepSampler.End();

                PostSolveStep();

                mutex.ReleaseMutex();
            }

            PostSolve();

            Profiler.EndThreadProfiling();
            GameManager.instance.DebugLogSafe("Simulation Over.");
        }

        /// <summary>
        /// PreSolveStep is called once per simulation frame, before SolveStep() 
        /// </summary>
        protected virtual void PreSolveStep() { }

        /// <summary>
        /// PostSolveStep is called once per simulation frame, after SolveStep() 
        /// </summary>
        protected virtual void PostSolveStep() { }
        /// <summary>
        /// Called on the main thread before the Solve thread is launched
        /// </summary>
        /// <remarks>
        /// This is useful if you need to initialize anything that makes use of Unity calls,
        /// which are not available to be called from secondary threads.
        /// </remarks>
        protected virtual void PreSolve() { Debug.Log("PreSolve"); }
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
                mutex.WaitOne();
                time = nT;           
                mutex.ReleaseMutex();
                solveThread = null;             
            }
        }

        public void ResetRacyastHits(RaycastHit hit)
        {
            raycastHits = new Tuple<int, double>[0];
        }
    }
    public class SimulationNotFoundException : Exception
    {
        public SimulationNotFoundException() : base() { }
        public SimulationNotFoundException(string message) : base(message) { }
        public SimulationNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}