#region includes
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using C2M2.NeuronalDynamics.UGX;
using UnityEditor;
using UnityEngine;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
#endregion

namespace C2M2.NeuronalDynamics.Visualization {
    using DiameterAttachment = IAttachment<DiameterData>;
    namespace vrn {
        sealed class vrnReader {
            /// GEOM1D                                                                                                                                                                                                                                                            
            /// <summary>                                                                                                                                                                                                                                                         
            /// Stores the 1D geometries by name and refinement                                                                                                                                                                                                                   
            /// </summary>                                                                                                                                                                                                                                                        
            [Serializable]
            private struct Geom1d {
                public string name;
                public string refinement;
                public string description;
            }

            /// GEOM2D                                                                                                                                                                                                                                                            
            /// <summary>                                                                                                                                                                                                                                                         
            /// Stores the 2d geometries by name and inflation                                                                                                                                                                                                                    
            /// </summary>                                                                                                                                                                                                                                                        
            [Serializable]
            private struct Geom2d {
                public string inflation;
                public string name;
                public string description;
            }

            /// GEOMETRY                                                                                                                                                                                                                                                          
            /// <summary>                                                                                                                                                                                                                                                         
            /// Stores 1D and 2D geometries                                                                                                                                                                                                                                       
            /// </summary>                                                                                                                                                                                                                                                        
            [Serializable]
            private class Geometry {
                /// All 1D geometries                                                                                                                                                                                                                                             
                public Geom1d[] geom1d;
                /// All 2D geometries                                                                                                                                                                                                                                             
                public Geom2d[] geom2d;
            }

            private readonly string fileName;
            private Geometry geometry;
            private Boolean loaded;

            /// LOAD
            /// <summary>
            /// Try to load the .vrn archive and deserialize JSON into appropriate data structures
            /// </summary>
            /// Note the load is only done if the corresponding archive has not yet been loaded
            /// <see cref="Geom1d"> 1D Geometries </see>
            /// <see cref="Geom2d"> 2D Geoemtries </see>
            private void Load () {
                // Load if not already loaded
                if (!loaded) { DoLoad (); }

                // Helper function to do the actual loading
                void DoLoad () {
                    using (ZipArchive archive = ZipFile.OpenRead (this.fileName)) {
                        var file = archive.GetEntry ("MetaInfo.json");
                        _ = file ??
                            throw new ArgumentNullException (nameof (file));
                        geometry = JsonUtility.FromJson<Geometry> (new StreamReader (file.Open ()).ReadToEnd ());
                        loaded = true;
                    }
                }
            }

            /// LIST
            /// <summary>
            /// Print out a list of 1D and 2D geometries contained in .vrn archive
            /// </summary>
            public string List () {
                Load ();
                string s = $"Geometries contained in supplied .vrn archive ({this.fileName}): ";
                /// 1D geometries
                foreach (var geom in geometry.geom1d.Select ((x, i) => new { Value = x, Index = i })) {
                    s += $"{Environment.NewLine}#{geom.Index+1} 1D geometry: {geom.Value.name} with refinement {geom.Value.refinement} ({geom.Value.description}).";
                }
                /// 2D geometries
                foreach (var geom in geometry.geom2d.Select ((x, i) => new { Value = x, Index = i })) {
                    s += $"{Environment.NewLine}#{geom.Index+1} 2D geometry: {geom.Value.name} with inflation {geom.Value.inflation} ({geom.Value.description}).";
                }
                return s;
            }

            /// READ_UGX
            /// <summary>
            /// Read a file (UGX mesh) from the .vrn archive and creates a Grid instance from it
            /// <param name="meshName"> Name of mesh in archive </param>
            /// <param name="grid"> Grid in which we store the UGX mesh from the file </param>
            /// </summary>
            public void ReadUGX (in string meshName, ref Grid grid) {
                Load ();
                using (ZipArchive archive = ZipFile.Open (this.fileName, ZipArchiveMode.Read)) {
                    var file = archive.GetEntry (meshName);
                    _ = file ??
                        throw new ArgumentNullException (nameof (file));
                    using (var stream = file.Open ()) {
                        UGXReader.ReadUGX (stream, ref grid);
                    }
                }
            }

            /// RETRIEVE_1D_MESH
            /// <summary>
            /// Retrieve the name of the 1d mesh corresponding to the refinement number from the .vrn archive
            /// </summary>
            /// <param name="refinement"> Refinement number (Default: 0 (coarse grid))</param>
            /// <returns> Filename of refined or unrefined 1D mesh in archive </returns>
            public string Retrieve1DMeshName (int refinement = 0) {
                Load ();
                int index = geometry.geom1d.ToList ().FindIndex (geom => Int16.Parse (geom.refinement) == refinement);
                return geometry.geom1d[index != -1 ? index : 0].name;
            }

            /// RETRIEVE_2D_MESH
            /// <summary>
            /// Retrieve the name of the 2D mesh corresponding to the inflation factor from the .vrn archive
            /// </summary>
            /// <param name="inflation"> Inflation factor (default: 1.0 (no inflation)) </param>
            /// <returns> Filename of inflated 2D mesh in archive </returns>
            public string Retrieve2DMeshName (double inflation = 1.0) {
                Load ();
                int index = geometry.geom2d.ToList ().FindIndex (geom => Double.Parse (geom.inflation) == inflation);
                return geometry.geom2d[index != -1 ? index : 0].name;
            }

