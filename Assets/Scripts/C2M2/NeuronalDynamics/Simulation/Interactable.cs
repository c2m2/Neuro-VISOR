using UnityEngine;

namespace C2M2.Simulation
{
    /// <summary>
    /// Interface for interaction scripts
    /// </summary>
    /// <remarks>
    /// Interactable provides an interface so that interaction scripts can affect a Simulation without needing to know its type.
    /// </remarks>
    public abstract class Interactable : MonoBehaviour
    {

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