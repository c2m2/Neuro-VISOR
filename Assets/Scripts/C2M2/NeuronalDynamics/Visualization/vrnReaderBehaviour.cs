#region includes
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using C2M2.NeuronalDynamics.UGX;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
#endregion

namespace C2M2.NeuronalDynamics.Visualization
{
    using DiameterAttachment = IAttachment<DiameterData>;
    namespace vrn
    {
        sealed class vrnReader
        {
            /// GEOM1D                                                                                                                                                                                                                                                            
            /// <summary>                                                                                                                                                                                                                                                         
            /// Stores the 1D geometries by name and refinement                                                                                                                                                                                                                   
            /// </summary>                                                                                                                                                                                                                                                        
            [Serializable]                                                                                                                                                                                                                                                        
            private struct Geom1d                                                                                                                                                                                                                                                 
            {                                                                                                                                                                                                                                                                     
                public string name;                                                                                                                                                                                                                                               
                public string refinement;                                                                                                                                                                                                                                         
            }                                                                                                                                                                                                                                                                     
                                                                                                                                                                                                                                                                                  
            /// GEOM2D                                                                                                                                                                                                                                                            
            /// <summary>                                                                                                                                                                                                                                                         
            /// Stores the 2d geometries by name and inflation                                                                                                                                                                                                                    
            /// </summary>                                                                                                                                                                                                                                                        
            [Serializable]                                                                                                                                                                                                                                                        
            private struct Geom2d                                                                                                                                                                                                                                                 
            {                                                                                                                                                                                                                                                                     
                public string inflation;                                                                                                                                                                                                                                          
                public string name;    
                public string description;                                                                                                                                                                                                                                           
            }                                                                                                                                                                                                                                                                     
                                                                                                                                                                                                                                                                                  
            /// GEOMETRY                                                                                                                                                                                                                                                          
            /// <summary>                                                                                                                                                                                                                                                         
            /// Stores 1D and 2D geometries                                                                                                                                                                                                                                       
            /// </summary>                                                                                                                                                                                                                                                        
            [Serializable]                                                                                                                                                                                                                                                        
            private class Geometry                                                                                                                                                                                                                                                
            {                                                                                                                                                                                                                                                                     
                /// All 1D geometries                                                                                                                                                                                                                                             
                public Geom1d[] geom1d;                                                                                                                                                                                                                                           
                /// All 2D geometries                                                                                                                                                                                                                                             
                public Geom2d[] geom2d;                                                                                                                                                                                                                                           
            }                                                                                                                                                                                                                                                                     
                                                                                                                                                                                                                                                                                  
            private string fileName;                                                                                                                                                                                                                                              
            private Geometry geometry;                                                                                                                                                                                                                                            
            private Boolean loaded = false;                                                                                                                                                                                                                                       
                                                                                                                                                                                                                                                                                  
            /// READ                                                                                                                                                                                                                                                              
            /// <summary>
            /// Loads the meta data stored in the JSON
            /// </summary>
            private void load()
            {
                using (ZipArchive archive = ZipFile.OpenRead(this.fileName))
                {
                    var file = archive.GetEntry("MetaInfo.json");
                    _ = file ?? throw new ArgumentNullException(nameof(file));
                    geometry = JsonUtility.FromJson<Geometry>(new StreamReader(file.Open()).ReadToEnd().ToString());
                    UnityEngine.Debug.Log("Geometry: " + geometry.geom1d.Length);
                    loaded = true;
                }
            }
            
            
            /// READ_UGX
            /// <summary>
            /// Read a file from the .vrn archive and creates a UGX grid
            /// </summary>
            public void read_ugx (in string meshName, ref Grid grid) {
                using (ZipArchive archive = ZipFile.Open (this.fileName, ZipArchiveMode.Read)) {
                    var file = archive.GetEntry (meshName);
                    _ = file ?? throw new ArgumentNullException (nameof (file));
                    using (var stream = file.Open()) {
                        UGXReader.ReadUGX(stream, ref grid);
                        UnityEngine.Debug.Log("Read in grid from archive {meshName}:");
                        UnityEngine.Debug.Log(grid);
                    }
                }
            }

