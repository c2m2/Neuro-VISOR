using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
namespace C2M2
{
    using Utilities;
    using Interaction;
    using Interaction.VR;
    namespace Simulation
    {
        /// <summary>
        /// Provides an interface for simulations using a general data type T
        /// </summary>
        /// <typeparam name="T"> Type of simulation values </typeparam>
        public abstract class Simulation<ValueType> : Interactable
        {
            #region Abstract Methods
            /// <summary>
            /// Retrieve simulation values of type T
            /// </summary>
            /// <remarks> 
            /// The simulation must make an array of scalars available in order for visaulization to work.
            /// The array should contain one scalar value for every point that needs to be visualized
            /// </remarks>
            public abstract ValueType GetValues();

            // Require derived simulation types to figure out the visualizations on their own depending on their value type
            protected abstract void UpdateVisualization(in ValueType newValues);
            #endregion

            #region Unity Methods
            // TODO: Only allow T to take certain values
 
            // Allow derived classes to run code in Awake/Start/Update if they choose
            protected virtual void OnAwake() { }
            protected virtual void OnStart() { }
            protected virtual void OnUpdate() { }
            #endregion

            // Don't allow derived classes to override this.Awake/Start/Update
            public void Awake()
            {
                OnAwake();

                #region Interaction
                switch (interactionType)
                {
                    case (InteractionType.Discrete): simHeater = gameObject.AddComponent<RaycastSimHeaterDiscrete>(); break;
                    case (InteractionType.Continuous): simHeater = gameObject.AddComponent<RaycastSimHeaterContinuous>(); break;
                }

                gameObject.AddComponent<TransformResetter>();

                RaycastEventManager eventManager = gameObject.AddComponent<RaycastEventManager>();

                GameObject child = new GameObject("HitInteractionEvent");
                child.transform.parent = transform;
                child.transform.position = Vector3.zero;
                child.transform.eulerAngles = Vector3.zero;

                RaycastPressEvents raycastEvents = child.AddComponent<RaycastPressEvents>();
                raycastEvents.OnHoldPress.AddListener((hit) => simHeater.Hit(hit));

                eventManager.rightTrigger = raycastEvents;
                eventManager.leftTrigger = raycastEvents;
                #endregion

                if (startOnAwake) StartSimulation();
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

            #region Threading
            public bool startOnAwake = true;
            private Thread solveThread = null;
            protected Mutex mutex = new Mutex();

            // Computation code should be contained within Solve(). Simulation.cs will launch Solve() on its own thread.
            protected abstract void Solve();

            #region Simulation Thread Controls
            // Stop any current simulation thread and launch a new one
            public void StartSimulation()
            {
                StopSimulation();
                solveThread = new Thread(Solve);
                solveThread.Start();
                Debug.Log("Solve() launched on thread " + solveThread.ManagedThreadId);
            }
            // Stop current simulation thread
            public void StopSimulation() { if (solveThread != null) solveThread.Abort(); }

            #endregion

            #region Unity Methods
            // Don't allow threads to keep running on pause, quit
            private void OnApplicationPause(bool pause)
            {
                if (pause && solveThread != null) solveThread.Abort();
            }
            private void OnApplicationQuit()
            {
                if (solveThread != null) solveThread.Abort();
            }
            #endregion
            #endregion
        }
    }
}