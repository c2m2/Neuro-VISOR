using System;
using System.Collections.Generic;
using System.IO;
using C2M2.NeuronalDynamics.UGX;
using UnityEngine;
using DiameterAttachment = C2M2.NeuronalDynamics.UGX.IAttachment<C2M2.NeuronalDynamics.UGX.DiameterData>;
using MappingAttachment = C2M2.NeuronalDynamics.UGX.IAttachment<C2M2.NeuronalDynamics.UGX.MappingData>;
using Math = C2M2.Utils.Math;
using C2M2.Simulation;
using C2M2.Utils.DebugUtils;
using C2M2.Utils.MeshUtils;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
using C2M2.NeuronalDynamics.Visualization.VRN;
using C2M2.NeuronalDynamics.Interaction;
using C2M2.NeuronalDynamics.Interaction.UI;
using C2M2.Interaction.UI;
using System.Linq;
using C2M2.Utils;

namespace C2M2.NeuronalDynamics.Simulation {

    /// <summary>
    /// Provide an interface for 1D neuron-surface simulations to be visualized and interacted with
    /// </summary>
    /// <remarks>
    /// 1D Neuron surface simulations should derive from this class.
    /// </remarks>
    public abstract class NDSimulation : MeshSimulation {

        private double visualInflation = 1;
        public double VisualInflation
        {
            get { return visualInflation; }
            set
            {
                if (visualInflation != value)
                {
                    visualInflation = value;

                    Update2DGrid();

                    VisualMesh = Grid2D.Mesh;
                    OnVisualInflationChange?.Invoke(visualInflation);
                }
            }
        }

        public delegate void OnVisualInflationChangeDelegate(double newInflation);
        public event OnVisualInflationChangeDelegate OnVisualInflationChange;

        private int refinementLevel = 0;
        public int RefinementLevel
        {
            get { return refinementLevel; }
            set
            {
                if (refinementLevel != value && value >= 0)
                {
                    refinementLevel = value;
                    UpdateGrid1D();
                }
            }
        }

        private Dictionary<double, Mesh> meshCache = new Dictionary<double, Mesh>();

        public NeuronClampManager clampManager = null;
        public List<NeuronClamp> clamps = new List<NeuronClamp>();
        internal readonly object clampLock = new object();
        private static readonly Tuple<int, double> nullClamp = new Tuple<int, double>(-1, -1);

        public NDGraphManager graphManager { get; private set; } = null;
        public List<NDLineGraph> Graphs
        {
            get { return graphManager.graphs; }
            set { graphManager.graphs = value; }
        }
    

        [Header ("1D Visualization")]
        public bool visualize1D = false;
        public Color32 color1D = Color.yellow;
        public float lineWidth1D = 0.005f;

        private InfoPanel infoPanel = null;

        public GameObject infoPanelPrefab = null;

        public GameObject controlPanel = null;

        // Need mesh options for each refinement, diameter level
        [Tooltip("Name of the vrn file within Assets/StreamingAssets/NeuronalDynamics/Geometries")]
        public string vrnFileName = "test.vrn";
        private VrnReader vrnReader = null;
        /// <summary>
        /// Used to read cell data from .vrn archives
        /// </summary>
        public VrnReader VrnReader
        {
            get
            {
                if (vrnReader == null)
                {
                    char sl = Path.DirectorySeparatorChar;
                    if (!vrnFileName.EndsWith(".vrn")) vrnFileName = vrnFileName + ".vrn";
                    vrnReader = new VrnReader(Application.streamingAssetsPath + sl + "NeuronalDynamics" + sl + "Geometries" + sl + vrnFileName);
                }
                return vrnReader;
            }
            private set { vrnReader = value; }
        }
        /// <summary>
        /// Includes info like cell species, strain, and archive
        /// </summary>
        public VrnReader.MetaInfo MetaInfo { get { return (VrnReader.MetaInfo)vrnReader.GetMetaInfo(); } }

        private Grid grid1D = null;
        public Grid Grid1D
        {
            get {
                return grid1D;
            }
            set
            {
                grid1D = value;
            }
        }

        public Vector3[] Verts1D { get { return grid1D.Mesh.vertices; } }
        public double[] vals1D = null;

        private Grid grid2D = null;
        public Grid Grid2D
        {
            get
            {
                return grid2D;
            }
            set
            {
                grid2D = value;
            }
        }
        public Neuron Neuron { get; set; } = null;

        private float averageDendriteRadius = 0;
        public float AverageDendriteRadius
        {
            get
            {
                if (averageDendriteRadius == 0)
                {
                    averageDendriteRadius = (float)Neuron.nodes.Select(node => node.NodeRadius).Average();
                }
                return averageDendriteRadius;
            }
        }

        // Stores the information from mapping in an array of structs.
        // Performs much better than using mapping directly.
        private Vert3D1DPair[] map = null;
        public Vert3D1DPair[] Map
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