            /// RETRIEVE_1D_MESH
            /// <summary>
            /// Retrieve the 1d mesh name corresponding to the refinement number from the archive
            /// </summary>
            /// <param name="refinement"> Refinement number (Default: 0)</param>
            /// <returns> Filename of refined 1D mesh in archive </returns>
            public string retrieve_1d_mesh(int refinement = 0)
            {
                if (!loaded) load();
                UnityEngine.Debug.Log("Geometry: " + geometry.geom1d);
                int index = geometry.geom1d.ToList().FindIndex
                            (geom => Int16.Parse(geom.refinement) == refinement);
                return geometry.geom1d[index != -1 ? index : 0].name;
            }

            /// RETRIEVE_2D_MESH
            /// <summary>
            /// Retrieve the 2D mesh name corresponding to the inflation factor from the archive
            /// </summary>
            /// <param name="inflation"> Inflation factor </param>
            /// <returns> Filename of inflated 2D mesh in archive </returns>
            public string retrieve_2d_mesh(double inflation = 1.0)
            {
                if (!loaded) load();
                int index = geometry.geom2d.ToList().FindIndex
                            (geom => Double.Parse(geom.inflation) == inflation);
                return geometry.geom2d[index != -1 ? index : 0].name;
            }

            /// VRNREADER
            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="fileName"> Archive's file name (.vrn file) </param>
            public vrnReader(in string fileName) => this.fileName = fileName;

            /// EXAMPLE
            /// <summary>
            /// Example demonstrating vrnReader usage from command line (Not Unity)
            /// </summary>
            /// On start a test file (test.vrn) is read from the assets folder
            /// The coarse 1d mesh is retrieved as well as the by a factor of 
            /// 2.5 inflated 2d mesh is retrieved from the archive test.vrn
            class Example
            {
                static void Main(string[] args)
                {
                    string fileName = "test.vrn";
                    try
                    {
                        /// Instantiate the VRN reader with the desired file name
                        vrnReader reader = new vrnReader(fileName);
                        /// Get the name of the 1d mesh (0-th refinement aka coarse grid)
                        Console.WriteLine(reader.retrieve_1d_mesh(0));
                        /// Get the name of the inflated 2d mesh by a factor of 2.5
                        Console.WriteLine(reader.retrieve_2d_mesh(2.5));
                    }
                    catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.ArgumentNullException)
                    {
                        Console.Error.WriteLine($"Archive or mesh file not found. Archive: {fileName}.");
                        Console.Error.WriteLine(ex);
                    }
                }
            }
        }

        /// VRNREADERBEHAVIOUR
        /// <summary>
        /// Example behaviour to demonstrate vrnReader usage
        /// </summary>
        public class vrnReaderBehaviour : MonoBehaviour
        {
            /// the test archive file test.vrn
            public string fileName = "test.vrn";

            /// <summary>
            /// On start a test file (test.vrn) is read from the assets folder
            /// </summary>
            /// The coarse 1d mesh is retrieved as well as the by a factor of 
            /// 2.5 inflated 2d mesh is retrieved from the archive test.vrn
            public void Start()
            {
                string fullFileName = Application.dataPath + Path.DirectorySeparatorChar + fileName;
                /// Instantiate the VRN reader with the desired file name
                vrnReader reader = new vrnReader(fullFileName);
                /// Get the name of the 1d mesh (0-th refinement aka coarse grid)
                UnityEngine.Debug.Log(reader.retrieve_1d_mesh(0));
                /// Get the name of the inflated 2d mesh by a factor of 2.5
                UnityEngine.Debug.Log(reader.retrieve_2d_mesh(2.5));
                
                /// 1. Find a refinement in the .vrn archive (0 = 0th refinement = coarse grid, 1 = 1st refinement, 2 = 2nd refinement, ...)
                /// or: Find a inflated mesh in the .vrn archive (1 = Inflated by factor 1, 2.5 = inflated by a factor 2.5, ...)
                /// 2. Create empty Grid grid to store the mesh
                /// 3. Read the file from the archive (.ugx filetype) into the Grid grid
                /// NOTE: If the required refinement or inflation is not stored in the archive an error is thrown
                /// Name of mesh in archive (Here: Name of the 0-th refinement aka coarse grid in the archive)
                string meshName = reader.retrieve_1d_mesh(0);
                /// Create empty grid with name of grid in archive
                Grid grid = new Grid(new Mesh(), meshName);
                grid.Attach(new DiameterAttachment());
                /// Read in the .ugx file into the grid (read_ugx uses UGXReader internally)
                UnityEngine.Debug.Log("Reading now mesh: " + meshName);
                reader.read_ugx(meshName, ref grid);
            }
        }
    }
}
