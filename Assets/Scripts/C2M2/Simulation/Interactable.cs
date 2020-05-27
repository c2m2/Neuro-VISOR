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
        private RaycastSimHeater simHeater = null;
        /// <summary>
        /// Interaction script that resolves player interaction and passes new values back here
        /// </summary>
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

        /// <summary> Require derived classes to know how to receive an interaction event </summary>
        /// <remarks>
        /// In order to affect live simulations, this method must know how to add values between 0 and 1 to the current simulation values
        /// </remarks>
        public abstract void SetValues(RaycastHit hit);
    }
}