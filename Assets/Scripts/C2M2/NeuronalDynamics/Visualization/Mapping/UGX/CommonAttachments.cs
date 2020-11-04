#region using
using System;
using UnityEngine;
#endregion

namespace C2M2.NeuronalDynamics.UGX
{
    /// DiameterData
    /// <summary>
    /// Data for diameter attachment
    /// Used to store diameters during reading in a grid
    /// </summary>
    readonly struct DiameterData : IAttachmentData
    {
        public Double Diameter { get; }
        /// <summary>
        /// DiameterData
        /// </summary>
        /// <param name="diameter">Diameter</param>
        public DiameterData(in Double diameter) => Diameter = diameter;
    }

    /// <summary>
    /// IndexData
    /// </summary>
    /// <param name="parentIndex"> ParentIndex </param>
    readonly struct IndexData : IAttachmentData { 
        public int ParentIndex { get; }
        public IndexData(in int parentIndex) => ParentIndex = parentIndex;
    }

    /// NormalData
    /// <summary>
    /// Data for normals of grid
    /// Used to store normals during reading in a grid
    /// </summary>
    readonly struct NormalData : IAttachmentData
    {
        public Vector3 Normal { get; }
        /// <summary>
        /// NormalData
        /// </summary>
        /// <param name="normal">Normal</param>
        public NormalData(in Vector3 normal) => Normal = normal;
    }

    /// MappingData
    /// <summary>
    /// Data for mapping attachment
    /// Used to read in mapping data which maps between 1d and 2d vertices
    /// </summary>
    readonly struct MappingData : IAttachmentData
    {
        public Vector3 Start { get; }
        public Vector3 End { get; }
        public Double Lambda { get; }
        /// <summary>
        /// MappingData 
        /// </summary>
        /// <param name="start">From vertex</param>
        /// <param name="end">To vertex</param>
        /// <param name="lambda">Interpolation parameter</param>
        public MappingData(in Vector3 start, in Vector3 end, in Double lambda) => (Start, End, Lambda) = (start, end, lambda);
    }
    /// SynapseData
    /// <summary>
    /// Data for synapse attachment
    /// Used to store synapse data during reading in a grid 
    /// </summary>
    readonly struct SynapseData : IAttachmentData
    {
        public readonly ISynapse Synapse;

        /// <summary>
        /// Set the synapse
        /// </summary>
        /// <param name="synapse"> Synapse data </param>
        public SynapseData(in ISynapse synapse) => Synapse = synapse;
    }
}
