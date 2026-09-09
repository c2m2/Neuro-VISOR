using System.Runtime.InteropServices;
using System.Text;

namespace C2M2.NeuronalDynamics.Generation
{
    /// <summary>
    /// Raw P/Invoke binding for neuronmesh_native.dll (Assets/Plugins/x86_64/) - a thin C API
    /// (see the NeuronMesh repo's include/neuronmesh/capi.h) wrapping
    /// NeuronGraph::write_vrn_levels, the exact same spline/mesh/junction-capping pipeline the
    /// `neuronmesh --vrn` CLI already uses to build a .vrn's contents. Prefer
    /// NeuronGeneratorService over calling this directly - it runs the call off the main thread
    /// and does the actual .vrn zip step afterward (the native side only writes the raw UGX/
    /// MetaInfo.json files into a folder - see capi.h's own doc comment for why).
    /// CharSet.Ansi matches how the native side actually reads these paths: NeuronGraph's file I/O
    /// (std::ifstream, tinyxml2's LoadFile/SaveFile) takes narrow (char*) paths, which Windows
    /// interprets via the current ANSI code page, not UTF-8 - the same interpretation
    /// CharSet.Ansi's marshaling uses.
    /// </summary>
    internal static class NeuronMeshNative
    {
        [DllImport("neuronmesh_native", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int nm_generate_vrn_files(
            string input_path,
            string output_dir,
            string cell_name,
            string method,
            int n_levels,
            double delta,
            int p,
            int sides,
            int repair,
            int bridge_samples,
            double bridge_inset,
            int sphere,
            int sphere_weld,
            int star_open,
            StringBuilder error_buf,
            int error_buf_len);

        // Generates a single 3D surface mesh (no 1D skeleton, no MetaInfo.json) at the given
        // delta/p/method/sides and writes it straight to output_path - used for the in-VR level
        // preview (NeuronGeneratorPanel's Preview button), which needs to regenerate cheaply as the
        // user flips between levels rather than paying for a full N-level .vrn build each time.
        [DllImport("neuronmesh_native", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int nm_generate_mesh_file(
            string input_path,
            string output_path,
            string method,
            double delta,
            int p,
            int sides,
            int repair,
            int bridge_samples,
            double bridge_inset,
            int sphere,
            int sphere_weld,
            int star_open,
            StringBuilder error_buf,
            int error_buf_len);
    }
}
