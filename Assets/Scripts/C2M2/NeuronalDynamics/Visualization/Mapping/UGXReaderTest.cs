#define ENABLE_PROFILER // Not strictly necessary

#region using
using KdTree;
using KdTree.Math;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
#endregion

namespace C2M2.NeuronalDynamics.UGX
{
    using DiameterAttachment = IAttachment<DiameterData>;
    using MappingAttachment = IAttachment<MappingData>;

    /// <summary>
    /// UGXReaderTest 
    /// </summary>
    /// Tests the UGXReader on a simple static CNG cell from NeuroMorpho database
    public sealed class UGXReaderTest : MonoBehaviour
    {
        [Header("CNG Cell")]
        public Cell cell;

        public enum Cell : byte
        {
            cell1 = 1,
            cell2 = 2,
            Cell10_6vkd1m = 3,
            Cell0_2a,
            Cell0_2b,
            Cell10_6kdv2 = 11
        }

        #region private
        private static GameObject go;
        private static Grid grid;
        #endregion
        /// <summary>
        /// Start
        /// </summary>
        void Start()
        {
            /// Which common attachments are available?
            AttachmentHandler.Available();

            /// Create an Empty grid
            grid = new Grid(new Mesh(), "3D cube");

            /// Which attachments should be populated during grid read-in process.
            /// The data is stored in the DiameterAttachment internally, and the type is DiameterData
            grid.Attach(new DiameterAttachment());

            /// Read the file with attachments specified by a list above
            // UGXReader.ReadUGX(@"C:/Users/tug41634/Desktop/cube_3d.ugx", ref grid);
            // UGXReader.ReadUGX(@"C:/Users/tug41634/Downloads/mouse4_standard_joined.ugx", ref grid);

            /// Additional attachments can be attached to the grid which will be populated with the default value for each vertex
            /// VertexAttachmentAccesors need to be created, see below, to access the attached data objects

            /// Debug for mesh
            // Debug.Log($"mesh.vertices.Length: {grid.Mesh.vertices.Length}");
            // Debug.Log($"mesh.triangles.Length: {grid.Mesh.triangles.Length}");

            /// Add visual components
            go = new GameObject(grid.Mesh.name);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            // go.GetComponent<MeshFilter>().sharedMesh = grid.Mesh;

            /// Example usage of Diameter attachment
            VertexAttachementAccessor<DiameterData> accessor = new VertexAttachementAccessor<DiameterData>(grid);
            grid.Attach(new MappingAttachment());

            /// Iterate through diameter data
            foreach (DiameterData diam in accessor)
            {
                Debug.Log($"Diameter {diam.Diameter}");
            }

            /// CNG cells
            bool defined = Enum.IsDefined(typeof(Cell), (byte)cell);
            if (defined)
            {
                var name = (Cell)(byte)cell;
                string cellName = name.ToString();
                cellName = cellName.Replace("_", "-");
                cellName = cellName.Replace("Cell", "");
                /// Build the map
                Debug.Log($"Building mesh for cell: {cellName}");
                /*  
                 BuildMap(@"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Cells/" + (byte) cell + "/" + cellName + ".CNG_1d.ugx",
                          @"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Cells/" + (byte) cell + "/" + cellName + ".CNG.ugx",
                          @"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Cells/" + (byte) cell + "/" + cellName + ".CNG_tris.ugx");
                 */
                /// Surface geom (With triangles)
                Grid grid3dvis = new Grid(new Mesh(), "3D Hippocampal Cell");
                UGXReader.Validate = false;
                UGXReader.ReadUGX(@"C:/Users/tug41634/Desktop/new mesh.ugx", ref grid3dvis);
                Debug.Log(grid3dvis);
                go.GetComponent<MeshFilter>().sharedMesh = grid3dvis.Mesh;
            }
        }

        /// <summary>
        /// BuildMap
        /// </summary>
        private static void BuildMap() => BuildMap(@"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_regularize.ugx",
                                                  @"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_selecting_boundary_elements.ugx",
                                                  @"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_selecting_boundary_elements_tris.ugx");


