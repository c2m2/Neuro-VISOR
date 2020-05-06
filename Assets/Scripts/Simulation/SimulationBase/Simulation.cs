using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
namespace C2M2
{
    namespace SimulationScripts
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
            protected sealed override void AwakeC() { AwakeD(); }
            protected sealed override void StartC() { StartD(); }
            protected sealed override void UpdateC()
            {
                UpdateD();

                ValueType simulationValues = GetValues();
                if(simulationValues != null)
                {
                    // Use simulation values to update visuals
                    UpdateVisualization(simulationValues);
                }
            }
            // Allow derived classes to run code in Awake/Start/Update if they choose
            protected virtual void AwakeD() { }
            protected virtual void StartD() { }
            protected virtual void UpdateD() { }
            #endregion
        }
    }
}