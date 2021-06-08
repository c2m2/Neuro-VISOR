using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.MolecularDynamics.Visualization
{
    public struct Sphere
    {
        public readonly Vector3 position;
        public readonly float radius;
        // Constructors
        public Sphere(Vector3 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }
        public Sphere(Vector3 position)
        {
            this.position = position;
            radius = 1;
        }
    }
}
