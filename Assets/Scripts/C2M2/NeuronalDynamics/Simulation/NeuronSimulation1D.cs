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
using C2M2.Utils;
using C2M2.Utils.DebugUtils;
using C2M2.Utils.Exceptions;
using C2M2.Utils.MeshUtils;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
using System.Text;
using C2M2.NeuronalDynamics.Visualization.vrn;
using C2M2.NeuronalDynamics.Interaction;

namespace C2M2.NeuronalDynamics.Simulation {

    /// <summary>
    /// Provide an interface for 1D neuron-surface simulations to be visualized and interacted with
    /// </summary>
    /// <remarks>
    /// 1D Neuron surface simulations should derive from this class.
    /// </remarks>
    public abstract class NeuronSimulation1D : MeshSimulation {

        private double visualInflation = 1;
        public double VisualInflation
        {
            get { return visualInflation; }
            set
            {
                visualInflation = value;
                if (ColliderInflation < visualInflation) ColliderInflation = visualInflation;
                VisualMesh = CheckMeshCache(visualInflation);
                OnVisualInflationChange?.Invoke(visualInflation);
            }
        }

        public delegate void OnVisualInflationChangeDelegate(double newInflation);
        public event OnVisualInflationChangeDelegate OnVisualInflationChange;

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

        public int refinementLevel = 0;

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

        private bool clampMode = false;
        public bool ClampMode
        {
            get { return clampMode; }
            set
            {
                clampMode = value;
                RaycastPressEvents newEvents = clampMode ?
                    GameManager.instance.gameObject.GetComponent<RaycastPressEvents>()
                    : GetComponentInChildren<RaycastPressEvents>();
                if (newEvents == null) return;
                raycastManager.leftTrigger = newEvents;
                raycastManager.rightTrigger = newEvents;

                foreach (GameObject clamp in GameManager.instance.clampControllers)
                {
                    MeshRenderChild renderControls = clamp.GetComponentInParent<MeshRenderChild>();
                    if (renderControls != null) renderControls.enabled = clampMode;
                    clamp.SetActive(clampMode);
                }
            }
        }

        [Header ("1D Visualization")]
        public bool visualize1D = false;
        public Color32 color1D = Color.yellow;
        public float lineWidth1D = 0.005f;

        // Need mesh options for each refinement, diameter level
        public string vrnPath = "test.vrn";
        private vrnReader vrnReader = null;
        private vrnReader VrnReader
        {
            get
            {
                if(vrnReader == null)
                    vrnReader = new vrnReader(Application.streamingAssetsPath + Path.DirectorySeparatorChar + vrnPath);
                return vrnReader;
            }
        }

        private Grid grid1D = null;
        public Grid Grid1D
        {
            get {
                if (grid1D == null)
                {
                    UpdateGrid1D();
                }
                return grid1D;
            }
        }
        public Vector3[] Verts1D { get { return grid1D.Mesh.vertices; } }

        private Grid grid2D = null;
        public Grid Grid2D
        {
            get
            {
                if (grid2D == null)
                {
                    UpdateGrid2D();
                }
                return grid2D;
            }
        }

        private NeuronCell neuronCell = null;
        public NeuronCell NeuronCell
        {
            get
            {
                if(neuronCell == null)
                {
                    neuronCell = new NeuronCell(Grid1D);
                }
                
                return neuronCell;
            }
        }

        // Storing the information from mapping in an array of structs greatly improves time performance
        private Vert3D1DPair[] map = null;
        private Vert3D1DPair[] Map
        {
            get
            {
                if(map == null)
                {
                    map = new Vert3D1DPair[Mapping.Data.Count];
                    for(int i = 0; i < Mapping.Data.Count; i++)
                    {
                        map[i] = new Vert3D1DPair(Mapping.Data[i].Item1, Mapping.Data[i].Item2, Mapping.Data[i].Item3);
                    }
                }
                return map;
            }
        }
        private MappingInfo mapping = default;
        private MappingInfo Mapping
        {
            get
            {
                if(mapping.Equals(default(MappingInfo)))
                {
                    mapping = (MappingInfo)MapUtils.BuildMap(Grid1D, Grid2D);
                }
                return mapping;
            }
            set
            {
                mapping = (MappingInfo)MapUtils.BuildMap(Grid1D, Grid2D);
            }
        }

        private RaycastEventManager raycastManager = null;

        private double[] scalars3D = new double[0];
        private double[] Scalars3D
        {
            get
            {
                if(scalars3D.Length == 0)
                {
                    scalars3D = new double[Mapping.Data.Count];
                }
                return scalars3D;
            }
        }

        public List<NeuronClamp> clampValues = new List<NeuronClamp>();

