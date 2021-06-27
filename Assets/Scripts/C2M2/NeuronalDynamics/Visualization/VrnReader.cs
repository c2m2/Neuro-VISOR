using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using C2M2.NeuronalDynamics.UGX;
using UnityEngine;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;

namespace C2M2.NeuronalDynamics.Visualization.VRN
{
    public class VrnReader
    {

        /// <summary>
        /// Stores the 1D geometries by name, refinement and a description
        /// Inflations are attached to the 1D geometries as a list of Geom2ds
        /// </summary>
        [Serializable]
        private struct Geom1d
        {
            public string name;
            public string refinement;
            public string description;
            public Geom2d[] inflations;

            public Geom1d(string name, string refinement, string description, Geom2d[] inflations)
            {
                this.name = name;
                this.refinement = refinement;
                this.description = description;
                this.inflations = inflations;
            }

        } 

        /// <summary>
        /// Stores the 2d geometries by name, inflation (factor), and description
        /// </summary>
        [Serializable]
        private struct Geom2d
        {
            public string inflation;
            public string name;
            public string description;


            public Geom2d(string inflation, string name, string description)
            {
                this.inflation = inflation;
                this.name = name;
                this.description = description;
            }
        }

        public struct MetaInfo {
            public string ARCHIVE;
            public string SPECIES;
            public string STRAIN;

            public MetaInfo(string archive, string species, string strain)
            {
                this.ARCHIVE = archive;
                this.SPECIES = species;
                this.STRAIN = strain;
            }
        }

        /// <summary>
        /// Stores 1D and associated 2D geometries in the member geom1d
        /// Additional metadata of the cell is stored in MetaInfo
        /// </summary>
        [Serializable]
        private class Geometry
        {
            public Geom1d[] geom1d;
            public string ARCHIVE;
            public string STRAIN;
            public string SPECIES;

            public Geometry(Geom1d[] geom1d, string ARCHIVE, string SPECIES, string STRAIN)
            {
                this.geom1d = geom1d;
                this.ARCHIVE = ARCHIVE;
                this.SPECIES = SPECIES;
                this.STRAIN = STRAIN;
            }
        }

        private readonly string fileName;
        private Geometry geometry;
        private MetaInfo metaInfo;
        private Boolean loaded;

        /// LOAD
        /// <summary>
        /// Try to load the .vrn archive and deserialize JSON into appropriate data structures
        /// </summary>
        /// Note the load is only done if the corresponding archive has not yet been loaded
        /// <see cref="Geom1d"> 1D Geometries </see>
        /// <see cref="Geom2d"> 2D Geoemtries </see>
        private void Load()
        {
            // Load if not already loaded
            if (!loaded) { DoLoad(); }

            // Helper function to do the actual loading
            void DoLoad()
            {
                using (ZipArchive archive = ZipFile.OpenRead($"{this.fileName}"))
                {
                    var file = archive.GetEntry("MetaInfo.json");
                    _ = file ??
                        throw new CouldNotReadMeshFromVRNArchive(nameof(file));
                    geometry = JsonUtility.FromJson<Geometry>(new StreamReader(file.Open()).ReadToEnd());
                    loaded = true;
                    metaInfo = new MetaInfo(geometry.ARCHIVE, geometry.SPECIES, geometry.STRAIN);
                }
            }
        }

        ///<summary>
        /// Returns the metainfo for the loaded cell geometry
        /// </summary>
        public MetaInfo? GetMetaInfo()
        {
            return metaInfo;
        }

        /// <summary>
        /// Print out a list of 1D and 2D geometries contained in .vrn archive
        /// </summary>
        public string List()
        {
            Load();
            string s = $"Geometries contained in supplied .vrn archive ({this.fileName}): ";
            foreach (var geom in geometry.geom1d.Select((x, i) => new { Value = x, Index = i }))
            {
                s += $"{Environment.NewLine}#{geom.Index + 1} 1D geometry: {geom.Value.name} with refinement {geom.Value.refinement} ({geom.Value.description}).";
            }
            return s;
        }

        public int[] ListRefinements()
        {
            Load();
            var geoms = geometry.geom1d.Select((x, i) => new { Value = x, Index = i });
            int[] options = new int[geoms.Count()];

            int j = 0;
            foreach (var geom in geoms)
            {
                bool isParsable = Int32.TryParse(geom.Value.refinement, out options[j]);
                j++;
            }

            return options;
        }

