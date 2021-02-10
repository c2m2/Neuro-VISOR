using UnityEngine;

namespace C2M2.Simulation
{
    using Interaction;
    /// <summary>
    /// Interface for interaction scripts
    /// </summary>
    /// <remarks>
    /// Interactable provides an interface so that interaction scripts can affect a Simulation without needing to know its type.
    /// </remarks>
    public abstract class Interactable : MonoBehaviour
    {
        private RaycastHeater heater = null;
        /// <summary>
        /// Interaction script that resolves player interaction and passes new values back here
        /// </summary>
        public RaycastHeater Heater
        {
            get { return heater; }
            protected set
            {
                if (heater != null) Destroy(heater);
                heater = value;
            }
        }
        public enum InteractionType { Discrete, Continuous }
        /// <summary>
        /// Continuous interaction can interact with mesh surface regions using an adjacency list and Dijkstra search.
        /// </summary>
        protected InteractionType interactionType = InteractionType.Discrete;

        /// <summary> Require derived classes to know how to receive an interaction event </summary>
        /// <remarks>
        /// In order to affect live simulations, this method must know how to add values between 0 and 1 to the current simulation values
        /// </remarks>
        public abstract void SetValues(RaycastHit hit);

        /// <summary>
        /// Return the current timestep for the simulation
        /// </summary>
        /// <returns></returns>
        public abstract float GetSimulationTime();

    }
}