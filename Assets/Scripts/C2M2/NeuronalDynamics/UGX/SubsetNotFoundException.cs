using System;
using System.Runtime.Serialization;

namespace C2M2.NeuronalDynamics.UGX {
    /// <summary>
    /// Custom RTE for situations when subset is not found in geometry
    /// </summary>
    [Serializable]
    public class SubsetNotFoundException : Exception {
        public SubsetNotFoundException (string message) : base (message) { }

        protected SubsetNotFoundException (SerializationInfo info, StreamingContext ctxt) : base (info, ctxt) { }
    }
}