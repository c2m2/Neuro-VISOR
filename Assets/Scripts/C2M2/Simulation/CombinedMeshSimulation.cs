using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2;
using System;
using GetSocialSdk.Capture.Scripts;

namespace C2M2
{
    using Interaction;
    namespace Simulation
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> Run a blank ScalarFieldSimulation using every MeshRenderer childed under this script  </summary>
        ///
        /// <remarks>   Jacob, 4/29/2020. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public class CombinedMeshSimulation : ScalarFieldSimulation
        {
            private double[] values;

            #region SimulationMethods
            // Retrieve simulation values as a double array
            public override double[] GetValues()
            {
                return values;
            }

            // Your simulation will receive new simulation values between 0 and 1 for each point
            public override void SetValues(RaycastHit hit)
            {
                Tuple<int, double>[] newValues = RaycastSimHeaterDiscrete.HitToTriangles(hit);

                foreach(Tuple<int, double> value in newValues)
                {
                    values[value.Item1] = value.Item2;
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            /// <summary> Get every child mesh and combine into one mesh </summary>
            /// <returns> Combined mesh </returns>
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            protected override Mesh BuildMesh()
            {
                Mesh combinedMesh, colMesh;

                // See if user made custom mesh collider source
                
                Transform colRoot = transform.Find("Collider");
                if (colRoot != null)
                {
                    colMesh = CombineMeshes(colRoot);
                    colMesh.name = "Combined_Mesh_Collider";
                    VRRaycastable raycastable = GetComponent<VRRaycastable>();
                    if (raycastable == null) { raycastable = gameObject.AddComponent<VRRaycastable>(); }
                    raycastable.ColliderMesh = colMesh;
                }

                combinedMesh = CombineMeshes(transform);
                // Store vert count for simulation use
                values = new double[combinedMesh.vertexCount];
                
                return combinedMesh;
            }
            private Mesh CombineMeshes(Transform root)
            {
                Debug.Log("Combining from " + root.gameObject.name);
                MeshFilter[] mfs = root.GetComponentsInChildren<MeshFilter>(true);
                return CombineMeshes(mfs);
            }
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
                for (int i = mfs.Length-1; i >= 0; i--)
                {
                    MeshFilter mf = mfs[i];
                    if (mf.gameObject != gameObject) { DestroyImmediate(mf.gameObject); }
                }

                return combinedMesh;
            }
            
            // Simulation computation should be contained in this method.
            // This method will launch in its own thread.
            protected override void Solve()
            {
                DateTime t0 = DateTime.Now;
                while (true)
                {
                    float dt = (DateTime.Now - t0).Milliseconds;
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] -= dt*values[i];
                    }
                    t0 = DateTime.Now;
                }
            }
            #endregion
        }
    }
}
