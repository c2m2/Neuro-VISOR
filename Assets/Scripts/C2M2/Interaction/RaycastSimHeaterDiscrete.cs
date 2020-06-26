using UnityEngine;
using System;
using C2M2.Utils.Exceptions;
namespace C2M2.Interaction
{
    public class RaycastSimHeaterDiscrete : RaycastHeater
    {
        private MeshFilter mf;
        protected override void OnAwake()
        {
            mf = GetComponent<MeshFilter>() ?? throw new MeshFilterNotFoundException();
        }
        protected override Tuple<int, double>[] HitMethod(RaycastHit hit)
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
            newValues[0] = new Tuple<int, double>(v1, 1);
            newValues[1] = new Tuple<int, double>(v2, 1);
            newValues[2] = new Tuple<int, double>(v3, 1);

            return newValues;
        }

        public static Tuple<int, double>[] HitToTriangles(RaycastHit hit)
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
            newValues[0] = new Tuple<int, double>(v1, 1);
            newValues[1] = new Tuple<int, double>(v2, 1);
            newValues[2] = new Tuple<int, double>(v3, 1);

            return newValues;
        }
    }
}
