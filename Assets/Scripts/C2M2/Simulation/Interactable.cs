using System;
using System.Threading;
using UnityEngine;

namespace C2M2.Simulation
{
    using Interaction;
    /// <summary>
    /// Manages interaction input to simulations
    /// </summary>
    /// <remarks>
    /// Interactable provides an interface so that interaction scripts can affect a Simulation without needing to know their type.
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