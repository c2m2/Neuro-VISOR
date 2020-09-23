using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using C2M2.NeuronalDynamics.UGX;
using UnityEditor;
using UnityEngine;
using DiameterAttachment = C2M2.NeuronalDynamics.UGX.IAttachment<C2M2.NeuronalDynamics.UGX.DiameterData>;
using MappingAttachment = C2M2.NeuronalDynamics.UGX.IAttachment<C2M2.NeuronalDynamics.UGX.MappingData>;

using Math = C2M2.Utils.Math;
using C2M2.Interaction;
using C2M2.Simulation;
using C2M2.Utils.DebugUtils;
using C2M2.Utils.Exceptions;
using C2M2.Utils.MeshUtils;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
using System.Text;
using C2M2.NeuronalDynamics.Visualization.vrn;

namespace C2M2.NeuronalDynamics.Simulation {
    public struct CellPathPacket {
        public string name { get; private set; }
        public string path1D { get; private set; }
        public string path3D { get; private set; }
        public string pathTris { get; private set; }

        public CellPathPacket (string path1D, string path3D, string pathTris, string name = "") {
            this.name = name;
            this.path1D = path1D;
            this.path3D = path3D;
            this.pathTris = pathTris;
        }
        /// <summary>
        /// Provide the absolute path to a directory containing all of the files
        /// </summary>
        public CellPathPacket (string sourceDir, string name = "") {
            this.name = name;
            // Default values
            this.path1D = "NULL";
            this.path3D = "NULL";
            this.pathTris = "NULL";

            string[] files = Directory.GetFiles (sourceDir);
            foreach (string file in files) {
                // If this isn't a non-metadata ugx file,
                if (!file.EndsWith (".meta") && file.EndsWith (".ugx")) {
                    if (file.EndsWith ("_1d.ugx")) path1D = file; // 1D cell
                    else if (file.EndsWith ("_tris.ugx")) pathTris = file; // Triangles
                    else if (file.EndsWith (".ugx")) path3D = file; // If it isn't specified as 1D or triangles, it's most likely 3D
                }
            }
        }
    }
    /// <summary>
    /// Stores two 1D indices and a lambda value for a 3D vertex
    /// </summary>
    public struct Vert3D1DPair {
        public int v1 { get; private set; }
        public int v2 { get; private set; }
        public double lambda { get; private set; }

        public Vert3D1DPair (int v1, int v2, double lambda) {
            this.v1 = v1;
            this.v2 = v2;
            this.lambda = lambda;
        }
        public override string ToString () {
            return "v1: " + v1 + "\nv2: " + v2 + "\nlambda: " + lambda;
        }
    }

    public struct CellInfo {
        public string name { get; private set; }
        public string filePath { get; private set; }
        public MappingInfo data { get; private set; }

        public Mesh mesh {
            get {
                return data.SurfaceGeometry.Mesh;
            }
        }
        public CellInfo (string filePath, string name = "") {
            this.name = name;
            this.filePath = filePath;
            CellPathPacket paths = new CellPathPacket (filePath);

            if (paths.path3D != "NULL" && paths.path1D != "NULL" && paths.pathTris != "NULL") {
                data = MapUtils.BuildMap (paths.path3D, paths.path1D, false, paths.pathTris);
            } else {
                string s = "";
                if (paths.path3D == "NULL") s += " [3D Cell] ";
                if (paths.path1D == "NULL") s += " [1D Cell] ";
                if (paths.pathTris == "NULL") s += " [Cell Triangles] ";
                throw new NullReferenceException ("Null paths found for " + s);
            }

        }
    }
    /// <summary>
    /// Provide an interface for 1D neuron-surface simulations to be visualized and interacted with
    /// </summary>
    /// <remarks>
    /// 1D Neuron surface simulations should derive from this class.
    /// </remarks>
    public abstract class NeuronSimulation1D : MeshSimulation {
        private int refinementLevel = 1;
        public int RefinementLevel
        {
            get { return refinementLevel; }
            set
            {
                refinementLevel = value;
            }
        }

        private double visualInflation = 1;
        public double VisualInflation
        {
            get { return visualInflation; }
            set
            {
                visualInflation = value;
                if (ColliderInflation < visualInflation) ColliderInflation = visualInflation;
                VisualMesh = CheckMeshCache(visualInflation);
            }
        }

        private double colliderInflation = 1;
        public double ColliderInflation
        {
            get { return colliderInflation; }
            set
            {
                if (value < visualInflation) return;
                colliderInflation = value;
                ColliderMesh = CheckMeshCache(colliderInflation);
            }
        }
        private Mesh visualMesh = null;
        public Mesh VisualMesh
        {
            get
            {
                return visualMesh;
            }
            private set
            {
                //if (value == null) return;
                visualMesh = value;

                var mf = GetComponent<MeshFilter>();
                if(mf == null) gameObject.AddComponent<MeshFilter>();
                if (GetComponent<MeshRenderer>() == null)
                    gameObject.AddComponent<MeshRenderer>().sharedMaterial = GameManager.instance.vertexColorationMaterial;
                mf.sharedMesh = visualMesh;
            }
        }
        private Mesh colMesh = null;
        public Mesh ColliderMesh
        {
            get { return colMesh; }
            private set
            {
                //if (value == null) return;
                colMesh = value;

                var cont = GetComponent<MeshColController>() ?? GetComponentInChildren<MeshColController>();
                if (cont == null)
                {
                    cont = gameObject.AddComponent<MeshColController>();
                }
                cont.Mesh = colMesh;
                base.colliderMesh = colMesh;
            }
        }

