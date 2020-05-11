using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2
{
    using Readers;
    namespace Simulation
    {
        public abstract class MDSimulation : Vector3Simulation
        {
            public string pdbFilePath;

            protected override Transform[] BuildTransforms()
            {
                Sphere[] spheres = PDBReader.ReadFile(pdbFilePath);
                SphereInstantiator instantiator = gameObject.AddComponent<SphereInstantiator>();
                Transform[] transforms = instantiator.InstantiateSpheres(spheres);
                return transforms;
            }

        }
    }
}
