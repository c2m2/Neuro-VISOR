using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2 {
    namespace SimulationScripts {
        public class ExampleMDSimulation : MDSimulation
        {
            // Define number of example spheres
            public int numSpheres = 1000;
            // Example radius for spheres
            public float radius = 0.1f;

            public int timestepCount = 1000000;
            public float timestepSize = 0.000002f;

            private Vector3[] values = null;

            public override Vector3[] GetValues()
            {
                return values;
            }
            public override void SetValues(Tuple<int, double>[] newValues)
            {
                throw new NotImplementedException();
            }
            /// Normally MDSimulation implements this method with the PDBReader, 
            /// and you don't need to worry about it. We don't have a PDB file, so
            /// we create so make-believe positions
            protected override Transform[] BuildTransforms()
            {
                // Create 5 spheres
                Sphere[] spheres = new Sphere[numSpheres];
                values = new Vector3[numSpheres];
                for(int i = 0; i < spheres.Length; i++)
                {
                    // Put our new spheres in a straight line and store their positions as simulation values
                    Vector3 pos = new Vector3(i, 0, 0);
                    spheres[i] = new Sphere(pos, radius);
                    values[i] = pos;
                }

                // Instantiate the created spheres and return their transform components
                SphereInstantiator instantiator = gameObject.AddComponent<SphereInstantiator>();
                Transform[] transforms = instantiator.InstantiateSpheres(spheres);
                return transforms;
            }

            // This method will launch in its own thread
            protected override void Solve()
            {
                // Define number of timesteps
                int nT = timestepCount;
                // Define timestep size: here we make it extremely small so that the simulation stays within a small area.
                // You don't have to do that just write your simulation code and we'll figure out scaling
                float dt = timestepSize;

                // Iterate over every timestep
                for (int t = 0; t < nT; t++)
                {
                    // iterate over every simulation value
                    for(int i = 0; i < values.Length; i++)
                    {
                        Vector3 oldPos = values[i];
                        // Just add the timestep to each sphere's position
                        float newX = oldPos.x + dt;
                        values[i] = new Vector3(newX, oldPos.y, oldPos.z);        
                    }
                }

                Debug.Log("ExampleMDSimulation complete.");
            }
        }
    }
}
