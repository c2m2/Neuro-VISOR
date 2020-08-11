#region includes
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System;
using System.Linq;
#endregion

namespace vrn
{
    public sealed class vrnReader
    {
        /// GEOMETRY
        /// <summary>
        /// Stores 1D and 2D geometries
        /// </summary>
        private sealed class Geometry
        {
            /// GEOM1D
            /// <summary>
            /// Stores the 1D geometries by name and refinement 
            /// </summary>
            public sealed class Geom1d
            {
                public string name { get; set; }
                public string refinement { get; set; }
            }

            /// GEOM2D
            /// <summary>
            /// Stores the 2d geometries by name and inflation
            /// </summary>
            public sealed class Geom2d
            {
                public string name { get; set; }
                public string inflation { get; set; }
            }

            /// All 1D geometries
            public List<Geom1d> geom1d { get; set; }
            /// All 2D geometries
            public List<Geom2d> geom2d { get; set; }
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
                geometry = JsonSerializer.Deserialize<Geometry>(File.ReadAllText(file.FullName).ToString());
                loaded = true;
            }
        }

        /// READ
        /// <summary>
        /// Read a file from the .vrn archive
        /// </summary>
        private string read(in string meshName)
        {
            string name = Path.GetTempPath() + Path.GetRandomFileName();
            using (ZipArchive archive = ZipFile.Open(this.fileName, ZipArchiveMode.Read))
            {
                var file = archive.GetEntry(meshName);
                _ = file ?? throw new ArgumentNullException(nameof(file));
                file.ExtractToFile(name);
            }
            return name;
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
            int index = geometry.geom1d.FindIndex
            (geom => Int16.Parse(geom.refinement) == refinement);
            return(read(geometry.geom1d[index != -1 ? index : 0].name));
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
            int index = geometry.geom2d.FindIndex
            (geom => Double.Parse(geom.inflation) == inflation);
            return read(geometry.geom2d[index != -1 ? index : 0].name);
        }

        /// VRNREADER
        /// <summary>
        /// constructor 
        /// </summary>
        /// <param name="fileName"> Archive's file name (.vrn file) </param>
        public vrnReader(in string fileName) => this.fileName = fileName;
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
