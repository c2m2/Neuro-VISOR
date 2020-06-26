using UnityEngine;
using System;

namespace C2M2.Interaction
{
    using VR;
    using Simulation;
    public abstract class RaycastHeater : MonoBehaviour
    {
        protected Interactable simulation;

        protected abstract void OnAwake();
        private void Awake()
        {
            OnAwake();
            // This script is useless if there isn't a simulation to effect
            simulation = GetComponent<Interactable>() ?? throw new SimulationNotFoundException();
        }
        // Return an array of 3D indices and new values to add to those indices
        protected abstract Tuple<int, double>[] HitMethod(RaycastHit hit);

        /// <summary> 
        /// Derived classes decide how to translate RaycastHit into index/newValue pairings 
        /// </summary>
        public void Hit(RaycastHit hit) => simulation.SetValues(hit);
    }
}
