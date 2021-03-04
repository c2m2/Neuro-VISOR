#region using
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using KdTree;
using KdTree.Math;
using UnityEditor;
using UnityEngine;
#endregion

namespace C2M2.NeuronalDynamics.UGX {
    using DiameterAttachment = IAttachment<DiameterData>;
    using MappingAttachment = IAttachment<MappingData>;
    /// MappingInfo
    /// <summary>
    /// Encapsulates the grids and mapping data
    /// </summary>
    /// Geometries will be stored in ModelGeometry (1d geometry) and SurfaceGeometry (2d geometry)
    /// The dictionary maps any 2d surface vertex, using its index in the 2d mesh, to two
    /// 1d vertices with their associated indices in the 1d mesh and a lambda parameter.
    public readonly struct MappingInfo {
        public Grid ModelGeometry { get; }
        public Grid SurfaceGeometry { get; }
        /// <summary>
        /// Look up a 3D vertex, get back its two corresponding 1D vertices and a lambda value representing 
        /// </summary>
        /// <remarks>
        /// Lambda is a value between 0 and 1. If a 3D vertex is given a value of 100, and it's lambda is 0.7,
        /// then the 1D vert stored at Data[vert3D].Entry1 should get a value of 30, and Data[vert3D].Entry2 should get a value of 70.
        /// </remarks>
        public Dictionary<int, Tuple<int, int, double>> Data { get; }

        /// <summary>
        /// Access the model geometry, surface geometry and mapping data
        /// </summary>
        /// <param name="grid1d"> 1d model geometry </param>
        /// <param name="grid2d"> Surface geoemtry </param>
        /// <param name="map2d1d"> 2d to 1d mapping data </param>
        public MappingInfo (in Grid grid1d, in Grid grid2d, in Dictionary<int, Tuple<int, int, double>> map2d1d) {
            ModelGeometry = grid1d;
            SurfaceGeometry = grid2d;
            Data = map2d1d;
        }
    }

    /// <summary>
    /// A map entry represents mapping data for one vertex pair v1, v2 and a scalar lambda
    /// Note: Lambda ist used to determine how to interpolate between v1 and v2
    /// </summary>
    public readonly struct MapEntry {
        public readonly int v1;
        public readonly int v2;
        public readonly double lambda;
    }
    /// MapUtils
    /// <summary>
    /// Utility class for all mapping associated utility functions
    /// </summary>
    static class MapUtils {
        /// BuildMap
        /// <summary>
        /// Build the mapping from two grid objects
        /// </summary>
        /// This delegates to the (private) helper build method:
        /// <see cref="MapUtils.Build(in Grid, in Grid, in Grid)()"/>
        /// <param name="grid1d"> 1D mesh </param>
        /// <param name="grid2d"> 2D mesh </param>
        /// <param name="grid2dvis"> 2D mesh for visualization (Default: null)</param>
        /// <returns> mapping information as MappingInfo or null if grids not consistent </param>
        public static MappingInfo? BuildMap (in Grid grid1d, in Grid grid2d, in Grid grid2dvis = null) {
            /// 1d mesh needs a diameter
            if (!grid1d.HasVertexAttachment<DiameterAttachment> ()) {
                throw new MapNotBuildException ("1d mesh needs a diameter attachment");
            }

            /// 2d mesh needs a mapping
            if (!grid2d.HasVertexAttachment<MappingAttachment> ()) {
                throw new MapNotBuildException ("2d needs a mapping attachment");
            }

            /// otherwise can build map
            return Build (grid2d, grid1d, grid2dvis ?? grid2d);
        }

