using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using C2M2.NeuronalDynamics.UGX;
using C2M2.Utils.DebugUtils;
using C2M2.Utils.MeshUtils;
using C2M2.Interaction;
using C2M2.Simulation;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;

namespace C2M2.NeuronalDynamics.Simulation
{
    public struct CellPathPacket
    {
        public string name { get; private set; }
        public string path1D { get; private set; }
        public string path3D { get; private set; }
        public string pathTris { get; private set; }

        public CellPathPacket(string path1D, string path3D, string pathTris, string name = "")
        {
            this.name = name;
            this.path1D = path1D;
            this.path3D = path3D;
            this.pathTris = pathTris;
        }
        /// <summary>
        /// Provide the absolute path to a directory containing all of the files
        /// </summary>
        public CellPathPacket(string sourceDir, string name = "")
        {
            this.name = name;
            // Default values
            this.path1D = "NULL";
            this.path3D = "NULL";
            this.pathTris = "NULL";

            string[] files = Directory.GetFiles(sourceDir);
            foreach (string file in files)
            {
                // If this isn't a non-metadata ugx file,
                if (!file.EndsWith(".meta") && file.EndsWith(".ugx"))
                {
                    if (file.EndsWith("_1d.ugx")) path1D = file;  // 1D cell
                    else if (file.EndsWith("tris.ugx")) pathTris = file;    // Triangles
                    else if (file.EndsWith(".ugx")) path3D = file;     // If it isn't specified as 1D or triangles, it's most likely 3D
                }
            }
        }
    }
    /// <summary>
    /// Provide an interface for 1D neuron-surface simulations to be visualized and interacted with
    /// </summary>
    /// <remarks>
    /// 1D Neuron surface simulations should derive from this class.
    /// </remarks>
    public abstract class NeuronSimulation1D : MeshSimulation
    {   
        public enum MeshColScaling { x1, x2, x3, x4, x5 }
        [Header("3D Visualization")]
        [Tooltip("Which mesh scale to use for the mesh collider used for raycasting. Larger meshes will be easier to interact with, but less accurate")]
        public MeshColScaling meshColScale = MeshColScaling.x1;
        public enum RefinementLevel { x0, x1, x2, x3, x4 }
        public RefinementLevel refinementLevel = RefinementLevel.x1;

        public string cell1xPath = "NULL";
        public string cell2xPath = "NULL";
        public string cell3xPath = "NULL";
        public string cell4xPath = "NULL";
        public string cell5xPath = "NULL";

        /*
        public string cell1DPath = "NULL";
        public string cell3DPath = "NULL";
        public string cellTrianglesPath = "NULL";
        public string cell3DColliderPath = "NULL";
        public string cellTrianglesColliderPath = "NULL";
        */
    

        [Header("1D Visualization")]
        public bool visualize1D = false;
        public Color32 color1D = Color.yellow;
        public float lineWidth1D = 0.005f;

        protected Grid grid1D;

        ///<summary> Lookup a 3D vert and get back two 1D indices and a lambda value for them </summary>
        private Dictionary<int, Tuple<int, int, double>> map;
        private MappingInfo mapping;


        /// <summary>
        /// Translate 1D vertex values to 3D values and pass them upwards for visualization
        /// </summary>
        /// <returns> One scalar value for each 3D vertex based on its 1D vert's scalar value </returns>
        public sealed override double[] GetValues()
        {
            double[] scalars1D = Get1DValues();

            if (scalars1D == null) { return null; }

            double[] scalars3D = new double[map.Count];
            for (int i = 0; i < map.Count; i++)
            { // for each 3D point,
                int v1Da = map[i].Item1;
                int v1Db = map[i].Item2;

                double lambda = map[i].Item3;
                // Get original 1D values:
                double val1Da = scalars1D[v1Da];
                double val1Db = scalars1D[v1Db];
                // Take an weighted average using lambda
                // Equivalent to [lambda * val1Db + (1 - lambda) * val1Da]        
                double newVal = lambda * (val1Db - val1Da) + val1Da;
                scalars3D[i] = newVal;
            }

            return scalars3D;
        }
        /// <summary>
        /// Translate 3D vertex values to 1D values, and pass them downwards for interaction
        /// </summary>
        public sealed override void SetValues(RaycastHit hit)
        {
            Tuple<int, double>[] newValues = RaycastSimHeaterDiscrete.HitToTriangles(hit);

            // Each 3D index will have TWO associated 1D vertices
            Tuple<int, double>[] new1DValues = new Tuple<int, double>[2 * newValues.Length];
            int j = 0;
            for (int i = 0; i < newValues.Length; i++)
            {
                // Get 3D vertex index
                int vert3D = newValues[i].Item1;
                double val3D = newValues[i].Item2;

                // Translate into two 1D vert indices and a lambda weight
                double lambda = map[vert3D].Item3;
                double val1D = (1 - lambda) * val3D;
                new1DValues[j] = new Tuple<int, double>(map[vert3D].Item1, val1D);

                // Weight newVal by (lambda) for second 1D vert                    
                val1D = lambda * val3D;
                new1DValues[j + 1] = new Tuple<int, double>(map[vert3D].Item2, val1D);

                // Move up two spots in 1D array
                j += 2;
            }

            // Send 1D-translated scalars to simulation
            Set1DValues(new1DValues);
        }

