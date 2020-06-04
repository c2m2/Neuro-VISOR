using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
using C2M2.MolecularDynamics.Visualization;
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
        LineRenderer renderer;
        private static Color defaultCol = Color.black;
        public BondRenderer(Transform a, Transform b, float width = 1f)
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
            Update();
        }
        public void Update()
        {
            renderer.SetPositions(new Vector3[] { a.position, b.position });
        }
    }
    public abstract class MDSimulation : PositionFieldSimulation
    {
        private readonly string relPath = Application.streamingAssetsPath + @"/MolecularDynamics/";
        public string pdbFilePath = "PE/pe_cg.pdb";
        public string psfFilePath = "PE/octatetracontane_128.cg.psf";
        public float radius = 1f;
        public Material bondMaterial;

        protected Dictionary<Transform, int> molLookup;
        protected Vector3[] x = null;
        protected Vector3[] v = null;
        protected Vector3[] r = null;
        protected int[] bonds = null;
        protected int[] angles = null;
	    protected float[] mass = null;
        protected int[][] bond_topo = null;

        private BondRenderer[] bondRenderers;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// In the future, MDSimulation should this method with the PDBReader, 
        /// so you won't need to worry about coding it every time. 
        /// We don't have a PDB file, so we create so make-believe positions
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override Transform[] BuildTransforms()
        {
            Timer timer = new Timer();
            timer.StartTimer();

            PDBFile pdbFile = PDBReader.ReadFile(relPath + pdbFilePath);
            PSFFile psfFile = PSFReader.ReadFile(relPath + psfFilePath);
            bonds = psfFile.bonds;
	        angles = psfFile.angles;
	        mass = psfFile.mass;
            Debug.Log(mass[2047]);

            // Convert bonds and angles to 0 base
            for(int i = 0; i < bonds.Length; i++)
            {
                bonds[i] = bonds[i] - 1;
            }
	        for(int i = 0; i < angles.Length; i++)
            {
                angles[i] = angles[i] - 1;
            }
            
            x = pdbFile.pos;

            // Initialize v
            v = new Vector3[x.Length];
            for(int i = 0; i < v.Length; i++)
            {
                v[i] = Vector3.zero;
            }

            Sphere[] spheres = new Sphere[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                //spheres[i] = new Sphere(x[i], radius);
                spheres[i] = new Sphere(Vector3.zero, radius);
            }

            // Instantiate the created spheres and return their transform components
            SphereInstantiator instantiator = gameObject.AddComponent<SphereInstantiator>();
            Transform[] transforms = instantiator.InstantiateSpheres(spheres, "Molecule", "Atom");
            // Apply positions to the transforms
            for(int i = 0; i < x.Length; i++)
            {
                transforms[i].localPosition = x[i];
            }

            // Create a lookup so that given a transform hit by a raycast we can get the molecule's index
            molLookup = new Dictionary<Transform, int>(transforms.Length);
            for (int i = 0; i < transforms.Length; i++)
            {
                molLookup.Add(transforms[i], i);
            }

            bond_topo = BuildBondTopology(bonds);

            ResizeField(transforms);
            RenderBonds(bonds, transforms);

            timer.StopTimer("BuildTransforms");
            timer.ExportCSV("MDSimulation.BuildTransforms");

            return transforms;

            int[][] BuildBondTopology(int[] bonds)
            {
                int maxInd = bonds.Max();
                List<int>[] bond_topo_list = new List<int>[maxInd + 1]; //make list of bond connections
                int i = 0;
                for(i = 0; i < bond_topo_list.Length; i++)
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
                for(i = 0; i < bond_topo_list.Length; i++)
                {
                    bond_topo[i] = bond_topo_list[i].ToArray();
                }
                //Debug.Log(bond_topo[0][1]);
                return bond_topo;
            }
            void RenderBonds(int[] bonds, Transform[] sphereTransforms)
            {
                bondRenderers = new BondRenderer[bonds.Length / 2];
               // bondViz = new XRLineRenderer[bonds.Length];
                int j = 0;
                for(int i = 0; i < bonds.Length-1; i+=2)
                {
                    /*Transform bondA = sphereTransforms[bonds[i]];
                    Transform bondB = sphereTransforms[bonds[i + 1]];

                    // Build child object for the line
                    Transform lineObj = new GameObject().transform;
                    lineObj.name = "Bond[" + bondA.name + "->" + bondB.name + "]";
                    lineObj.parent = bondA;
                    //lineObj.localPosition = Vector3.zero;
                    //lineObj.eulerAngles = Vector3.zero;

                    // Set rendering options for the line
                    XRLineRenderer line = lineObj.gameObject.AddComponent<XRLineRenderer>();
                    line.SetPositions(new Vector3[] { bondA.localPosition, bondB.localPosition });
                    line.sharedMaterial = GameManager.instance.lineRendMaterial;
                    line.widthStart = radius;
                    line.widthEnd = radius;

                    // Store the line
                    bondViz[j] = line;
                    j++;*/
                    bondRenderers[j] = new BondRenderer(sphereTransforms[bonds[i]], sphereTransforms[bonds[i + 1]], 0.001f);
                    j++;
                }               
            }
            void ResizeField(Transform[] sphereTransforms)
            {
                // Separate transform positions into their parts
                float[] xs = new float[sphereTransforms.Length];
                float[] ys = new float[sphereTransforms.Length];
                float[] zs = new float[sphereTransforms.Length];
                Vector3[] positions = new Vector3[sphereTransforms.Length];
                for(int i = 0; i < sphereTransforms.Length; i++)
                {
                    xs[i] = sphereTransforms[i].position.x;
                    ys[i] = sphereTransforms[i].position.y;
                    zs[i] = sphereTransforms[i].position.z;
                }
                float xScale = 1, yScale = 1, zScale = 1;
                float[] boundsArray = { xs.Max(), ys.Max(), zs.Max() };
                float max = boundsArray.Max();

                Vector3 targetSize = GameManager.instance.objectScaleDefault;
                float[] targetArray = { targetSize.x, targetSize.y, targetSize.z };
                float min = Mathf.Min(targetArray);
                xScale = min / max;
                yScale = xScale;
                zScale = xScale;

                // Get the parent of all the atom transforms
                Transform mol = sphereTransforms[0].parent;
                Debug.Log("mol.name: " + mol.name);
                mol.localScale = new Vector3(xScale, yScale, zScale);
            }
        }        
        /// <summary>
        /// Called after position field transforms are updated, used here to update bond visualization
        /// </summary>
        protected override void UpdateVisChild(in Vector3[] simulationValues)
        {
            if(bondRenderers != null)
            {
                for(int i = 0; i < bondRenderers.Length; i++)
                {
                    bondRenderers[i].Update();
                }
            }      
        }

    }
}
