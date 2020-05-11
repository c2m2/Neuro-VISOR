using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2
{
    using Readers;
    namespace Simulation
    {
        public class MDSolver : MDSimulation
        {
            private Vector3[] values;

            // Scripts will try to get the most up to date simulation values using this function
            public override Vector3[] GetValues()
            {
                return values;
            }

            // Scripts will try to affect your simulation values using this function
            public override void SetValues(RaycastHit hit)
            {
                
            }

            // Simulation code is contained here
            protected override void Solve()
            {
                // Initialize simulation values

                // Run simulation
                for(int i = 0; i < values.Length; i++)
                {
                    // You could also do this in a while loop, so that your simulation runs indefinitely

                }


            }

        }
    }
}