        /// <summary>
        /// Requires deived classes to know how to receive one value to add onto each 1D vert index
        /// </summary>
        /// <param name="newValuess"> List of 1D vert indices and values to add onto that index. </param>
        protected abstract void Set1DValues(Tuple<int, double>[] newValuess);

        /// <summary>
        /// Requires derived classes to know how to make available one value for each 1D vertex
        /// </summary>
        /// <returns></returns>
        protected abstract double[] Get1DValues();

        /// <summary>
        /// Pass the UGX 1D and 3D cells to simulation code
        /// </summary>
        /// <param name="grid"></param>
        protected abstract void SetNeuronCell(Grid grid);

        protected override void ReadData()
        {
            CellPathPacket pathPacket = new CellPathPacket(cell1xPath, "1xDiameter");   
            ReadCellFiles(pathPacket);
        }

        private void ReadCellFiles(CellPathPacket cellPaths)
        {
            // Read in 1D & 3D data and build a map between them
            Debug.Log("Cell1d: " + cellPaths.path1D + "\ncell3d: " + cellPaths.path3D + "\ncellTris: " + cellPaths.pathTris);
            mapping = MapUtils.BuildMap(cellPaths.path1D, cellPaths.path3D, false, cellPaths.pathTris);
            map = mapping.Data;

            // Pass the cell to simulation code
            SetNeuronCell(mapping.ModelGeometry);
        }

        /// <summary>
        /// Read in the cell and initialize 3D/1D visualization/interaction infrastructure
        /// </summary>
        /// <returns> Unity Mesh visualization of the 3D geometry. </returns>
        protected override Mesh BuildVisualization()
        {            
            Mesh cellMesh = new Mesh();
            if (!dryRun)
            {
                cellMesh = Clean3DCell();
                if (visualize1D) Render1DCell();
                BuildMeshCollider();
            }

            return cellMesh;

            Mesh Clean3DCell()
            {
                Mesh mesh = mapping.SurfaceGeometry.Mesh;
                mesh.Rescale(transform, new Vector3(4, 4, 4));
                mesh.RecalculateNormals();
                return mesh;
            }

            void Render1DCell()
            {
                Grid geom1D = mapping.ModelGeometry;
                GameObject lines1D = gameObject.AddComponent<LinesRenderer>().Constr(geom1D, color1D, lineWidth1D);
            }

            // Returns whichever mesh is used for the mesh collider
            Mesh BuildMeshCollider()
            {
                MeshColController meshColController = gameObject.AddComponent<MeshColController>();
                Mesh blownupMesh = null;


                // Build blownup mesh name
                CellPathPacket cellPathPacket = new CellPathPacket();
                string scale = "";
                switch (meshColScale)
                {
                    case (MeshColScaling.x1):
                        blownupMesh = cellMesh;
                        break;
                    case (MeshColScaling.x2):
                        cellPathPacket = new CellPathPacket(cell2xPath);
                        scale = "x2";
                        break;
                    case (MeshColScaling.x3):
                        cellPathPacket = new CellPathPacket(cell3xPath);
                        scale = "x3";
                        break;
                    case (MeshColScaling.x4):
                        cellPathPacket = new CellPathPacket(cell4xPath);
                        scale = "x4";
                        break;
                    case (MeshColScaling.x5):
                        cellPathPacket = new CellPathPacket(cell5xPath);
                        scale = "x5";
                        break;
                    default:
                        Debug.LogError("Cannot resolve mesh scale");
                        break;
                }

                // Use 1x scaling as the default case
                if (meshColScale != MeshColScaling.x1)
                {
                    blownupMesh = MapUtils.BuildMap(cellPathPacket.path3D,
                        cellPathPacket.path1D,
                        false,
                        cellPathPacket.pathTris).SurfaceGeometry.Mesh;             
                }

                blownupMesh.name = blownupMesh.name + scale;

                // Pass blownupMesh upwards to SurfaceSimulation
                colliderMesh = blownupMesh;

                return blownupMesh;
            }
        }
    }
}