        /// <summary>
        /// Translate 1D vertex values to 3D values and pass them upwards for visualization
        /// </summary>
        /// <returns> One scalar value for each 3D vertex based on its 1D vert's scalar value </returns>
        public sealed override double[] GetValues () {
            double[] scalars1D = Get1DValues ();

            if (scalars1D == null) { return null; }
            //double[] scalars3D = new double[map.Length];
            for (int i = 0; i < Map.Length; i++) { // for each 3D point,

                // Take an weighted average using lambda
                // Equivalent to [lambda * v2 + (1 - lambda) * v1]
                double newVal = map[i].lambda * (scalars1D[map[i].v2] - scalars1D[map[i].v1]) + scalars1D[map[i].v1];

                Scalars3D[i] = newVal;
            }
            // Debug.Log(sb.ToString());
            return Scalars3D;
        }
        /// <summary>
        /// Translate 3D vertex values to 1D values, and pass them downwards for interaction
        /// </summary>
        public sealed override void SetValues (RaycastHit hit) {
            Tuple<int, double>[] newValues = ((RaycastSimHeaterDiscrete)Heater).HitToTriangles (hit);

            SetValues (newValues);
        }

        /// <summary>
        /// Translate 3D vertex values to 1D values, and pass them downwards for interaction
        /// </summary>
        /// <returns>
        /// Either the same set of values given, or values translated
        /// </returns>
        public void SetValues (Tuple<int, double>[] newValues) {        
            // Each 3D index will have TWO associated 1D vertices
            Tuple<int, double>[] new1DValues = new Tuple<int, double>[2 * newValues.Length];
            int j = 0;
            for (int i = 0; i < newValues.Length; i++) {
                // Get 3D vertex index
                int vert3D = newValues[i].Item1;
                double val3D = newValues[i].Item2;

                // TODO: What if a 1D vert belongs to multiple 3D verts in this list?
                // Translate into two 1D vert indices and a lambda weight
                double val1D = (1 - Map[vert3D].lambda) * val3D;
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
        /// <param name="newValues"> List of 1D vert indices and values to add onto that index. </param>
        public abstract void Set1DValues (Tuple<int, double>[] newValues);

        /// <summary>
        /// Requires derived classes to know how to make available one value for each 1D vertex
        /// </summary>
        /// <returns></returns>
        public abstract double[] Get1DValues ();

        protected override void OnStart()
        {
            base.OnStart();
            raycastManager = GetComponent<RaycastEventManager>();

            ClampMode = clampMode;
        }
        /// <summary>
        /// Read in the cell and initialize 3D/1D visualization/interaction infrastructure
        /// </summary>
        /// <returns> Unity Mesh visualization of the 3D geometry. </returns>
        protected override Mesh BuildVisualization () {
            if (!dryRun) {

                if (visualize1D) Render1DCell ();

                VisualMesh = Grid2D.Mesh;
                VisualMesh.Rescale (transform, new Vector3 (4, 4, 4));
                VisualMesh.RecalculateNormals ();

                // Pass blownupMesh upwards to SurfaceSimulation
                colMesh = VisualMesh;

                InitUI ();
            }

            return VisualMesh;

            void Render1DCell () {
                Grid geom1D = Mapping.ModelGeometry;
                GameObject lines1D = gameObject.AddComponent<LinesRenderer> ().Constr (geom1D, color1D, lineWidth1D);
            }
            void InitUI () {
                // Instantiate neuron diameter control panel, announce active simulation to each button
                GameObject diameterControlPanel = Resources.Load ("Prefabs/NDControls") as GameObject;
                NDControlButton[] buttons = diameterControlPanel.GetComponentsInChildren<NDControlButton> ();
                foreach (NDControlButton button in buttons) {
                    button.ndSimulation = this;
                }

                GameObject.Instantiate (diameterControlPanel, GameManager.instance.whiteboard);

                // Instantiate a ruler to allow the cell to be scaled interactively
                GameObject ruler = Resources.Load ("Prefabs/Ruler") as GameObject;
                ruler.GetComponent<GrabbableRuler> ().scaleTarget = transform;
                GameObject.Instantiate (ruler);

                gameObject.AddComponent<ScaleLimiter> ();
            }
        }

        private Mesh CheckMeshCache(double inflation)
        {
            if (!meshCache.ContainsKey(inflation) || meshCache[inflation] == null)
            {
                string meshName = VrnReader.Retrieve2DMeshName(inflation);

                Grid grid = new Grid(new Mesh(), meshName);
                VrnReader.ReadUGX(meshName, ref grid);
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

        private void UpdateGrid1D()
        {
            string meshName1D = VrnReader.Retrieve1DMeshName(refinementLevel);
            /// Create empty grid with name of grid in archive
            grid1D = new Grid(new Mesh(), meshName1D);
            grid1D.Attach(new DiameterAttachment());

            VrnReader.ReadUGX(meshName1D, ref grid1D);
        }
        private void UpdateGrid2D()
        {
            /// Retrieve mesh names from archive
            string meshName2D = VrnReader.Retrieve2DMeshName(visualInflation);

            /// Empty 2D grid which stores geometry + mapping data
            grid2D = new Grid(new Mesh(), meshName2D);
            grid2D.Attach(new MappingAttachment());
            VrnReader.ReadUGX(meshName2D, ref grid2D);
        }
    }

    /// <summary>
    /// Stores two 1D indices and a lambda value for a 3D vertex
    /// </summary>
    public struct Vert3D1DPair
    {
        public int v1 { get; private set; }
        public int v2 { get; private set; }
        public double lambda { get; private set; }

        public Vert3D1DPair(int v1, int v2, double lambda)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.lambda = lambda;
        }
        public override string ToString()
        {
            return "v1: " + v1 + "\nv2: " + v2 + "\nlambda: " + lambda;
        }
    }
}
