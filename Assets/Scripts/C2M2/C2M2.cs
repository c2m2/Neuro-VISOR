/// <summary>
/// Domain for all scripts produced in-house at the Center for Computational Mathematics and Modeling (C2M2) at Temple University
/// </summary>
/// <remarks>
/// This script provides Doxygen with namespace descriptions for documentation purposes
/// </remarks>
namespace C2M2
{
    /// <summary> Scripts powering Molecular Dynamics modeling </summary>
    namespace MolecularDynamics
    {
        /// <summary> Neuronal Dynamics simulation code </summary>
        namespace Simulation { }
        /// <summary> Neuronal Dynamics visualization </summary>
        namespace Visualization { }
    }

    /// <summary> Scripts powering Neuronal Dynamics modeling </summary>
    namespace NeuronalDynamics
    {
        /// <summary> Neuronal Dynamics simulation code </summary>
        namespace Simulation { }
        /// <summary> Neuronal Dynamics visualization </summary>
        namespace Visualization { }
        /// <summary> Utilities for processing .ugx files containing 1D, 3D, and mapping geometry info </summary>
        namespace UGX { }
    }

    /// <summary> Scripts that were used during an unsuccessful exploration into implementing order-independent transparency in VR. </summary>
    namespace OIT { }

    /// <summary> Utility scripts used in many other backend scripts </summary>
    namespace Utils
    {
        /// <summary> Utilities that apply to Unity Mesh objects </summary>
        namespace MeshUtils { }

        /// <summary> Utilities used in debugging </summary>
        namespace DebugUtils { }

        namespace Editor { }
    }
    
    /// <summary>
    /// All scripts powering general model simulation
    /// </summary>
    namespace Simulation { }

    /// <summary> All scripts powering general model visualization </summary>
    namespace Visualization
    {
        /// <summary> All scripts directly related to visualization in virtual reality </summary>
        namespace VR { }
    }

    /// <summary> All scripts powering general model interaction </summary>
    namespace Interaction
    {
        /// <summary> All scripts directly related to interaction in virtual reality </summary>
        namespace VR { }
    }
}
