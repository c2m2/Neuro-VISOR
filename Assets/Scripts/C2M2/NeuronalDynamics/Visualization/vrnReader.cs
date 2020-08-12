#region includes
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
#endregion

namespace C2M2.NeuronalDynamics.Visualization
{
    namespace vrn
    {
        public sealed class vrnReader : MonoBehaviour
        {
        
                 /// GEOM1D
                /// <summary>
                /// Stores the 1D geometries by name and refinement 
                /// </summary>
                            [Serializable]
                public struct Geom1d
                {
                    public string name;
                    public string refinement;
                }

                /// GEOM2D
                /// <summary>
                /// Stores the 2d geometries by name and inflation
                /// </summary>
                            [Serializable]
                public struct Geom2d
                {
		  public string inflation;
                   public string name;
                 }
                 
            /// GEOMETRY
            /// <summary>
            /// Stores 1D and 2D geometries
            /// </summary>
            [Serializable]
            public class Geometry
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
                    loaded = true;
              
                }
            }

            /// READ
            /// <summary>
            /// Read a file from the .vrn archive
            /// </summary>
            private Stream read(in string meshName)
            {
                string name = Path.GetTempPath() + Path.GetRandomFileName();
                using (ZipArchive archive = ZipFile.Open(this.fileName, ZipArchiveMode.Read))
                {
                    var file = archive.GetEntry(meshName);
                    _ = file ?? throw new ArgumentNullException(nameof(file));
                    return file.Open();
                }
            }

            /// RETRIEVE_1D_MESH
            /// <summary>
            /// Retrieve the 1d mesh corresponding to the refinement number from the archive
            /// </summary>
            /// <param name="refinement"> Refinement number (Default: 0)</param>
            /// <returns> Filename of refined 1D mesh in archive </returns>
            public string retrieve_1d_mesh(int refinement = 0)
            {
                if (!loaded) load();
                int index = geometry.geom1d.ToList().FindIndex
                (geom => Int16.Parse(geom.refinement) == refinement);
                return geometry.geom1d[index != -1 ? index : 0].name;
            
            }

            /// RETRIEVE_2D_MESH
            /// <summary>
            /// Retrieve the 2D mesh corresponding to the inflation factor from the archive
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
        
        
        public void Start() {
	  
	  UnityEngine.Debug.Log(Application.dataPath);
	             string fileName = Application.dataPath + "/test.vrn";
                try
                {
                    vrnReader reader = new vrnReader(fileName);
                    /// Get 1d mesh (0-th refinement aka coarse grid)
                    Console.WriteLine(reader.retrieve_1d_mesh(0));
                    UnityEngine.Debug.Log(reader.retrieve_1d_mesh(0));
                         
                    /// Get inflated 2d mesh by a factor of 2.5
                    ///Console.WriteLine(reader.retrieve_2d_mesh(2.5));
                }
                catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.ArgumentNullException)
                {
		    UnityEngine.Debug.Log("file not found!");
		    UnityEngine.Debug.Log(ex);
                    Console.WriteLine($"File not found: {fileName}!");
                    Console.WriteLine(ex);
                }
	    }
        }

        /// EXAMPLE
        /// <summary>  
        /// Example demonstrating vrnReader usage
        /// </summary>
        class Example
        {
            static void Main(string[] args)
            {
                string fileName = "test.vrn";
                try
                {
                    vrnReader reader = new vrnReader(fileName);
                    /// Get 1d mesh (0-th refinement aka coarse grid)
                    Console.WriteLine(reader.retrieve_1d_mesh(0));
                         
                    /// Get inflated 2d mesh by a factor of 2.5
                    Console.WriteLine(reader.retrieve_2d_mesh(2.5));
                }
                catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.ArgumentNullException)
                {
                    Console.WriteLine($"File not found: {fileName}!");
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
