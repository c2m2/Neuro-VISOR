using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2;
using System;

namespace C2M2.Simulation.Samples
{
    using Interaction;
    using Interaction.VR;
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary> Run a blank ScalarFieldSimulation using every MeshRenderer childed under this script  </summary>
    ///
    /// <remarks>   Jacob, 4/29/2020. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class CombinedMeshSimulation : Simulation.MeshSimulation
    {
        private double[] values;
        public override float GetSimulationTime() { return 0; }
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Send values upwards for visualization
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override double[] GetValues()
        {
            return values;
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Incorporate a raycast hit interaction into simulation values
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override void SetValues(RaycastHit hit)
        {
            Tuple<int, double>[] newValues = ((RaycastSimHeaterDiscrete)Heater).HitToTriangles(hit);

            foreach (Tuple<int, double> value in newValues)
            {
                values[value.Item1] = value.Item2;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> Get every child mesh and combine into one mesh </summary>
        /// <returns> Combined mesh </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override Mesh BuildVisualization()
        {
            Mesh combinedMesh, colMesh;

            // See if user made custom mesh collider source

            Transform colRoot = transform.Find("Collider");
            if (colRoot != null)
            {
                colMesh = CombineMeshes(colRoot);
                colMesh.name = "Combined_Mesh_Collider";
                VRRaycastableMesh raycastable = GetComponent<VRRaycastableMesh>();
                if (raycastable == null) { raycastable = gameObject.AddComponent<VRRaycastableMesh>(); }
                raycastable.SetSource(colMesh);
            }

            combinedMesh = CombineMeshes(transform);
            // Store vert count for simulation use
            values = new double[combinedMesh.vertexCount];

            return combinedMesh;
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Find all meshes childed under a root transform and combine them into one.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private Mesh CombineMeshes(Transform root)
        {
            Debug.Log("Combining from " + root.gameObject.name);
            MeshFilter[] mfs = root.GetComponentsInChildren<MeshFilter>(true);
            return CombineMeshes(mfs);
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Take all MeshFilter.mesh's from an array and combine them into a single Mesh
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private Mesh CombineMeshes(MeshFilter[] mfs)
        {
            List<CombineInstance> meshesToCombine = new List<CombineInstance>(mfs.Length);

            for (int i = 0; i < mfs.Length; i++)
            {
                if (mfs[i].mesh != null)
                {
                    CombineInstance combine = new CombineInstance();

                    combine.mesh = mfs[i].mesh;
                    combine.transform = mfs[i].transform.localToWorldMatrix;

                    meshesToCombine.Add(combine);
                }
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(meshesToCombine.ToArray());
            combinedMesh.RecalculateNormals();

            // Destroy old objects
            for (int i = mfs.Length - 1; i >= 0; i--)
            {
                MeshFilter mf = mfs[i];
                if (mf.gameObject != gameObject) { DestroyImmediate(mf.gameObject); }
            }

            return combinedMesh;
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Simply degrades the value at each point by the time change times its current value
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override void Solve()
        {
            DateTime t0 = DateTime.Now;
            while (true)
            {
                float dt = (DateTime.Now - t0).Milliseconds;
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] -= dt * values[i];
                }
                t0 = DateTime.Now;
            }
        }
    }
}