        /// BuildMap
        /// <summary>
        /// Geometries will be stored in grid1d and grid3d (Unity meshes)
        /// </summary>
        /// <param name="geom1d"> Filename of 1d (model) geometry </param>
        /// <param name="geom3d"> Filename of 2d (surface) geometry with mapping data </param>
        /// <param name="geom3dvis"> Filename of 2d (surface) geometry for visualization </param>
        /// <returns> A tuple containing the grids (1d and 3d) and the mapping data as a Dictionary </returns>
        public static Tuple<Grid, Grid, Dictionary<int, Tuple<int, int, double>>> BuildMap(in string geom1d, in string geom3d, in string geom3dvis)
        {
            /// Model geom
            Grid grid1d = new Grid(new Mesh(), "1D Hippocampal Cell");
            grid1d.Attach(new DiameterAttachment());
            UGXReader.Validate = false;
            UGXReader.ReadUGX(geom1d, ref grid1d);
            Debug.Log(grid1d);

            Debug.Log("after 1d ");

            /// Surface geom (With triangles)
            Grid grid3dvis = new Grid(new Mesh(), "3D Hippocampal Cell");
            UGXReader.Validate = false;
            UGXReader.ReadUGX(geom3dvis, ref grid3dvis);
            Debug.Log(grid3dvis);
            go.GetComponent<MeshFilter>().sharedMesh = grid3dvis.Mesh;

            Debug.Log("after 3d vis");

            /// For mapping
            Grid grid3d = new Grid(new Mesh(), "3D Hippocampal Cell");
            grid3d.Attach(new MappingAttachment());
            UGXReader.Validate = false;
            UGXReader.ReadUGX(geom3d, ref grid3d);
            Debug.Log(grid3d);

            Debug.Log("after 3d");

            /// Each 2d (surface) vertex id is associated with two 1d (model) vertex indices and a Lambda parameter
            Profiler.BeginSample("BuildMap");

            VertexAttachementAccessor<MappingData> accessor = new VertexAttachementAccessor<MappingData>(grid3d);
            Dictionary<int, Tuple<int, int, double>> map2d1d = new Dictionary<int, Tuple<int, int, double>>();

            int size1d = grid1d.Mesh.vertices.Length;
            int size3d = grid3dvis.Mesh.vertices.Length;
            Vector3[] vertices = grid1d.Mesh.vertices;
            Vector3[] vertices2 = grid3dvis.Mesh.vertices;

            foreach (var edge in grid1d.Edges)
            {
                int from = edge.From.Id;
                int to = edge.To.Id;
                // Mapping.DrawLine(vertices[from], vertices[to], Color.red, Color.red);
            }


            /*
            Color[] colors = grid3dvis.Mesh.colors;
            for (int i = 0; i < size3d; i++)
            {
                colors[i] = Color.blue;
            }

            grid3dvis.Mesh.colors = colors;
            */

            KdTree<float, int> tree = new KdTree<float, int>(3, new FloatMath());
            for (int i = 0; i < size1d; i++)
            {
                tree.Add(new float[] { vertices[i].x, vertices[i].y, vertices[i].z }, i);
            }

            /// Depending on precision points are not the same (Vector3) when read from the .ugx and from the Unity mesh (double vs. float)
            for (int i = 0; i < size3d; i++)
            {
                var node = tree.GetNearestNeighbours(new[] { accessor[i].Start[0], accessor[i].Start[1], accessor[i].Start[2] }, 1); // current nearest neighbor
                Debug.Log("i:" + i);
                map2d1d[i] = new Tuple<int, int, double>(node[0].Value, node[0].Value, accessor[i].Lambda);
                // Mapping.DrawLine(vertices2[i], vertices[node[0].Value], Color.red, Color.green);
            }


            /*
              for (int i = 0; i < size3d; i++)
              {
                  Debug.Log("1d vert: " + accessor[i].Start);
                  int index1 = Array.IndexOf(vertices, accessor[i].Start); /// O(n): slow. FIXME: use kd tree
                  int index2 = Array.IndexOf(vertices, accessor[i].End); /// O(n): slow. FIXME: use kd tree
                  map2d1d[i] = new Tuple<int, int, double>(index1, index2, accessor[i].Lambda);
                  Debug.Log("index: " + index1);
                  if (index1 == -1 || index2 == -1)
                  {
                      Debug.LogError("Id geometry vertex not found: " + accessor[i].Start);
                      continue; 
                  }
                 // Mapping.DrawLine(vertices2[i], vertices[index1], Color.red, Color.green);
              }
              */
            Profiler.EndSample();
            go.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Surface"));
            return new Tuple<Grid, Grid, Dictionary<int, Tuple<int, int, double>>>(grid1d, grid3d, map2d1d);
        }

        /// <summary>
        /// Example for reading diameters from UGX files
        /// </summary>
        /// <param name="filename"> Name of file to read in as an example to verify diameters are read</param>
        private void DiameterExample(in string filename)
        {
            Grid grid = new Grid(new Mesh(), "Your mesh's name");
            UGXReader.ReadUGX(filename, ref grid);
            // Your mesh will be stored in grid instance and attachments can be accessed via (Which are stored within the grid):
            VertexAttachementAccessor<DiameterData> accessor = new VertexAttachementAccessor<DiameterData>(grid);
            foreach (var data in accessor)
            {
                Debug.Log($"Diameter {data.Diameter}");
            }
        }
    }
}
