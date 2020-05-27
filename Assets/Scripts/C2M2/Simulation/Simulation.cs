using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using C2M2.Utils;
using C2M2.Interaction;
using C2M2.Interaction.VR;

namespace C2M2.Simulation
{
    /// <summary>
    /// Provides an base interface for simulations using a general data type T
    /// </summary>
    /// <typeparam name="ValueType"> Type of simulation values </typeparam>
    public abstract class Simulation<ValueType> : Interactable
    {
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
        /// Require derived classes to know how to translate simulation values onto their simulation
        /// </summary>
        /// <param name="newValues"></param>
        protected abstract void UpdateVisualization(in ValueType newValues);

        /// <summary>
        /// Launch Solve thread
        /// </summary>
        public void StartSimulation()
        {
            StopSimulation();
            solveThread = new Thread(Solve);
            solveThread.Start();
            Debug.Log("Solve() launched on thread " + solveThread.ManagedThreadId);
        }
        /// <summary>
        /// Stop current Solve thread
        /// </summary>
        public void StopSimulation() { if (solveThread != null) solveThread.Abort(); }
        /// <summary>
        /// Method containing simulation code
        /// </summary>
        /// <remarks>
        /// Launches in its own thread
        /// </remarks>
        protected abstract void Solve();

        #region Unity Methods
        public void Awake()
        {
            // Run child awake methods first
            OnAwake();

            InitInteraction();

            if (startOnAwake) StartSimulation();

            return;

            void InitInteraction(){
                switch (interactionType)
                {
                    case (InteractionType.Discrete): SimHeater = gameObject.AddComponent<RaycastSimHeaterDiscrete>(); break;
                    case (InteractionType.Continuous): SimHeater = gameObject.AddComponent<RaycastSimHeaterContinuous>(); break;
                }

                /// Add event child object for interaction scripts to find
                GameObject child = new GameObject("HitInteractionEvent");
                child.transform.parent = transform;
                child.transform.position = Vector3.zero;
                child.transform.eulerAngles = Vector3.zero;
                // Create hit event
                RaycastPressEvents raycastEvents = child.AddComponent<RaycastPressEvents>();
                raycastEvents.OnHoldPress.AddListener((hit) => SimHeater.Hit(hit));
                // Attach event to an event manager
                RaycastEventManager eventManager = gameObject.AddComponent<RaycastEventManager>();
                eventManager.rightTrigger = raycastEvents;
                eventManager.leftTrigger = raycastEvents;
                // Some scripts change transform position for some reason, reset the position/rotation at the first frame
                gameObject.AddComponent<Utils.DebugUtils.Actions.TransformResetter>();
            }
        }
        public void Start()
        {
            OnStart();
            gameObject.AddComponent<VRGrabbable>();
        }
        public void Update()
        {
            OnUpdate();

            ValueType simulationValues = GetValues();
            if (simulationValues != null)
            {
                // Use simulation values to update visuals
                UpdateVisualization(simulationValues);
            }
        }
        // Allow derived classes to run code in Awake/Start/Update if they choose
        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        // Don't allow threads to keep running when application pauses or quits
        private void OnApplicationPause(bool pause)
        {
            if (pause && solveThread != null) solveThread.Abort();
        }
        private void OnApplicationQuit()
        {
            if (solveThread != null) solveThread.Abort();
        }
        #endregion
    }
    public class SimulationNotFoundException : Exception
    {
        public SimulationNotFoundException() : base() { }
        public SimulationNotFoundException(string message) : base(message) { }
        public SimulationNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}