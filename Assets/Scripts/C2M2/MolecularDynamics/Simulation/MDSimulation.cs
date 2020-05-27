using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
using C2M2.MolecularDynamics.Visualization;
namespace C2M2.MolecularDynamics.Simulation
{
    using Utils;
    public abstract class MDSimulation : PositionFieldSimulation
    {
        private readonly string relPath = Application.streamingAssetsPath + @"/MolecularDynamics/";
        public string pdbFilePath = "PE/pe_cg.pdb";
        public string psfFilePath = "PE/octatetracontane_128.cg.psf";
        protected Dictionary<Transform, int> molLookup;
        protected Vector3[] x = null;
        protected Vector3[] v = null;
        protected Vector3[] r = null;
        protected int[] bonds = null;
        protected int[] angles = null;
	protected float[] mass = null;
        protected int[][] bond_topo = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// In the future, MDSimulation should this method with the PDBReader, 
        /// so you won't need to worry about coding it every time. 
        /// We don't have a PDB file, so we create so make-believe positions
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override Transform[] BuildTransforms()
        {
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
                spheres[i] = new Sphere(x[i], 1.5);
            }

            // Instantiate the created spheres and return their transform components
            SphereInstantiator instantiator = gameObject.AddComponent<SphereInstantiator>();
            Transform[] transforms = instantiator.InstantiateSpheres(spheres, "Molecule", "Atom");

            // Create a lookup so that given a transform hit by a raycast we can get the molecule's index
            molLookup = new Dictionary<Transform, int>(transforms.Length);
            for (int i = 0; i < transforms.Length; i++)
            {
                molLookup.Add(transforms[i], i);
            }

            bond_topo = BuildBondTopology(bonds);

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

                try
                {
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
                }catch(IndexOutOfRangeException e)
                {
                    Debug.LogError("a: " + a
                        + "\nb: " + b
                        + "\nmaxInd: " + maxInd
                        + "\n" + e);
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
        }

    }
}
