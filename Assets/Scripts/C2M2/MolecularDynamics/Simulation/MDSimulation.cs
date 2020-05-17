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

        protected override Transform[] BuildTransforms()
        {
            //Sphere[] spheres = PDBReader.ReadFile(pdbFilePath);
            //SphereInstantiator instantiator = gameObject.AddComponent<SphereInstantiator>();
            //Transform[] transforms = instantiator.InstantiateSpheres(spheres);
            //return transforms;
	    return null;
        }

    }
}
