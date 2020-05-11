using System;
using System.Threading;
using UnityEngine;

namespace C2M2
{
    namespace Simulation
    { 
        using Interaction;
        /// <summary>
        /// Manages interaction input to simulations
        /// </summary>
        /// <remarks>
        /// InteractableSimulation provides a public link to SetValues() so that other scripts don't need to know the type parameter of Simulation.
        /// Only Simulation.cs should derive from this class. Custom simulation code should derive from Simulation.cs or one
        /// of its derived classes such as ScalarFieldSimulation, Neuron1DSimulation, etc
        /// </remarks>
        public abstract class Interactable : MonoBehaviour
        {
            protected RaycastSimHeater simHeater = null;
            public RaycastSimHeater SimHeater
            {
                get { return simHeater; }
                protected set
                {
                    if (simHeater != null) Destroy(simHeater);
                    simHeater = value;
                }
            }
            public enum InteractionType { Discrete, Continuous }
            public InteractionType interactionType = InteractionType.Discrete;

            /// <summary> Send an array of (index, newValue) pairings for hit points </summary>
            /// <remarks>
            /// In order to affect live simulations, this method must know how to add values between 0 and 1 to the current simulation values
            /// </remarks>
            public abstract void SetValues(RaycastHit hit);



        }
    }
}