        private Dictionary<double, Mesh> meshCache = new Dictionary<double, Mesh>();

        // Need mesh options for each refinement, diameter level
        public string vrnPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + "test.vrn";

        [Header ("1D Visualization")]
        public bool visualize1D = false;
        public Color32 color1D = Color.yellow;
        public float lineWidth1D = 0.005f;

        protected Grid grid1D;
        public Vector3[] Verts1D { get { return grid1D.Mesh.vertices; } }

        ///<summary> Lookup a 3D vert and get back two 1D indices and a lambda value for them </summary>
        //private Dictionary<int, Tuple<int, int, double>> map;
        private Vert3D1DPair[] map;
        private MappingInfo mapping;

        private double[] scalars3D = new double[0];

        /// <summary>
        /// Translate 1D vertex values to 3D values and pass them upwards for visualization
        /// </summary>
        /// <returns> One scalar value for each 3D vertex based on its 1D vert's scalar value </returns>
        public sealed override double[] GetValues () {
            double[] scalars1D = Get1DValues ();

            if (scalars1D == null) { return null; }
            //double[] scalars3D = new double[map.Length];
            for (int i = 0; i < map.Length; i++) { // for each 3D point,

                // Take an weighted average using lambda
                // Equivalent to [lambda * val1Db + (1 - lambda) * val1Da]
                double newVal = map[i].lambda * (scalars1D[map[i].v2] - scalars1D[map[i].v1]) + scalars1D[map[i].v1];

                scalars3D[i] = newVal;
            }
            // Debug.Log(sb.ToString());
            return scalars3D;
        }
        /// <summary>
        /// Translate 3D vertex values to 1D values, and pass them downwards for interaction
        /// </summary>
        public sealed override void SetValues (RaycastHit hit) {
            Tuple<int, double>[] newValues = RaycastSimHeaterDiscrete.HitToTriangles (hit);

            SetValues (newValues);
        }

        /// <summary>
        /// Translate 3D vertex values to 1D values, and pass them downwards for interaction
        /// </summary>
        public void SetValues (Tuple<int, double>[] newValues) {
            // Each 3D index will have TWO associated 1D vertices
            Tuple<int, double>[] new1DValues = new Tuple<int, double>[2 * newValues.Length];
            int j = 0;
            for (int i = 0; i < newValues.Length; i++) {
                // Get 3D vertex index
                int vert3D = newValues[i].Item1;
                double val3D = newValues[i].Item2;

                // Translate into two 1D vert indices and a lambda weight
                double val1D = (1 - map[vert3D].lambda) * val3D;
                new1DValues[j] = new Tuple<int, double> (map[vert3D].v1, val1D);

                // Weight newVal by (lambda) for second 1D vert
                val1D = map[vert3D].lambda * val3D;
                new1DValues[j + 1] = new Tuple<int, double> (map[vert3D].v2, val1D);
                // Move up two spots in 1D array
                j += 2;
            }

            // Send 1D-translated scalars to simulation
            Set1DValues (new1DValues);
        }

        /// <summary>
        /// Requires deived classes to know how to receive one value to add onto each 1D vert index
        /// </summary>
        /// <param name="newValuess"> List of 1D vert indices and values to add onto that index. </param>
        public abstract void Set1DValues (Tuple<int, double>[] newValuess);

        /// <summary>
        /// Requires derived classes to know how to make available one value for each 1D vertex
        /// </summary>
        /// <returns></returns>
        public abstract double[] Get1DValues ();

        /// <summary>
        /// Pass the UGX 1D and 3D cells to simulation code
        /// </summary>
        /// <param name="grid"></param>
        protected abstract void SetNeuronCell (Grid grid);

        vrnReader reader = null;
        protected override void ReadData () {
            /// This goes to StreamingAssets
            if (reader == null) reader = new vrnReader (vrnPath);
            Debug.Log ("Path: " + vrnPath);
            Debug.Log (reader.List ());

            string meshName1D = reader.Retrieve1DMeshName ();
            /// Create empty grid with name of grid in archive
            grid1D = new Grid (new Mesh (), meshName1D);
            grid1D.Attach (new DiameterAttachment ());
            
            reader.ReadUGX (meshName1D, ref grid1D);

            // Pass the cell to simulation code
            SetNeuronCell (grid1D);
        }

