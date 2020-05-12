#region using
using System;
using UnityEngine;
#endregion

namespace C2M2
{
    namespace UGX
    {
        /// DiameterData
        /// <summary>
        /// Data for diameter attachment
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
        
        /// NormalData
        /// <summary>
        /// Data for normals of grid
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
        /// </summary>
        /// TODO: Add caching for synapse current data
        readonly struct SynapseData : IAttachmentData
        {
            public readonly ISynapse Synapse;

            /// <summary>
            /// Set the synapse
            /// </summary>
            /// <param name="synapse"></param>
            public SynapseData(in ISynapse synapse) => Synapse = synapse;
        }
    } // UGX
} // C2M2