        void ShowInfoPanel(bool show, RaycastHit hit)
        {
            if (infoPanel == null)
            {
                infoPanel = Instantiate(infoPanelPrefab, transform).GetComponent<InfoPanel>();
            }
            infoPanel.gameObject.SetActive(show);
            if(show)
            {
                int id = GetNearestPoint(hit);
                infoPanel.unit = unit;
                infoPanel.Vertex = id;
                infoPanel.Power = vals1D[id] * unitScaler;
                infoPanel.FocusLocalPosition = Verts1D[id]; //offset so the popup is not in the middle of the dendrite
            }        
        }

        protected override void PostSolveStep(int t)
        {
            ApplyInteractionVals();
            SetOutputValues();
            void ApplyInteractionVals()
            {
                ///<c>if (clamps != null && clamps.Count > 0)</c> this if statement is where we apply voltage clamps   
                /// Apply clamp values, if there are any clamps

                if (clamps.Count > 0)
                {
                    Tuple<int, double>[] clampValues = new Tuple<int, double>[clamps.Count];
                    lock (clampLock)
                    {
                        for (int i = 0; i < clamps.Count; i++)
                        {
                            if (clamps[i] != null && clamps[i].focusVert != -1 && clamps[i].ClampLive)
                            {
                                clampValues[i] = new Tuple<int, double>(clamps[i].focusVert, clamps[i].ClampPower);
                            }
                            else
                            {
                                clampValues[i] = nullClamp;
                            }
                        }
                    }
                    
                    Set1DValues(clampValues);
                }

                // Apply raycast values
                if (raycastHits.Length > 0)
                {
                    Set1DValues(raycastHits);
                }
            }
        }

        internal abstract void SetOutputValues();

        protected override void OnAwakePost(Mesh viz)
        {
            base.OnAwakePost(viz);
            infoPanelPrefab = (GameObject)Resources.Load("Prefabs" + Path.DirectorySeparatorChar + "NeuronalDynamics" + Path.DirectorySeparatorChar + "HoverInfo");

            defaultRaycastEvent.OnHover.AddListener((hit) =>
            {
                ShowInfoPanel(true, hit);
            });
            defaultRaycastEvent.OnHoverEnd.AddListener((hit) =>
            {
                ShowInfoPanel(false, hit);
            });
            defaultRaycastEvent.OnHoldPress.AddListener((hit) =>
            {
                ShowInfoPanel(true, hit);
            });
            defaultRaycastEvent.OnEndPress.AddListener((hit) =>
            {
                ShowInfoPanel(false, hit);
            });

            switch (((NDSimulationManager)Manager).FeatState)
            {
                case (NDSimulationManager.FeatureState.Direct):
                    raycastEventManager.LRTrigger = defaultRaycastEvent;
                    break;
                case (NDSimulationManager.FeatureState.Clamp):
                    raycastEventManager.LRTrigger = clampManager.hitEvent;
                    break;
                case (NDSimulationManager.FeatureState.Plot):
                    raycastEventManager.LRTrigger = graphManager.hitEvent;
                    break;
                case (NDSimulationManager.FeatureState.Synapse):

                    break;
            }
            
        }
        /// <summary>
        /// Translate 1D vertex values to 3D values and pass them upwards for visualization
        /// </summary>
        /// <returns> One scalar value for each 3D vertex based on its 1D vert's scalar value </returns>
        public sealed override float[] GetValues () {
            vals1D = Get1DValues();

            if (vals1D == null) { return null; }
            
            for (int i = 0; i < Map.Length; i++) { // for each 3D point,

                // Take an weighted average using lambda
                // Equivalent to [lambda * v2 + (1 - lambda) * v1]
                double newVal = map[i].lambda * (vals1D[map[i].v2] - vals1D[map[i].v1]) + vals1D[map[i].v1];

                Scalars3D[i] = newVal;
            }

            // Update graphs
            foreach(NDLineGraph graph in Graphs)
            {
                graph.AddValue(1000*GetSimulationTime(), (float)vals1D[graph.vert] * unitScaler);
            }

            return Scalars3D.ToFloat();
        }

