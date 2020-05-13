#region using
using KdTree;
using KdTree.Math;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#endregion

namespace C2M2.NeuronalDynamics.UGX
{
    using DiameterAttachment = IAttachment<DiameterData>;
    using MappingAttachment = IAttachment<MappingData>;

    /// MappingInfo
    /// <summary>
    /// Encapsulates the grids and mapping data
    /// </summary>
    /// Geometries will be stored in ModelGeometry (1d geometry) and SurfaceGeometry (2d geometry)
    /// The dictionary maps any 2d surface vertex, using its index in the 2d mesh, to two
    /// 1d vertices with their associated indices in the 1d mesh and a lambda parameter.
    public readonly struct MappingInfo
    {
        public Grid ModelGeometry { get; }
        public Grid SurfaceGeometry { get; }
        public Dictionary<int, Tuple<int, int, double>> Data { get; }

        /// <summary>
        /// Access the model geometry, surface geometry and mapping data
        /// </summary>
        public MappingInfo(in Grid grid1d, in Grid grid2d, in Dictionary<int, Tuple<int, int, double>> map2d1d)
        {
            ModelGeometry = grid1d;
            SurfaceGeometry = grid2d;
            Data = map2d1d;
        }
    }
    // TODO: Replace Tuple<int, int, double> with this struct
    public readonly struct MapEntry
    {
        public readonly int v1;
        public readonly int v2;
        public readonly double lambda;
    }
    /// MapUtils
    /// <summary>
    /// Utility class for all mapping associated utility functions
    /// </summary>
    static class MapUtils
    {
        /// BuildMap
        /// <summary>
        /// Build the meshes of the model geometry and surface geometry as well as the 2d->1d mapping
        /// TODO: New optimized meshes produced with ug4 will be triangulated and contain the mapping data.
        /// Thus the additional geom3dtris geometry will be discarded in the future. 
        /// </summary>
        /// <param name="geom1d"> Filename of 1d (model) geometry </param>
        /// <param name="geom2d"> Filename of 2d (surface) geometry with mapping data </param>
        /// <param name="geomTris"> Filename of 2d (surface) geometry for visualitation </param>
        /// <param name="validate"> Validate XML file (default is not to validate) </param>
        /// <returns> The mapping information </returns>
        /// <see cref="MappingInfo"> MappingInfo struct for details </see>A
        public static MappingInfo
            BuildMap(in string geom1d, in string geom2d, in bool validate = false, in string geomTris = null)
        {
            // Model geometry consisting ouf of edges and vertices
            Grid grid1d = new Grid(new Mesh(), "1D geom");
            grid1d.Attach(new DiameterAttachment());
            UGXReader.Validate = validate;
            UGXReader.ReadUGX(geom1d, ref grid1d);

            // Surface geometry consisting out of triangles and does not need diameters
            Grid grid2dvis = new Grid(new Mesh(), "2D geom tris");
            UGXReader.Validate = validate;
            if (geomTris != null) { UGXReader.ReadUGX(geomTris, ref grid2dvis); }
            Debug.Log(grid2dvis);

            // Surface geometry with mapping data
            Grid grid2d = new Grid(new Mesh(), "2D geom mapping");
            grid2d.Attach(new MappingAttachment());

            UGXReader.Validate = validate;
            UGXReader.ReadUGX(geom2d, ref grid2d);
            Debug.Log(grid2d);

            // Accessors for attachments and map
            VertexAttachementAccessor<MappingData> accessor = new VertexAttachementAccessor<MappingData>(grid2d);
            Dictionary<int, Tuple<int, int, double>> map2d1d = new Dictionary<int, Tuple<int, int, double>>();
            int size1d = grid1d.Mesh.vertices.Length;
            int size3d = grid2d.Mesh.vertices.Length;

            Vector3[] vertices = grid1d.Mesh.vertices;

            KdTree<float, int> tree = new KdTree<float, int>(3, new FloatMath());
            for (int i = 0; i < size1d; i++)
            {
                tree.Add(new float[] { vertices[i].x, vertices[i].y, vertices[i].z }, i);
            }

            for (int i = 0; i < size3d; i++)
            {
                var node = tree.GetNearestNeighbours(new[] { accessor[i].Start[0], accessor[i].Start[1], accessor[i].Start[2] }, 1);
                map2d1d[i] = new Tuple<int, int, double>(node[0].Value, node[0].Value, accessor[i].Lambda);
            }

            return new MappingInfo(grid1d, grid2dvis, map2d1d);
        }

        /// <summary>
        /// Delegate to BuildMap without a special visualization surface geometry
        /// </summary>
        /// <param name="geom1d"></param>
        /// <param name="geom2d"></param>
        /// <param name="validate"></param>
        /// <returns></returns>
        public static MappingInfo BuildMap(in string geom1d, in string geom2d, in bool validate = false)
        {
            return BuildMap(geom1d, geom2d, validate, null);
        }

        public static MappingInfo BuildMap()
        {
            return BuildMap(@"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_regularize.ugx",
                                                 @"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_selecting_boundary_elements.ugx",
                                                 false,
                                                 @"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_selecting_boundary_elements_tris.ugx");
        }
    }
}

