using UnityEngine;
using System;

namespace C2M2.Interaction
{
    using C2M2.Utils.Exceptions;
    using Simulation;
    public class RaycastHeater : MonoBehaviour
    {
        protected Interactable simulation;
        private MeshFilter mf;
        public double value = 55;

        private void Awake()
        {
            mf = GetComponent<MeshFilter>() ?? throw new MeshFilterNotFoundException();
            // This script is useless if there isn't a simulation to effect
            simulation = GetComponent<Interactable>() ?? throw new SimulationNotFoundException();
        }

        /// <summary> 
        /// Derived classes decide how to translate RaycastHit into index/newValue pairings 
        /// </summary>
        public void Hit(RaycastHit hit) => simulation.SetValues(hit);

        // Return an array of 3D indices and new values to add to those indices
        protected Tuple<int, double>[] HitMethod(RaycastHit hit)
        {
            // We will have 3 new index/value pairings
            Tuple<int, double>[] newValues = new Tuple<int, double>[3];

            // Translate hit triangle index so we can index into triangles array
            int triInd = hit.triangleIndex * 3;
            // Get mesh vertices from hit triangle
            int v1 = mf.mesh.triangles[triInd];
            int v2 = mf.mesh.triangles[triInd + 1];
            int v3 = mf.mesh.triangles[triInd + 2];

            // Attach new values to new vertices
            newValues[0] = new Tuple<int, double>(v1, value);
            newValues[1] = new Tuple<int, double>(v2, value);
            newValues[2] = new Tuple<int, double>(v3, value);

            return newValues;
        }

        public Tuple<int, double>[] HitToTriangles(RaycastHit hit)
        {
            // We will have 3 new index/value pairings
            Tuple<int, double>[] newValues = new Tuple<int, double>[3];

            // Translate hit triangle index so we can index into triangles array
            int triInd = hit.triangleIndex * 3;
            MeshFilter mf = hit.transform.GetComponentInParent<MeshFilter>();
            // Get mesh vertices from hit triangle
            int v1 = mf.mesh.triangles[triInd];
            int v2 = mf.mesh.triangles[triInd + 1];
            int v3 = mf.mesh.triangles[triInd + 2];

            // Attach new values to new vertices
            newValues[0] = new Tuple<int, double>(v1, value);
            newValues[1] = new Tuple<int, double>(v2, value);
            newValues[2] = new Tuple<int, double>(v3, value);

            return newValues;
        }
    }
}