        /// <summary>
        /// Translate 3D vertex values to 1D values, and pass them downwards for interaction
        /// </summary>
        public sealed override void SetValues (RaycastHit hit) {
            int[] verts = HitToVertices(hit);
            Tuple<int, double>[] newValues = new Tuple<int, double>[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                newValues[i] = new Tuple<int, double>(verts[i], raycastHitValue);
            }
            SetValues(newValues);
        }
        /// <summary>
        /// Translate 3D vertex values to 1D values, and store the values to be applied to simulation values
        /// </summary>
        public void SetValues (Tuple<int, double>[] newValues) {
            // Reserve space for new1DValuess
            Tuple<int, double>[] new1Dvalues = new Tuple<int, double>[newValues.Length];
            // Receive values given to 3D vertices, translate them onto 1D vertices and apply values there
            for (int i = 0; i < newValues.Length; i++)
            {    
                int vert3D = newValues[i].Item1;
                double val3D = newValues[i].Item2;

                // If lambda > 0.5, the vert is closer to v2 so apply val3D there
                int vert1D = (map[vert3D].lambda > 0.5) ? map[vert3D].v2 : map[vert3D].v1;
                new1Dvalues[i] = new Tuple<int, double>(vert1D, val3D);
            }
            raycastHits = new1Dvalues;
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
        protected override void OnAwakePre()
        {
            UpdateGrid1D();
            var clampManagerObj = GameObject.Instantiate(GameManager.instance.clampManagerPrefab);
            clampManager = clampManagerObj.GetComponent<NeuronClampManager>();
            base.OnAwakePre();
        }

        /// <summary>
        /// Read in the cell and initialize 3D/1D visualization/interaction infrastructure
        /// </summary>
        /// <returns> Unity Mesh visualization of the 3D geometry. </returns>
        /// <remarks> BuildVisualization is called by Simulation.cs,
        /// it is called after OnAwakePre and before OnAwakePost.
        /// If dryRun == true, Simulation will not call BuildVisualization. </remarks>
        protected override Mesh BuildVisualization () {
            if (!dryRun) {

                if (visualize1D) Render1DCell ();

                Update2DGrid();

                VisualMesh = Grid2D.Mesh;
 
                VisualMesh.Rescale(transform, new Vector3 (4, 4, 4));
                VisualMesh.RecalculateNormals ();

                // Pass blownupMesh upwards to MeshSimulation
                ColliderMesh = VisualMesh;

                InitUI();
            }

            return VisualMesh;

            void Render1DCell () {
                Grid geom1D = Mapping.ModelGeometry;
                GameObject lines1D = gameObject.AddComponent<LinesRenderer> ().Draw (geom1D, color1D, lineWidth1D);
            }

            void InitUI()
            {
                GameObject gm = new GameObject
                {
                    name = "LineGraphManager"
                };
                gm.transform.parent = transform;
                graphManager = gm.AddComponent<NDGraphManager>();

                controlPanel = GameObject.FindGameObjectWithTag("ControlPanel");
                if(controlPanel == null)
                {
                    controlPanel = Resources.Load("Prefabs/NeuronalDynamics/ControlPanel/NDControls") as GameObject;
                    controlPanel = Instantiate(controlPanel);
                }


                NDBoardController controller = controlPanel.GetComponent<NDBoardController>();
                if (controller == null)
                {
                    Debug.LogWarning("No NDSimulationController found.");
                    Destroy(controlPanel);
                    return;
                }
                controller.MinimizeBoard(false);
            }
        }

        public void SwitchVisualMesh (double inflation) {
            inflation = Math.Clamp (inflation, 1, 5);
            VisualInflation = inflation;
        }

        private void UpdateGrid1D()
        {
            string meshName1D = VrnReader.Retrieve1DMeshName(RefinementLevel);
            /// Create empty grid with name of grid in archive
            Grid1D = new Grid(new Mesh(), meshName1D);
            Grid1D.Attach(new DiameterAttachment());

            VrnReader.ReadUGX(meshName1D, ref grid1D);

            Neuron = new Neuron(grid1D);
        }
        private void Update2DGrid()
        {
            /// Retrieve mesh names from archive
            string meshName2D = VrnReader.Retrieve2DMeshName(VisualInflation, RefinementLevel);

            /// Empty 2D grid which stores geometry + mapping data
            Grid2D = new Grid(new Mesh(), meshName2D);
            Grid2D.Attach(new MappingAttachment());
            VrnReader.ReadUGX(meshName2D, ref grid2D);
        }

        public int GetNearestPoint(RaycastHit hit)
        {
            if (mf == null) return -1;

            // Get 3D mesh vertices from hit triangle
            int triInd = hit.triangleIndex * 3;
            int v1 = mf.mesh.triangles[triInd];
            int v2 = mf.mesh.triangles[triInd + 1];
            int v3 = mf.mesh.triangles[triInd + 2];

            // Find 1D verts belonging to these 3D verts
            int[] verts1D = new int[]
            {
                Map[v1].v1, Map[v1].v2,
                Map[v2].v1, Map[v2].v2,
                Map[v3].v1, Map[v3].v2
            };
            Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);

            float nearestDist = float.PositiveInfinity;
            int nearestVert1D = -1;
            foreach (int vert in verts1D)
            {
                float dist = Vector3.Distance(localHitPoint, Verts1D[vert]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestVert1D = vert;
                }
            }

            return nearestVert1D;
        }
    }

    /// <summary>
    /// Stores two 1D indices and a lambda value for a 3D vertex
    /// </summary>
    /// <remarks>
    /// Lambda is a value between 0 and 1. A lambda value greater than 0.5 implies that the 3D vert lies closer to v2.
    /// A lambda value of 0 would imply that the 3D vert lies directly over v1,
    /// and a lambda of 1 implies that it lies completely over v2.
    /// </remarks>
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