        /// READ_UGX
        /// <summary>
        /// Read a file (UGX mesh) from the .vrn archive and creates a Grid instance from it
        /// <param name="meshName"> Name of mesh in archive </param>
        /// <param name="grid"> Grid in which we store the UGX mesh from the file </param>
        /// </summary>
        public void ReadUGX(in string meshName, ref Grid grid)
        {
            Load();
            using (ZipArchive archive = ZipFile.Open(this.fileName, ZipArchiveMode.Read))
            {
                var file = archive.GetEntry(meshName);
                _ = file ??
                    throw new CouldNotReadMeshFromVRNArchive(this.fileName);
                using (var stream = file.Open())
                {
                    UGXReader.ReadUGX(stream, ref grid);
                }
            }
        }

        /// RETRIEVE_1D_MESH
        /// <summary>
        /// Retrieve the name of the 1D mesh corresponding to the 0th refinement and no inflation variant
        /// </summary>
        /// <param name="refinement"> Refinement number (Default: 0 (coarse grid))</param>
        /// <returns> Filename of refined or unrefined 1D mesh in archive </returns>
        public string Retrieve1DMeshName(int refinement = 0)
        {
            Load();
            int index = geometry.geom1d.ToList().FindIndex(geom => Int16.Parse(geom.refinement) == refinement);
            return geometry.geom1d[index != -1 ? index : 0].name;
        }

        /// RETRIEVE_2D_MESH
        /// <summary>
        /// Retrieve the name of the 2D mesh corresponding to the inflation and refinement factor from the .vrn archive
        /// </summary>
        /// <param name="inflation"> Inflation factor (default: 1.0 (no inflation)) </param>
        /// <param name="refinement"> Refinement level (default: 0 (no refinement)) </param>
        /// <returns> Filename of inflated 2D mesh in archive </returns>
        public string Retrieve2DMeshName(double inflation = 1.0, int refinement = 0)
        {
            Load();

            int index = geometry.geom1d.ToList().FindIndex(geom => Int16.Parse(geom.refinement) == refinement);
            int index2 = geometry.geom1d[index].inflations.ToList().FindIndex(geom => Double.Parse(geom.inflation) == inflation);
            return geometry.geom1d[index].inflations[index2 != -1 ? index2 : 0].name;
        }

        /// VRNREADER
        /// <summary>
        /// Supplies the reader with an .vrn archive to load and manipulate
        /// </summary>
        /// <param name="fileName"> Archive's file name (.vrn file) </param>
        public VrnReader(in string fileName) => this.fileName = fileName;

        /// EXAMPLE
        /// <summary>
        /// Example demonstrating vrnReader usage from command line (Not Unity)
        /// </summary>
        /// On start a test file (test.vrn) is read from the assets folder
        /// The coarse 1d mesh is retrieved as well as the by a factor of 
        /// 2.5 inflated 2d mesh is retrieved from the archive test.vrn
        static class Example
        {
            static void Main(string[] args)
            {
                string fileName = "testNew.vrn";
                try
                {
                    /// Instantiate the VRN reader with the desired file name
                    VrnReader reader = new VrnReader(fileName);
                    /// Get the name of the 1d mesh (0-th refinement aka coarse grid)
                    Console.WriteLine(reader.Retrieve1DMeshName());
                    /// Get the name of the inflated 2d mesh by a factor of 2.5
                    Console.WriteLine(reader.Retrieve2DMeshName(2.5));
                }
                catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.ArgumentNullException)
                {
                    Console.Error.WriteLine($"Archive not found or unable to open the .vrn archive: {fileName}.");
                    Console.Error.WriteLine(ex);
                }
                catch (Exception ex) when (ex is CouldNotReadMeshFromVRNArchive)
                {
                    Console.Error.WriteLine($"Requested mesh not contained in MetaInfo.json or given .vrn archive {fileName}.");
                    Console.Error.WriteLine(ex);
                }
            }
        }
    }
    /// <summary>
    /// Custom exception thrown if file could not be found in VRN archive
    /// </summary>
    /// <see cref="Exception"> Exception base class </see>
    [Serializable]
    public class CouldNotReadMeshFromVRNArchive : Exception
    {
        public CouldNotReadMeshFromVRNArchive() : base() { }
        public CouldNotReadMeshFromVRNArchive(string message) : base(message) { }
        public CouldNotReadMeshFromVRNArchive(string message, Exception inner) : base(message, inner) { }
        protected CouldNotReadMeshFromVRNArchive(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt) { }
    }
}
