using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using C2M2.NeuronalDynamics.UGX;
using DiameterAttachment = C2M2.NeuronalDynamics.UGX.IAttachment<C2M2.NeuronalDynamics.UGX.DiameterData>;
using MappingAttachment = C2M2.NeuronalDynamics.UGX.IAttachment<C2M2.NeuronalDynamics.UGX.MappingData>;

using Math = C2M2.Utils.Math;
using C2M2.Utils.DebugUtils;
using C2M2.Utils.MeshUtils;
using C2M2.Interaction;
using C2M2.Simulation;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
using C2M2.NeuronalDynamics.Visualization.vrn;

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
                    else if (file.EndsWith("_tris.ugx")) pathTris = file;    // Triangles
                    else if (file.EndsWith(".ugx")) path3D = file;     // If it isn't specified as 1D or triangles, it's most likely 3D
                }
            }
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
    }

    public struct CellInfo
    {
        public string name { get; private set; }
        public string filePath { get; private set; }
        public MappingInfo data { get; private set; }
       
        public Mesh mesh 
        {
            get
            {
                return data.SurfaceGeometry.Mesh;
            }
        }
        public CellInfo(string filePath, string name = "")
        {
            this.name = name;
            this.filePath = filePath;
            CellPathPacket paths = new CellPathPacket(filePath);

            if (paths.path3D != "NULL" && paths.path1D != "NULL" && paths.pathTris != "NULL")
            {
                data = MapUtils.BuildMap(paths.path3D, paths.path1D, false, paths.pathTris);
            }
            else
            {
                string s = "";
                if (paths.path3D == "NULL") s += " [3D Cell] ";
                if (paths.path1D == "NULL") s += " [1D Cell] ";
                if (paths.pathTris == "NULL") s += " [Cell Triangles] ";
                throw new NullReferenceException("Null paths found for " + s);
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
        public enum MeshScaling { x1 = 0, x2 = 1, x3 = 2, x4 = 3, x5 = 4}
        [Header("3D Visualization")]
        [Tooltip("Which mesh scale to use for the mesh collider used for raycasting. Larger meshes will be easier to interact with, but less accurate")]
        private MeshScaling meshScale = MeshScaling.x1;
        private MeshScaling meshColScale = MeshScaling.x1;
        public enum RefinementLevel { x0, x1, x2, x3, x4 }
        public RefinementLevel refinementLevel = RefinementLevel.x1;

        // Need mesh options for each refinement, diameter level
        public string vrnCellPath = "Assets" + Path.DirectorySeparatorChar + "test.vrn";
        public string cell1xPath;
        public string cell2xPath;
        public string cell3xPath;
        public string cell4xPath;
        public string cell5xPath;

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

        public Vector3[] Verts1D { get { return mapping.ModelGeometry.Mesh.vertices; } }

        protected Grid grid1D;

        ///<summary> Lookup a 3D vert and get back two 1D indices and a lambda value for them </summary>
        //private Dictionary<int, Tuple<int, int, double>> map;
        private Vert3D1DPair[] map;
        private MappingInfo mapping;

        private MeshColController meshColController = null;
        private Mesh[] scaledMeshes = new Mesh[5];

        private double[] scalars3D = new double[0];


        /// <summary>
        /// Translate 1D vertex values to 3D values and pass them upwards for visualization
        /// </summary>
        /// <returns> One scalar value for each 3D vertex based on its 1D vert's scalar value </returns>
        public sealed override double[] GetValues()
        {
            double[] scalars1D = Get1DValues();

            if (scalars1D == null) { return null; }

            //double[] scalars3D = new double[map.Length];
            for (int i = 0; i < map.Length; i++)
            { // for each 3D point,

                // Take an weighted average using lambda
                // Equivalent to [lambda * val1Db + (1 - lambda) * val1Da]        
                double newVal = map[i].lambda * (scalars1D[map[i].v2] - scalars1D[map[i].v1]) + scalars1D[map[i].v1];

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

            SetValues(newValues);
        }

        /// <summary>
        /// Translate 3D vertex values to 1D values, and pass them downwards for interaction
        /// </summary>
        public void SetValues(Tuple<int, double>[] newValues)
        {
            // Each 3D index will have TWO associated 1D vertices
            Tuple<int, double>[] new1DValues = new Tuple<int, double>[2 * newValues.Length];
            int j = 0;
            for (int i = 0; i < newValues.Length; i++)
            {
                // Get 3D vertex index
                int vert3D = newValues[i].Item1;
                double val3D = newValues[i].Item2;

                // Translate into two 1D vert indices and a lambda weight
                double val1D = (1 - map[vert3D].lambda) * val3D;
                new1DValues[j] = new Tuple<int, double>(map[vert3D].v1, val1D);

                // Weight newVal by (lambda) for second 1D vert                    
                val1D = map[vert3D].lambda * val3D;
                new1DValues[j + 1] = new Tuple<int, double>(map[vert3D].v2, val1D);
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
        public abstract void Set1DValues(Tuple<int, double>[] newValuess);

        /// <summary>
        /// Requires derived classes to know how to make available one value for each 1D vertex
        /// </summary>
        /// <returns></returns>
        public abstract double[] Get1DValues();

        /// <summary>
        /// Pass the UGX 1D and 3D cells to simulation code
        /// </summary>
        /// <param name="grid"></param>
        protected abstract void SetNeuronCell(Grid grid);

        /*protected override void ReadData()
        {
            vrnReader reader = new vrnReader(CellPath);
            CellPathPacket pathPacket = new CellPathPacket(cell1xPath, "1xDiameter");

            // Read in 1D & 3D data and build a map between them
            mapping = MapUtils.BuildMap(pathPacket.path1D,
                pathPacket.path3D,
                false,
                pathPacket.pathTris);

            //map = mapping.Data;
            // Convert dictionary to array for speed
            map = new Vert3D1DPair[mapping.Data.Count];
            foreach(KeyValuePair<int, Tuple<int, int, double>> entry in mapping.Data)
            {
                map[entry.Key] = new Vert3D1DPair(entry.Value.Item1, entry.Value.Item2, entry.Value.Item3);
            }

            scalars3D = new double[map.Length];

            // Pass the cell to simulation code
            SetNeuronCell(mapping.ModelGeometry);
        }*/

        vrnReader reader = null;
        protected override void ReadData()
        {
            /// FIXME: Specifiying vrnCellPath alone did not work for me (Assets/test.vrn), so I had to manually override it here
            if(reader == null) reader = new vrnReader(Application.dataPath + Path.DirectorySeparatorChar + "test.vrn");
            Debug.Log("Path: " + Application.dataPath + Path.DirectorySeparatorChar + "test.vrn");
            Debug.Log(reader.List());

            string meshName1D = reader.Retrieve1DMeshName();
            /// Create empty grid with name of grid in archive
            Grid grid1D = new Grid(new Mesh(), meshName1D);
            grid1D.Attach(new DiameterAttachment());
            reader.ReadUGX(meshName1D, ref grid1D);
            string meshName2D = reader.Retrieve2DMeshName();
            /// Create empty grid with name of grid in archive
            Grid grid2D = new Grid(new Mesh(), meshName2D);
            grid2D.Attach(new DiameterAttachment());
            grid2D.Attach(new MappingAttachment());
            reader.ReadUGX(meshName2D, ref grid2D);

            //GetComponent<MeshFilter>().sharedMesh = grid.Mesh;
            // Read in 1D & 3D data and build a map between them
            mapping = (MappingInfo)MapUtils.BuildMap(grid1D, grid2D);

            //map = mapping.Data;
            // Convert dictionary to array for speed
            map = new Vert3D1DPair[mapping.Data.Count];
            foreach (KeyValuePair<int, Tuple<int, int, double>> entry in mapping.Data)
            {
                map[entry.Key] = new Vert3D1DPair(entry.Value.Item1, entry.Value.Item2, entry.Value.Item3);
            }

            scalars3D = new double[map.Length];

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
                cellMesh = mapping.SurfaceGeometry.Mesh;
                cellMesh.Rescale(transform, new Vector3(4, 4, 4));
                cellMesh.RecalculateNormals();


                //scaledMeshes[(int)MeshScaling.x1] = cellMesh;

                if (visualize1D) Render1DCell();

                meshColController = gameObject.AddComponent<MeshColController>();

                // Pass blownupMesh upwards to SurfaceSimulation
                colliderMesh = BuildMesh((int)meshColScale);
                //colliderMesh = BuildMesh();

                InitUI();
            }

            return cellMesh;

            void Render1DCell()
            {
                Grid geom1D = mapping.ModelGeometry;
                GameObject lines1D = gameObject.AddComponent<LinesRenderer>().Constr(geom1D, color1D, lineWidth1D);
            }
            void InitUI()
            {
                // Instantiate neuron diameter control panel, announce active simulation to each button
                GameObject diameterControlPanel = Resources.Load("Prefabs/NeuronDiameterControls") as GameObject;
                SwitchNeuronMesh[] buttons = diameterControlPanel.GetComponentsInChildren<SwitchNeuronMesh>();
                foreach (SwitchNeuronMesh button in buttons)
                {
                    button.neuronSimulation1D = this;
                }

                GameObject.Instantiate(diameterControlPanel, GameManager.instance.whiteboard);

                // Instantiate a ruler to allow the cell to be scaled interactively
                GameObject ruler = Resources.Load("Prefabs/Ruler") as GameObject;
                ruler.GetComponent<GrabbableRuler>().scaleTarget = transform;
                GameObject.Instantiate(ruler);

                gameObject.AddComponent<ScaleLimiter>();
            }

        }

        public void RescaleMesh(Vector3 newSize)
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                mf.sharedMesh.Rescale(transform, newSize);
            }
        }


        // Returns whichever mesh is used for the mesh collider
        /*
        private Mesh BuildMesh(MeshScaling meshScale)
        {
            if (scaledMeshes[(int)meshScale] == null)
            {
                Mesh mesh = null;

                // Build blownup mesh name
                CellPathPacket cellPathPacket = new CellPathPacket();
                string scale = "";
                switch (meshScale)
                {
                    case (MeshScaling.x1):
                        cellPathPacket = new CellPathPacket(cell1xPath);
                        scale = "x1";
                        break;
                    case (MeshScaling.x2):
                        cellPathPacket = new CellPathPacket(cell2xPath);
                        scale = "x2";
                        break;
                    case (MeshScaling.x3):
                        cellPathPacket = new CellPathPacket(cell3xPath);
                        scale = "x3";
                        break;
                    case (MeshScaling.x4):
                        cellPathPacket = new CellPathPacket(cell4xPath);
                        scale = "x4";
                        break;
                    case (MeshScaling.x5):
                        cellPathPacket = new CellPathPacket(cell5xPath);
                        scale = "x5";
                        break;
                    default:
                        Debug.LogError("Cannot resolve mesh scale");
                        break;
                }

                mesh = MapUtils.BuildMap(cellPathPacket.path3D,
                    cellPathPacket.path1D,
                    false,
                    cellPathPacket.pathTris).SurfaceGeometry.Mesh;

                mesh.RecalculateNormals();

                mesh.name = mesh.name + scale;
                
                scaledMeshes[(int)meshScale] = mesh;
            }

            return scaledMeshes[(int)meshScale];
        }*/
        
        private Mesh BuildMesh(double inflation = 1, int refinement = 0)
        {
            Mesh mesh = null;

            // 1 <= inflation
            inflation = Math.Max(inflation, 1);
            // 0 <= refinement
            refinement = Math.Max(refinement, 0);

            if (reader == null) reader = new vrnReader(vrnCellPath);
            mesh = MapUtils.BuildMap(reader.Retrieve2DMeshName(inflation),
                reader.Retrieve1DMeshName(refinement),
                false).SurfaceGeometry.Mesh;

            mesh.RecalculateNormals();

            mesh.name = mesh.name + "ref" + refinement.ToString() + "inf" + inflation.ToString();

            return mesh;
        }

        /*        public void SwitchColliderMesh(int scale)
                {    
                    scale = Math.Clamp(scale, 0, 4);

                    if(scaledMeshes[scale] == null) BuildMesh((MeshScaling)scale);



                    meshColController.Mesh = scaledMeshes[scale];
                    //meshColController.Mesh = BuildMesh(scale);
                }

                public void SwitchMesh(int scale)
                {
                    scale = Math.Clamp(scale, 0, 4);

                    if (scaledMeshes[scale] == null) BuildMesh((MeshScaling)scale);


                    MeshFilter mf = GetComponent<MeshFilter>();

                    if (mf != null) mf.sharedMesh = scaledMeshes[scale];
                    //if (mf != null) mf.sharedMesh = BuildMesh(scale);
                    else Debug.LogError("No MeshFilter found on " + name);
                }*/

        public void SwitchColliderMesh(int scale)
        {
            scale = Math.Clamp(scale, 0, 4);

            meshColController.Mesh = BuildMesh(scale);
            //meshColController.Mesh = BuildMesh(scale);
        }

        public void SwitchMesh(int scale)
        {
            scale = Math.Clamp(scale, 0, 4);


            MeshFilter mf = GetComponent<MeshFilter>();

            if (mf != null) mf.sharedMesh = BuildMesh(scale);
            //if (mf != null) mf.sharedMesh = BuildMesh(scale);
            else Debug.LogError("No MeshFilter found on " + name);
        }

        /// <summary>
        /// Switch the visualization or collider mesh
        /// </summary>
        /// <param name="mesh"></param>
        private void SwitchColliderMesh(Mesh mesh)
        {
            if(meshColController != null)
            {
                if (mesh != null)
                {
                    meshColController.Mesh = mesh;
                }
                else Debug.LogError("Mesh given for collider is invalid.");
            }
            else Debug.LogError("No MeshColController found.");
        }
    }
}