        /// Build
        /// <summary>
        /// Helper method to which other methods can delegate to
        /// </summary>
        /// <param name="grid2d"> 2D mesh </param>
        /// <param name="grid1d"> 1D mesh </param>
        /// <param name="grid2dvis"> 2D mesh for visualization </param>
        /// <returns> mapping information as MappingInfo </returns>
        private static MappingInfo Build (in Grid grid2d, in Grid grid1d, in Grid grid2dvis) {
            // Accessors for attachments and map
            VertexAttachementAccessor<MappingData> accessor = new VertexAttachementAccessor<MappingData> (grid2d);
            Dictionary<int, Tuple<int, int, double>> map2d1d = new Dictionary<int, Tuple<int, int, double>> ();
            int size1d = grid1d.Mesh.vertices.Length;
            int size3d = grid2d.Mesh.vertices.Length;
            if (size1d == 0) {
                throw new MapNotBuildException ($"1D mesh ({grid1d.Mesh.name}) has size 0 - how to build a mapping?");
            }

            if (size3d == 0) {
                throw new MapNotBuildException ($"2D mesh ({grid2d.Mesh.name}) has size 0 - how to build a mapping?");
            }

            Vector3[] vertices = grid1d.Mesh.vertices;

            KdTree<float, int> tree = new KdTree<float, int> (3, new FloatMath ());
            for (int i = 0; i < size1d; i++) {
                tree.Add (new float[] { vertices[i].x, vertices[i].y, vertices[i].z }, i);
            }

            
            for (int i = 0; i < size3d; i++) {
                var node1 = tree.GetNearestNeighbours (new [] { accessor[i].Start[0], accessor[i].Start[1], accessor[i].Start[2] }, 1);
                var node2 = tree.GetNearestNeighbours (new [] { accessor[i].End[0], accessor[i].End[1], accessor[i].End[2] }, 1);
                map2d1d[i] = new Tuple<int, int, double> (node1[0].Value, node2[0].Value, accessor[i].Lambda);
            }

            return new MappingInfo (grid1d, grid2dvis, map2d1d);
        }

        /// BuildMap
        /// <summary>
        /// Build the meshes of the model geometry and surface geometry as well as the 2d->1d mapping
        /// Thus the additional geom3dtris geometry will be discarded in the future. 
        /// </summary>
        /// This delegates to the (private) helper build method:
        /// <see cref="MapUtils.Build(in Grid, in Grid, in Grid)()"/>
        /// <param name="geom1d"> Filename of 1d (model) geometry </param>
        /// <param name="geom2d"> Filename of 2d (surface) geometry with mapping data </param>
        /// <param name="geomTris"> Filename of 2d (surface) geometry for visualitation </param>
        /// <param name="validate"> Validate XML file (default is not to validate) </param>
        /// <returns> The mapping information as MappingInfo </returns>
        /// <see cref="MappingInfo"> MappingInfo struct for details </see>
        /// Note: Mapping data could be encapsulated also in the triangulated mesh, thus eliminating the third mesh parameter
        public static MappingInfo
        BuildMap (in string geom1d, in string geom2d, in bool validate = false, in string geomTris = null) {
            // Model geometry consisting ouf of edges and vertices
            Grid grid1d = new Grid (new Mesh (), "1D geom");
            grid1d.Attach (new DiameterAttachment ());
            UGXReader.Validate = validate;
            UGXReader.ReadUGX (geom1d, ref grid1d);

            // Surface geometry consisting out of triangles and does not need diameters
            Grid grid2dvis = new Grid (new Mesh (), "2D geom tris");
            UGXReader.Validate = validate;
            if (geomTris != null) { UGXReader.ReadUGX (geomTris, ref grid2dvis); }

            // Surface geometry with mapping data
            Grid grid2d = new Grid (new Mesh (), "2D geom mapping");
            grid2d.Attach (new MappingAttachment ());

            UGXReader.Validate = validate;
            UGXReader.ReadUGX (geom2d, ref grid2d);

            /// build the mapping
            return Build (grid2d, grid1d, grid2dvis);
        }

        /// <summary>
        /// Delegate to BuildMap without a special visualization surface geometry
        /// </summary>
        /// <param name="geom1d"></param>
        /// <param name="geom2d"></param>
        /// <param name="validate"></param>
        /// <returns> MappingInfo </returns>
        public static MappingInfo BuildMap (in string geom1d, in string geom2d, in bool validate = false) {
            return BuildMap (geom1d, geom2d, validate, null);
        }

        /// <summary>
        /// Delegate to build a default map for testing purposes
        /// </summary>
        public static MappingInfo BuildMap () {
            return BuildMap (@"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_regularize.ugx",
                @"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_selecting_boundary_elements.ugx",
                false,
                @"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Mapping/after_selecting_boundary_elements_tris.ugx");
        }
    }

    /// <summary>
    /// Custom exception thrown if map could not be build
    /// </summary>
    /// <see cref="Exception"> See exception base class </see>
    [Serializable]
    public class MapNotBuildException : Exception {
        public MapNotBuildException () : base () { }
        public MapNotBuildException (string message) : base (message) { }
        public MapNotBuildException (string message, Exception inner) : base (message, inner) { }
        protected MapNotBuildException (SerializationInfo info, StreamingContext ctxt) : base (info, ctxt) { }
    }
}