        /// <summary>
        /// Read in the cell and initialize 3D/1D visualization/interaction infrastructure
        /// </summary>
        /// <returns> Unity Mesh visualization of the 3D geometry. </returns>
        protected override Mesh BuildVisualization () {
            Mesh cellMesh = new Mesh ();
            if (!dryRun) {
                /// Retrieve mesh names from archive
                string meshName2D = reader.Retrieve2DMeshName ();
                string meshName1D = reader.Retrieve1DMeshName ();

                /// Empty 2D grid which stores geometry + mapping data
                Grid grid2D = new Grid (new Mesh (), meshName2D);
                grid2D.Attach (new MappingAttachment ());

                /// Empty 1D grid which stores geometry + diameter data
                Grid grid1D = new Grid (new Mesh (), meshName1D);
                grid1D.Attach (new DiameterAttachment ());

                /// Read the meshes with vrnReader directly from .vrn archive
                try {
                    reader.ReadUGX (meshName2D, ref grid2D);
                    reader.ReadUGX (meshName1D, ref grid1D);
                } catch (CouldNotReadMeshFromVRNArchive ex) {
                    UnityEngine.Debug.LogError (ex);
                }

                /// Build the 1D/2D mapping
                try {
                    mapping = (MappingInfo) MapUtils.BuildMap (grid1D, grid2D);
                    UnityEngine.Debug.Log("Mapping build succesfully.");
                } catch (MapNotBuildException ex) {
                    UnityEngine.Debug.LogError (ex);
                }

                // Convert dictionary to array for speed
                map = new Vert3D1DPair[mapping.Data.Count];
                foreach (KeyValuePair<int, Tuple<int, int, double>> entry in mapping.Data) {
                    map[entry.Key] = new Vert3D1DPair (entry.Value.Item1, entry.Value.Item2, entry.Value.Item3);
                }
                scalars3D = new double[map.Length];

                if (visualize1D) Render1DCell ();

                VisualMesh = grid2D.Mesh;
                VisualMesh.Rescale (transform, new Vector3 (4, 4, 4));
                VisualMesh.RecalculateNormals ();
                cellMesh = VisualMesh;

                // Pass blownupMesh upwards to SurfaceSimulation
                colMesh = grid2D.Mesh;

                InitUI ();
            }

            return cellMesh;

            void Render1DCell () {
                Grid geom1D = mapping.ModelGeometry;
                GameObject lines1D = gameObject.AddComponent<LinesRenderer> ().Constr (geom1D, color1D, lineWidth1D);
            }
            void InitUI () {
                // Instantiate neuron diameter control panel, announce active simulation to each button
                GameObject diameterControlPanel = Resources.Load ("Prefabs/NeuronDiameterControls") as GameObject;
                SwitchNeuronMesh[] buttons = diameterControlPanel.GetComponentsInChildren<SwitchNeuronMesh> ();
                foreach (SwitchNeuronMesh button in buttons) {
                    button.neuronSimulation1D = this;
                }

                GameObject.Instantiate (diameterControlPanel, GameManager.instance.whiteboard);

                // Instantiate a ruler to allow the cell to be scaled interactively
                GameObject ruler = Resources.Load ("Prefabs/Ruler") as GameObject;
                ruler.GetComponent<GrabbableRuler> ().scaleTarget = transform;
                GameObject.Instantiate (ruler);

                gameObject.AddComponent<ScaleLimiter> ();
            }

        }

        public void RescaleMesh (Vector3 newSize) {
            MeshFilter mf = GetComponent<MeshFilter> ();
            if (mf != null && mf.sharedMesh != null) {
                mf.sharedMesh.Rescale (transform, newSize);
            }
        }


        private Mesh BuildMesh (double inflation = 1) {
            Mesh mesh = null;

            // 1 <= inflation
            inflation = Math.Max (inflation, 1);
            // 0 <= refinement

            if (reader == null) reader = new vrnReader (vrnPath);
            mesh = MapUtils.BuildMap (reader.Retrieve2DMeshName (inflation),
                reader.Retrieve1DMeshName (0),
                false).SurfaceGeometry.Mesh;

            mesh.RecalculateNormals ();

          //  mesh.name = mesh.name + "ref" + refinement.ToString () + "inf" + inflation.ToString ();

            return mesh;
        }
        private Mesh CheckMeshCache(double inflation)
        {
            if (!meshCache.ContainsKey(inflation) || meshCache[inflation] == null)
            {
                if (reader == null) reader = new vrnReader(vrnPath);
                string meshName = reader.Retrieve2DMeshName(inflation);
                Debug.Log("meshName: " + meshName + "\ninflation: " + inflation);
                Grid grid = new Grid(new Mesh(), meshName);
                reader.ReadUGX(meshName, ref grid);
                meshCache[inflation] = grid.Mesh;
            }
            return meshCache[inflation];
        }

        public void SwitchColliderMesh (double inflation) {
            inflation = Math.Clamp (inflation, 1, 5);
            ColliderInflation = inflation;
        }

        public void SwitchMesh (double inflation) {
            inflation = Math.Clamp (inflation, 1, 5);
            VisualInflation = inflation;
        }

        /// <summary>
        /// Switch the visualization or collider mesh
        /// </summary>
        /// <param name="mesh"></param>
        private void SwitchColliderMesh (Mesh mesh) {
            var meshColController = GetComponent<MeshColController>();
            if (meshColController == null) meshColController = gameObject.AddComponent<MeshColController>();
            meshColController.Mesh = mesh;
        }
    }
}
