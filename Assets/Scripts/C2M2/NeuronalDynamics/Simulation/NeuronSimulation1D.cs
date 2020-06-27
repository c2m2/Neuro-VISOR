using System.Collections.Generic;
using UnityEngine;
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
    /// <summary>
    /// Provide an interface for 1D neuron-surface simulations to be visualized and interacted with
    /// </summary>
    /// <remarks>
    /// 1D Neuron surface simulations should derive from this class.
    /// </remarks>
    public abstract class NeuronSimulation1D : SurfaceSimulation
    {
        [Header("1D Visualization")]
        public bool visualize1D = false;
        public Color32 color1D = Color.yellow;
        public float lineWidth1D = 0.005f;
        protected Grid grid1D;

        ///<summary> Lookup a 3D vert and get back two 1D indices and a lambda value for them </summary>
        private Dictionary<int, Tuple<int, int, double>> map;
        private MappingInfo mapping;
        private string[] cellFileNames;

        // Cell folder names and extensions
        private readonly string neuronCellFolder = "NeuronalDynamics";
        private readonly string activeCellFolder = "ActiveCell";
        private readonly string ugxExt = ".ugx";
        private readonly string cngExt = ".CNG";
        private readonly string spec1D = "_1d";
        private readonly string specTris = "_tris";
        private readonly string specBlownup = "_blown_up";

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
            cellFileNames = BuildCellFileNames();
            // Read in 1D & 3D data and build a map between them
            mapping = MapUtils.BuildMap(cellFileNames[1], cellFileNames[0], false, cellFileNames[2]);
            map = mapping.Data;
            // Pass the cell to simulation code
            SetNeuronCell(mapping.ModelGeometry);

            string[] BuildCellFileNames()
            {
                string[] cells = new string[4];
                cells[3] = "NULL";

                char slash = Path.DirectorySeparatorChar;
                string cellPath = Application.streamingAssetsPath + slash + neuronCellFolder + slash + activeCellFolder + slash;
                // Only take the first cell found
                cellPath = Directory.GetDirectories(cellPath)[0];

                string[] files = Directory.GetFiles(cellPath);
                foreach (string file in files)
                {
                    // If this isn't a non-metadata ugx file,
                    if (!file.EndsWith(".meta") && file.EndsWith(ugxExt))
                    {
                        if (file.EndsWith(cngExt + ugxExt)) cells[0] = file;    // 3D cell
                        else if (file.EndsWith(cngExt + spec1D + ugxExt)) cells[1] = file;  // 1D cell
                        else if (file.EndsWith(cngExt + specTris + ugxExt)) cells[2] = file;    // Triangles
                        else if (file.EndsWith(cngExt + specBlownup + ugxExt)) cells[3] = file;    // Blown up mesh
                    }
                }
                return cells;
            }
        }

        /// <summary>
        /// Read in the cell and initialize 3D/1D visualization/interaction infrastructure
        /// </summary>
        /// <returns> Unity Mesh visualization of the 3D geometry. </returns>
        protected override Mesh BuildVisualization()
        {            

            Mesh newMesh = new Mesh();
            if (!dryRun)
            {
                newMesh = Clean3DCell();
                if (visualize1D) Render1DCell();
                CheckForCustomCollider();
            }

            return newMesh;

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
            void CheckForCustomCollider()
            {
                // If a blownup mesh file is given, read it in and apply it
                if (!cellFileNames[3].Equals("NULL"))
                {
                    Mesh blownupMesh = MapUtils.BuildMap(cellFileNames[3], cellFileNames[0], false, cellFileNames[2]).SurfaceGeometry.Mesh;
                    MeshColController meshColController = gameObject.AddComponent<MeshColController>();
                    meshColController.mesh = blownupMesh;
                }
            }
        }
    }
}