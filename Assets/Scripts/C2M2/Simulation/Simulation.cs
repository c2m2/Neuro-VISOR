using UnityEngine;
using System.Threading;
using System;
using C2M2.Interaction;
using C2M2.Interaction.VR;

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
        public bool startOnAwake = true;

        /// <summary>
        /// Provide mutual exclusion to derived classes
        /// </summary>
        protected Mutex mutex = new Mutex();

        /// <summary>
        /// Thread that runs simulation code
        /// </summary>
        private Thread solveThread = null;

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
        protected abstract void Solve();

        /// <summary>
        /// Called on the main thread before the Solve thread is launched
        /// </summary>
        /// <remarks>
        /// This is useful if you need to initialize anything that makes use of Unity calls,
        /// which are not available to be called from secondary threads.
        /// </remarks>
        protected virtual void PreSolve() { }

        #region Unity Methods
        public void Awake()
        {
            if (!dryRun)
            {
                viz = BuildVisualization();
                BuildInteraction();
            }

            // Run child awake methods first
            OnAwake(viz);

            return;

            void BuildInteraction()
            {
                switch (interactionType)
                {
                    case (InteractionType.Discrete): Heater = gameObject.AddComponent<RaycastSimHeaterDiscrete>(); break;
                    case (InteractionType.Continuous): Heater = gameObject.AddComponent<RaycastSimHeaterContinuous>(); break;
                }

                /// Add event child object for interaction scripts to find
                GameObject child = new GameObject("HitInteractionEvent");
                child.transform.parent = transform;
                child.transform.position = Vector3.zero;
                child.transform.eulerAngles = Vector3.zero;

                // Attach hit events to an event manager
                RaycastEventManager eventManager = gameObject.AddComponent<RaycastEventManager>();
                // Create hit events
                RaycastPressEvents raycastEvents = child.AddComponent<RaycastPressEvents>();
                raycastEvents.OnHoldPress.AddListener((hit) => Heater.Hit(hit));
                eventManager.rightTrigger = raycastEvents;
                eventManager.leftTrigger = raycastEvents;

                // Some scripts change transform position for some reason, reset the position/rotation at the first frame
                gameObject.AddComponent<Utils.DebugUtils.Actions.TransformResetter>();
            }
        }

        public void Start()
        {
            OnStart();

            if (startOnAwake) StartSimulation();
        }
        public void Update()
        {
            OnUpdate();

            if (!dryRun)
            {
                ValueType simulationValues = GetValues();

                if (simulationValues != null) UpdateVisualization(simulationValues);
            }
        }

        // Allow derived classes to run code in Awake/Start/Update if they choose
        protected virtual void OnAwake(VizType viz) { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }

        // Don't allow threads to keep running when application pauses or quits
        private void OnApplicationPause(bool pause)
        {
            OnPause();
            if (pause && solveThread != null) solveThread.Abort();
        }
        private void OnApplicationQuit()
        {
            OnQuit();
            if (solveThread != null) solveThread.Abort();
        }
        // Use OnPause and OnQuit to wrap up I/O or other processes if the application pauses or quits during solve code.
        protected virtual void OnPause() { }
        protected virtual void OnQuit() { }
        #endregion

        /// <summary>
        /// Launch Solve thread
        /// </summary>
        public void StartSimulation()
        {
            StopSimulation();
            Debug.Log("Running PreSolve...");
            PreSolve();
            solveThread = new Thread(Solve);
            solveThread.Start();
            Debug.Log("Solve() launched on thread " + solveThread.ManagedThreadId);
        }

        /// <summary>
        /// Stop current Solve thread
        /// </summary>
        public void StopSimulation() { if (solveThread != null) solveThread.Abort(); }
    }
    public class SimulationNotFoundException : Exception
    {
        public SimulationNotFoundException() : base() { }
        public SimulationNotFoundException(string message) : base(message) { }
        public SimulationNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}