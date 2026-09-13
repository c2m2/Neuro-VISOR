using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Generation
{
    /// <summary>
    /// Orchestrates the NeuronGenerator pipeline: a .swc file plus the same options
    /// `neuronmesh --vrn` takes on the command line, in -> a .vrn written straight into
    /// StreamingAssets/NeuronalDynamics/Geometries (the exact folder CellPreviewer.cs already
    /// scans), out. The native call (NeuronMeshNative.nm_generate_vrn_files) runs on a background
    /// thread via Task.Run - real neurons at a fine delta can take real time, and this must never
    /// block Unity's main/VR thread - then this class does the actual .vrn zip step itself via
    /// .NET's own System.IO.Compression.ZipFile, matching exactly the format VrnReader.cs already
    /// expects (a zip containing MetaInfo.json + the per-level UGX pairs). See capi.h's own doc
    /// comment for why the native side stops short of zipping itself.
    /// </summary>
    public static class NeuronGeneratorService
    {
        public struct Options
        {
            public string method;      // "PG"/"CP"/"HC"/"PC"/"BS"/"SG"/"CC" (case-insensitive)
            public int n;               // refinement level count
            public double delta;        // starting point spacing (halved per level)
            public int p;                // polynomial degree / iteration count (meaning depends on method)
            public int sides;            // tube cross-section vertex count
            public bool repair;          // preprocess()/soma repair before generating
            public int bridgeSamples;    // Y-junction/star-junction curved bridge resolution
            public double bridgeInset;   // how far that bridge sags toward the junction center (0-1)
            public bool sphere;          // force a plain unwelded soma sphere regardless of incidence
            public bool sphereWeld;      // force a soma sphere with real per-branch holes welded in
            public bool starOpen;        // leave the default 3+-incident soma patch's 2 poles open

            public static Options Default()
            {
                return new Options
                {
                    method = "PG",
                    n = 3,
                    delta = 1.0,
                    p = 3,
                    sides = 12,
                    repair = false,
                    bridgeSamples = 12,
                    bridgeInset = 0.5,
                    sphere = false,
                    sphereWeld = false,
                    starOpen = false,
                };
            }
        }

        public class Result
        {
            public bool success;
            public string vrnPath;
            public string errorMessage;
        }

        public class PreviewResult
        {
            public bool success;
            public string ugxPath;
            public string errorMessage;
        }

        /// <summary>
        /// Generates a .vrn from swcPath. Safe to call from the main/UI thread - the native call and
        /// zip step both happen on a background thread; the returned Task's continuation still lands
        /// on a thread-pool thread, so a caller that needs to touch Unity APIs afterward (updating a
        /// status label, calling CellPreviewer.Refresh(), ...) must marshal that back to the main
        /// thread itself (e.g. via a MonoBehaviour's Update() polling the Task, or a
        /// SynchronizationContext capture) - see NeuronGeneratorPanel.cs for how the UI does this.
        /// </summary>
        public static Task<Result> GenerateAsync(string swcPath, Options options)
        {
            string cellName = Path.GetFileNameWithoutExtension(swcPath);
            string geometriesDir = Path.Combine(Application.streamingAssetsPath, "NeuronalDynamics", "Geometries");
            string vrnPath = Path.Combine(geometriesDir, cellName + ".vrn");
            string scratchDir = Path.Combine(Application.temporaryCachePath,
                "NeuronGenerator_" + Guid.NewGuid().ToString("N"));

            if (options.sphere && options.sphereWeld)
            {
                var badOptionsResult = new Result
                {
                    success = false,
                    vrnPath = vrnPath,
                    errorMessage = "sphere and sphereWeld are mutually exclusive",
                };
                return Task.FromResult(badOptionsResult);
            }

            return Task.Run(() =>
                GenerateBlocking(swcPath, cellName, geometriesDir, vrnPath, scratchDir, options, options.n, options.delta));
        }

        /// <summary>
        /// Generates a throwaway single-level .vrn (written under Application.temporaryCachePath, not
        /// StreamingAssets/Geometries) at the given preview `level`'s delta, purely so the panel's 1D
        /// view button can get at real 1D skeleton geometry - unlike GeneratePreviewMeshAsync's
        /// nm_generate_mesh_file, which explicitly produces surface-only output with no 1D skeleton at
        /// all (see NeuronMeshNative's own doc comment), nm_generate_vrn_files is the only native entry
        /// point that writes one. Costs roughly what a real single-level Generate does - unavoidable
        /// without a native-side "1D only" export - so this is only worth calling from the 1D button,
        /// not on every level/parameter tweak the way the cheap surface preview is. The caller reads
        /// the 1D mesh back out via VrnReader (same as NeuronCellPreview.cs already does for real
        /// saved cells) and is responsible for deleting the returned vrnPath afterward.
        /// </summary>
        public static Task<Result> GeneratePreview1DVrnAsync(string swcPath, Options options, int level)
        {
            double levelDelta = options.delta / Math.Pow(2.0, level);
            string cellName = Path.GetFileNameWithoutExtension(swcPath);
            string geometriesDir = Application.temporaryCachePath;
            string vrnPath = Path.Combine(geometriesDir, "NeuronPreview1D_" + Guid.NewGuid().ToString("N") + ".vrn");
            string scratchDir = Path.Combine(Application.temporaryCachePath,
                "NeuronGeneratorPreview1D_" + Guid.NewGuid().ToString("N"));

            if (options.sphere && options.sphereWeld)
            {
                var badOptionsResult = new Result
                {
                    success = false,
                    vrnPath = vrnPath,
                    errorMessage = "sphere and sphereWeld are mutually exclusive",
                };
                return Task.FromResult(badOptionsResult);
            }

            return Task.Run(() =>
                GenerateBlocking(swcPath, cellName, geometriesDir, vrnPath, scratchDir, options, nLevels: 1, delta: levelDelta));
        }

        private static Result GenerateBlocking(string swcPath, string cellName, string geometriesDir,
            string vrnPath, string scratchDir, Options options, int nLevels, double delta)
        {
            var result = new Result { vrnPath = vrnPath };
            try
            {
                Directory.CreateDirectory(scratchDir);

                var errorBuf = new StringBuilder(2048);
                int rc = NeuronMeshNative.nm_generate_vrn_files(
                    swcPath,
                    scratchDir,
                    cellName,
                    options.method,
                    nLevels,
                    delta,
                    options.p,
                    options.sides,
                    options.repair ? 1 : 0,
                    options.bridgeSamples,
                    options.bridgeInset,
                    options.sphere ? 1 : 0,
                    options.sphereWeld ? 1 : 0,
                    options.starOpen ? 1 : 0,
                    errorBuf,
                    errorBuf.Capacity);

                if (rc != 0)
                {
                    result.success = false;
                    result.errorMessage = errorBuf.Length > 0
                        ? errorBuf.ToString()
                        : $"nm_generate_vrn_files failed (code {rc})";
                    return result;
                }

                Directory.CreateDirectory(geometriesDir);
                if (File.Exists(vrnPath)) { File.Delete(vrnPath); }
                ZipFile.CreateFromDirectory(scratchDir, vrnPath, System.IO.Compression.CompressionLevel.Optimal,
                    includeBaseDirectory: false);

                result.success = true;
                return result;
            }
            catch (Exception e)
            {
                result.success = false;
                result.errorMessage = e.Message;
                return result;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(scratchDir)) { Directory.Delete(scratchDir, recursive: true); }
                }
                catch
                {
                    // Best-effort cleanup only - a leftover scratch dir under
                    // Application.temporaryCachePath isn't user-visible and doesn't affect the
                    // Geometries folder the app actually scans.
                }
            }
        }

        /// <summary>
        /// Generates a single 3D surface mesh at the given refinement `level` (0-based; delta for
        /// level k is options.delta / 2^k, the same per-level halving write_vrn_levels/run_vrn_mode
        /// use) and writes it to a scratch .ugx file under Application.temporaryCachePath - never
        /// touches StreamingAssets/Geometries, since this is a throwaway preview, not the final
        /// output. The caller is responsible for deleting the returned ugxPath once done with it
        /// (e.g. after parsing it into a Grid/Mesh on the main thread - see NeuronGeneratorPanel's
        /// Preview button) and for reading the file only via a plain path, since UGXReader.ReadUGX
        /// and Unity's Mesh/GameObject APIs are main-thread-only and must not be touched from here.
        /// </summary>
        public static Task<PreviewResult> GeneratePreviewMeshAsync(string swcPath, Options options, int level)
        {
            double levelDelta = options.delta / Math.Pow(2.0, level);
            string ugxPath = Path.Combine(Application.temporaryCachePath,
                "NeuronPreview_" + Guid.NewGuid().ToString("N") + ".ugx");

            return Task.Run(() => GeneratePreviewBlocking(swcPath, ugxPath, options, levelDelta));
        }

        private static PreviewResult GeneratePreviewBlocking(string swcPath, string ugxPath, Options options,
            double levelDelta)
        {
            var result = new PreviewResult { ugxPath = ugxPath };
            try
            {
                var errorBuf = new StringBuilder(2048);
                int rc = NeuronMeshNative.nm_generate_mesh_file(
                    swcPath,
                    ugxPath,
                    options.method,
                    levelDelta,
                    options.p,
                    options.sides,
                    options.repair ? 1 : 0,
                    options.bridgeSamples,
                    options.bridgeInset,
                    options.sphere ? 1 : 0,
                    options.sphereWeld ? 1 : 0,
                    options.starOpen ? 1 : 0,
                    errorBuf,
                    errorBuf.Capacity);

                if (rc != 0)
                {
                    result.success = false;
                    result.errorMessage = errorBuf.Length > 0
                        ? errorBuf.ToString()
                        : $"nm_generate_mesh_file failed (code {rc})";
                    return result;
                }

                result.success = true;
                return result;
            }
            catch (Exception e)
            {
                result.success = false;
                result.errorMessage = e.Message;
                return result;
            }
        }
    }
}
