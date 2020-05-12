/// <summary>
/// Domain for all scripts produced in-house at the Center for Computational Mathematics and Modeling (C2M2) at Temple University
/// </summary>
namespace C2M2
{
    /// <summary>
    /// Utility scripts used in many other backend scripts
    /// </summary>
    namespace Utils
    {
        /// <summary>
        /// Utilities that apply to Unity Mesh objects
        /// </summary>
        namespace MeshUtils { }

        /// <summary>
        /// Utilities used in debugging
        /// </summary>
        namespace DebugUtils { }
    }
    
    /// <summary>
    /// All scripts powering the model simulation
    /// </summary>
    namespace Simulation { }

    /// <summary>
    /// All scripts powering model visualization
    /// </summary>
    namespace Visualization
    {
        /// <summary>
        /// All scripts directly related to visualization in virtual reality
        /// </summary>
        namespace VR { }
    }

    /// <summary>
    /// All scripts powering model interaction
    /// </summary>
    namespace Interaction
    {
        /// <summary>
        /// All scripts directly related to interaction in virtual reality
        /// </summary>
        namespace VR { }
    }
}
