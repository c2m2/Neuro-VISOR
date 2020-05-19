using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
using C2M2.MolecularDynamics.Visualization;
namespace C2M2.MolecularDynamics.Simulation
{
    public abstract class MDSimulation : PositionFieldSimulation
    {
        public string pdbFilePath;
        protected Dictionary<Transform, int> molLookup;
        protected Vector3[] x = null;
        protected Vector3[] v = null;
        protected Vector3[] r = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// In the future, MDSimulation should this method with the PDBReader, 
        /// so you won't need to worry about coding it every time. 
        /// We don't have a PDB file, so we create so make-believe positions
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override Transform[] BuildTransforms()
        {
            PDBFile pdbfile = PDBReader.ReadFile(pdbFilePath);

            x = pdbfile.pos;
            Sphere[] spheres = new Sphere[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                spheres[i] = new Sphere(x[i], 1.5);
            }

            // Instantiate the created spheres and return their transform components
            SphereInstantiator instantiator = gameObject.AddComponent<SphereInstantiator>();
            Transform[] transforms = instantiator.InstantiateSpheres(spheres);

            // Create a lookup so that given a transform hit by a raycast we can get the molecule's index
            molLookup = new Dictionary<Transform, int>(transforms.Length);
            for (int i = 0; i < transforms.Length; i++)
            {
                molLookup.Add(transforms[i], i);
            }
            return null;
        }

    }
}