            /// VRNREADER
            /// <summary>
            /// Supplies the reader with an .vrn archive to load and manipulate
            /// </summary>
            /// <param name="fileName"> Archive's file name (.vrn file) </param>
            public vrnReader (in string fileName) => this.fileName = fileName;

            /// EXAMPLE
            /// <summary>
            /// Example demonstrating vrnReader usage from command line (Not Unity)
            /// </summary>
            /// On start a test file (test.vrn) is read from the assets folder
            /// The coarse 1d mesh is retrieved as well as the by a factor of 
            /// 2.5 inflated 2d mesh is retrieved from the archive test.vrn
            static class Example {
                static void Main (string[] args) {
                    string fileName = "test.vrn";
                    try {
                        /// Instantiate the VRN reader with the desired file name
                        vrnReader reader = new vrnReader (fileName);
                        /// Get the name of the 1d mesh (0-th refinement aka coarse grid)
                        Console.WriteLine (reader.Retrieve1DMeshName ());
                        /// Get the name of the inflated 2d mesh by a factor of 2.5
                        Console.WriteLine (reader.Retrieve2DMeshName (2.5));
                    } catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.ArgumentNullException) {
                        Console.Error.WriteLine ($"Archive or mesh file not found. Archive: {fileName}.");
                        Console.Error.WriteLine (ex);
                    }
                }
            }
        }

        /// VRNREADERBEHAVIOUR
        /// <summary>
        /// Example behaviour to demonstrate vrnReader usage
        /// </summary>
        public class vrnReaderBehaviour : MonoBehaviour {
            /// the test archive file test.vrn
            public string fileName = "test.vrn";

            /// <summary>
            /// On start a test file (test.vrn) is read from the assets folder
            /// </summary>
            /// The coarse 1d mesh is retrieved as well as the by a factor of 
            /// 2.5 inflated 2d mesh is retrieved from the archive test.vrn
            public void Start () {
                try {
                    ////////////////////////////////////////////////////////////////
                    /// Example 1: List grids and/or retrieve mesh file name from 
                    ///            archive based on refinement or inflation
                    ////////////////////////////////////////////////////////////////
                    string fullFileName = Application.dataPath + Path.DirectorySeparatorChar + fileName;
                    /// Instantiate the VRN reader with the desired file name (.vrn archive) to load from Assets
                    vrnReader reader = new vrnReader (fullFileName);
                    /// List all 1D and 2D geometries contained in given .vrn archive
                    Debug.Log (reader.List ());
                    /// Get the name of the 1d mesh (0-th refinement aka coarse grid) in archive
                    UnityEngine.Debug.Log (reader.Retrieve1DMeshName ());
                    /// Get the name of the inflated 2d mesh by a factor of 2.5 in archive
                    UnityEngine.Debug.Log (reader.Retrieve2DMeshName (2.5));
                    ////////////////////////////////////////////////////////////////

                    string meshName1D = reader.Retrieve1DMeshName();
                    /// Create empty grid with name of grid in archive
                    Grid grid1D = new Grid(new Mesh(), meshName1D);
                    grid1D.Attach(new DiameterAttachment());
                    /// Read in the .ugx file into the grid (read_ugx uses UGXReader internally)
                    UnityEngine.Debug.Log("Reading now mesh: " + meshName1D);
                    reader.ReadUGX(meshName1D, ref grid1D);
                    ////////////////////////////////////////////////////////////////
                    /// Example 2: Load a UGX file (mesh) from the .vrn archive and 
                    /// store it in a Grid object: Here the 1D coarse grid is loaded
                    ////////////////////////////////////////////////////////////////
                    /// NOTE: If the required refinement or inflation is not stored in the archive the first available 1D or 2D mesh is loaded
                    /// 1. Find a refinement in the .vrn archive (0 = 0th refinement = coarse grid, 1 = 1st refinement, 2 = 2nd refinement, ...)
                    /// or: Find a inflated mesh in the .vrn archive (1 = Inflated by factor 1, 2.5 = inflated by a factor 2.5, ...)
                    /// 2. Create empty Grid grid to store the mesh
                    /// 3. Read the file from the archive (.ugx filetype) into the Grid grid
                    string meshName2D = reader.Retrieve2DMeshName ();
                    /// Create empty grid with name of grid in archive
                    Grid grid2D = new Grid (new Mesh (), meshName2D);
                    grid2D.Attach (new DiameterAttachment ());
                    /// Read in the .ugx file into the grid (read_ugx uses UGXReader internally)
                    UnityEngine.Debug.Log ("Reading now mesh: " + meshName2D);
                    reader.ReadUGX (meshName2D, ref grid2D);

                    GetComponent<MeshFilter>().sharedMesh = grid2D.Mesh;
                    ////////////////////////////////////////////////////////////////
                } catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.ArgumentNullException) {
                    UnityEngine.Debug.LogError ($"Archive or mesh file not found. Archive: {fileName}.");
                    UnityEngine.Debug.LogError (ex);
                }
            }
        }
    }
}