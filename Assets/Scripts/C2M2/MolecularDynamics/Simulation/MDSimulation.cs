using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
using C2M2.MolecularDynamics.Visualization;
using System.Linq;
namespace C2M2.MolecularDynamics.Simulation
{
    using Utils;

    /// <summary>
    /// Stores and updates information needed to render bonds
    /// </summary>
    public struct BondRenderer
    {
        public Transform a { get; private set; }
        public Transform b { get; private set; }
        public float maxLength { get; private set; }

        LineRenderer renderer;
        public BondRenderer(Transform a, Transform b, float width = 1f, float maxLength = float.PositiveInfinity)
        {
            this.a = a;
            this.b = b;
            renderer = a.gameObject.AddComponent<LineRenderer>();
            renderer.sharedMaterial = GameManager.instance.lineRendMaterial;
            renderer.positionCount = 2;
            renderer.startWidth = width;
            renderer.endWidth = width;
            renderer.startColor = Color.black;
            renderer.endColor = Color.black;

            this.maxLength = maxLength;

            Update();
        }
        public void Update()
        {
            if (Vector3.Distance(a.position, b.position) < maxLength)
                renderer.SetPositions(new Vector3[] { a.position, b.position });
            else
                renderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });
        }
    }
    
    /// <summary>
    /// Reads data files and creates and updates visualizations for molecular dynamics systemss
    /// </summary>
    public abstract class MDSimulation : PositionFieldSimulation
    {
        protected class Vector3Int
        {
            public int x = 0;
            public int y = 0;
            public int z = 0;
            
            public Vector3Int(int x, int y, int z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public static Vector3Int zero = new Vector3Int(0, 0, 0);
        }

        // Folder path to PDB and PSF files
        private readonly string path = Application.streamingAssetsPath + @"/MolecularDynamics/";
        [Tooltip("Path to PDB file, relative to Assets/StreamingAssets/MolecularDynamics/")]
        public string pdbPath = "PE/pe_cg.pdb";
        [Tooltip("Path to PSF file, relative to Assets/StreamingAssets/MolecularDynamics/")]
        public string psfPath = "PE/octatetracontane_128.cg.psf";
        [Tooltip("Initial radius of atoms in the system. These will be scaled down to fit inside of the room")]
        public float radius = 1f;
        [Tooltip("Name of shader to use on particles")]
        public string shaderName = "Standard";
        [Tooltip("Colors to use on each atom type. If not enough colors are provided, random colors will be generated for each additional type.")]
        public Color[] atomColors = new Color[] { Color.cyan, Color.gray };

        public Material bondMaterial;
        public float maxBondLength = 10f;

        public enum MethodType { gjI, gjII, gjIII }
        public MethodType methodType = MethodType.gjI;

        public int timestepCount = 50000;
        public float timestepSize = .1f;
        public float gamma = 0.1f;

        // TODO: This needs to be read from a separate data file
        public float boxLengthX = 58.065f;
        public float boxLengthY = 58.065f;
        public float boxLengthZ = 58.065f;

        protected float c = -1;

        protected Dictionary<Transform, int> particleLookup;
        protected Vector3[] coord = null;
        /// <summary> Velocity of each particle </summary>
        protected Vector3[] vel = null;
        protected Vector3[] r = null;
        protected Vector3Int[] pbcFlag = null;

        /// <summary> Each two indices represents one bond. </summary>
        /// <remarks>
        /// If bonds[0] = 10 and bonds[1] = 15, then the particles at x[10] & x[15] represent a bonded pair.
        /// </remarks>
        protected int[] bonds = null;
        protected int[] angles = null;
	    protected float[] mass = null;
        protected string[] types = null;
        protected int[][] bond_topo = null;

        private BondRenderer[] bondRenderers;
        private Shader shader;

        protected override void ReadData()
        {
            PSFFile psfFile = PSFReader.ReadFile(path + psfPath);
            mass = psfFile.mass;
            bonds = psfFile.bonds;
            angles = psfFile.angles;
            types = psfFile.types;       

            // Convert bonds and angles to 0 base
            for (int i = 0; i < bonds.Length; i++) bonds[i] = bonds[i] - 1;
            for (int i = 0; i < angles.Length; i++) angles[i] = angles[i] - 1;

            PDBFile pdbFile = PDBReader.ReadFile(path + pdbPath);
            coord = pdbFile.pos;

            // Initialize v's to zero
            vel = new Vector3[coord.Length];
            for (int i = 0; i < vel.Length; i++)
            {
                vel[i] = Vector3.zero;
            }

            // Initialize pbc flags to zero
            pbcFlag = new Vector3Int[coord.Length];
            for(int i = 0; i < pbcFlag.Length; i++)
            {
                pbcFlag[i] = Vector3Int.zero;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Reads and stores PDB and PSF data, uses the data to build the system visualization
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override Transform[] BuildVisualization()
        {
            Transform[] transforms = RenderSpheres(coord, types, radius);

            bond_topo = BuildBondTopology(bonds);

            ResizeField(transforms);

            RenderBonds(bonds, transforms);

            // Initialize simulation parameter
            switch (methodType)
            {
                case (MethodType.gjI):
                    c = gjI(gamma, timestepSize);
                    break;
                case (MethodType.gjII):
                    c = gjII(gamma, timestepSize);
                    break;
                case (MethodType.gjIII):
                    c = gjIII(gamma, timestepSize);
                    break;
            }

            return transforms;

            Transform[] RenderSpheres(Vector3[] x, string[] types, float radius)
            {
                // Instantiate one sphere per atom
                Sphere[] spheres = new Sphere[x.Length];
                for (int i = 0; i < x.Length; i++) spheres[i] = new Sphere(Vector3.zero, radius);
                // Instantiate sphere objects in the scene and store their transform components for later reference
                SphereInstantiator instantiator = gameObject.AddComponent<SphereInstantiator>();
                Transform[] ts = instantiator.InstantiateSpheres(spheres, true, "Molecule", "Atom");

                // Resolve which colors and shader to use for our system
                shader = Shader.Find(shaderName);
                string[] atomTypes = types.Distinct().ToArray();
                Color[] uniqueCols = new Color[atomTypes.Length];
                // Use the user-given colors first, then randomly generate other necessary colors
                for (int i = 0; i < uniqueCols.Length; i++)
                {
                    if (i < atomColors.Length) uniqueCols[i] = atomColors[i];
                    else uniqueCols[i] = UnityEngine.Random.ColorHSV();
                }
                atomColors = uniqueCols;

                // Create a material for each unique atom type and add it to a dictionary
                Dictionary<string, Material> matLookup = new Dictionary<string, Material>(ts.Length);
                for (int i = 0; i < atomTypes.Length; i++)
                {
                    Material mat = new Material(shader);
                    mat.color = atomColors[i];
                    mat.name = atomTypes[i] + "Mat";
                    matLookup.Add(atomTypes[i], mat);
                }

                // Apply positions and materials to atoms
                if (ts.Length != x.Length) throw new Exception("Number of atoms does not match number of positions given!");
                if (types.Length != x.Length) throw new Exception("Number of atoms does not match number of atom types given!");
                for (int i = 0; i < ts.Length; i++)
                {
                    ts[i].localPosition = x[i];
                    ts[i].GetComponent<MeshRenderer>().sharedMaterial = matLookup[types[i]];
                }

                // Create a lookup so that given a transform hit by a raycast we can get the molecule's index
                particleLookup = new Dictionary<Transform, int>(ts.Length);
                for (int i = 0; i < ts.Length; i++) particleLookup.Add(ts[i], i);

                return ts;
            }

            int[][] BuildBondTopology(int[] bonds)
            {
                int maxInd = bonds.Max();
                List<int>[] bond_topo_list = new List<int>[maxInd + 1]; //make list of bond connections
                int i = 0;
                for (i = 0; i < bond_topo_list.Length; i++)
                {
                    bond_topo_list[i] = new List<int>();
                }

                int a = -1, b = -1;

                // Look at each bond and store it in our symmetric matrix
                for (i = 0; i < bonds.Length; i += 2)
                {
                    if (!(bonds.Length % 2 == 0)) throw new System.Exception("Bond array is not divisible by 2");

                    // Convert indices to be zero based
                    a = bonds[i];
                    b = bonds[i + 1];
                    bond_topo_list[a].Add(b);
                    bond_topo_list[b].Add(a);
                }

                // Convert array of lists to jagged array
                bond_topo = new int[maxInd + 1][];
                for (i = 0; i < bond_topo_list.Length; i++)
                {
                    bond_topo[i] = bond_topo_list[i].ToArray();
                }
                //Debug.Log(bond_topo[0][1]);
                return bond_topo;
            }

            void ResizeField(Transform[] sphereTransforms)
            {
                // Separate transform positions into their parts
                float[] xs = new float[sphereTransforms.Length];
                float[] ys = new float[sphereTransforms.Length];
                float[] zs = new float[sphereTransforms.Length];
                Vector3[] positions = new Vector3[sphereTransforms.Length];

                // Separate positions
                for(int i = 0; i < sphereTransforms.Length; i++)
                {
                    xs[i] = sphereTransforms[i].position.x;
                    ys[i] = sphereTransforms[i].position.y;
                    zs[i] = sphereTransforms[i].position.z;
                }

                // TODO: Need max of absolute values, not just max's here

                float[] boundsArray = { xs.Max(), ys.Max(), zs.Max() };
                float max = boundsArray.Max();

                Vector3 targetSize = GameManager.instance.objDefaultScale;
                float[] targetArray = { targetSize.x, targetSize.y, targetSize.z };
                float min = targetArray.Min();

                float xScale = 1, yScale = 1, zScale = 1;
                xScale = min / max;
                yScale = xScale;
                zScale = xScale;

                // Get the parent of all the atom transforms
                Transform mol = sphereTransforms[0].parent;
                mol.localScale = new Vector3(xScale, yScale, zScale);
            }

            void RenderBonds(int[] bonds, Transform[] sphereTransforms)
            {
                // The max bond length should be 1/3 of the shortest edge length
                //float maxLength = Math.Min(Math.Min(boxLengthX, boxLengthY), boxLengthZ) / 3;

                bondRenderers = new BondRenderer[bonds.Length / 2];
                int j = 0;
                for (int i = 0; i < bonds.Length - 1; i += 2)
                {
                    bondRenderers[j] = new BondRenderer(
                        sphereTransforms[bonds[i]], 
                        sphereTransforms[bonds[i + 1]], 
                        0.001f,
                        maxBondLength);
                    j++;
                }
            }
        }

        private float gjI(float gamma, float dt) => (1 - gamma * dt / 2) / (1 + gamma * dt / 2);
        private float gjII(float gamma, float dt) => (float)System.Math.Exp(-gamma * dt);
        private float gjIII(float gamma, float dt) => 1 - (gamma * dt);

        /// <summary>
        /// Used here to update bond visualization.
        /// </summary>
        /// <remarks>
        /// Called after position field transforms are updated.
        /// </remarks>
        protected override void UpdateVisChild(in Vector3[] simulationValues)
        {
            if (bondRenderers == null) return;
            for (int i = 0; i < bondRenderers.Length; i++)
            {
                bondRenderers[i].Update();
            }
        }
    }
}
