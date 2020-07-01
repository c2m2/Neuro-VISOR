using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Tests;
namespace C2M2.NeuronalDynamics.Tests
{
    [System.Obsolete("This is an obsolete/not required test but still might be useful to test mesh normals read in vs. computed normals from unity")]
    public class NormalTest : AwakeTest
    {
        /// Mesh
        private MeshFilter mf;

        public override bool PreTest() {
            mf = GameObject.Find ( "HHSolver" ).GetComponent<MeshFilter>();
            if ( mf is null ) {
                UnityEngine.Debug.LogError ( "Mesh not found!" );
            }
            return mf is null;
        }

        public override bool RunTest() {
            Mesh mesh = mf.sharedMesh;

            int numCCWs = 0;
            int numCWs = 0;

            UnityEngine.Debug.Log ( $"num triangles: {mesh.triangles.Length/3}" );
            for ( int i = 0; i < mesh.triangles.Length; i += 3 ) {
                Vector3 x0 = mesh.vertices[mesh.triangles[i + 0]];
                Vector3 x1 = mesh.vertices[mesh.triangles[i + 1]];
                Vector3 x2 = mesh.vertices[mesh.triangles[i + 2]];
                Vector3 x3 = new Vector3 ( 1, 1, 1 );

                float[,] mat = new float[,] { { x0.x, x0.y, x0.z, 1 }, 
                { x1.x, x1.y, x1.z, 1 }, { x2.x, x2.y, x2.z, 1, }, 
                { x3.x, x3.y, x3.z, 1 } };

                float det = determinant ( mat );
                if ( det <= 0 ) {
                    numCCWs++;
                } else {
                    numCWs++;
                }
            }
            UnityEngine.Debug.Log ( $"numCCWs: {numCCWs}" );
            UnityEngine.Debug.Log ( $"numCWs: {numCWs}" );

            Vector3[] normals = mesh.normals;
            for ( int i = 0; i < normals.Length; i++ ) {
                UnityEngine.Debug.Log ( $"normal: {normals[i]}" );
            }

            return true;
        }

        /// Helper function to calculaet the 4x4 determinant
        private float determinant ( float[,] m ) {
            return
                m[0,3] * m[1,2] * m[2,1] * m[3,0] - m[0,2] * m[1,3] * m[2,1] * m[3,0] -
                m[0,3] * m[1,1] * m[2,2] * m[3,0] + m[0,1] * m[1,3] * m[2,2] * m[3,0] +
                m[0,2] * m[1,1] * m[2,3] * m[3,0] - m[0,1] * m[1,2] * m[2,3] * m[3,0] -
                m[0,3] * m[1,2] * m[2,0] * m[3,1] + m[0,2] * m[1,3] * m[2,0] * m[3,1] +
                m[0,3] * m[1,0] * m[2,2] * m[3,1] - m[0,0] * m[1,3] * m[2,2] * m[3,1] -
                m[0,2] * m[1,0] * m[2,3] * m[3,1] + m[0,0] * m[1,2] * m[2,3] * m[3,1] +
                m[0,3] * m[1,1] * m[2,0] * m[3,2] - m[0,1] * m[1,3] * m[2,0] * m[3,2] -
                m[0,3] * m[1,0] * m[2,1] * m[3,2] + m[0,0] * m[1,3] * m[2,1] * m[3,2] +
                m[0,1] * m[1,0] * m[2,3] * m[3,2] - m[0,0] * m[1,1] * m[2,3] * m[3,2] -
                m[0,2] * m[1,1] * m[2,0] * m[3,3] + m[0,1] * m[1,2] * m[2,0] * m[3,3] +
                m[0,2] * m[1,0] * m[2,1] * m[3,3] - m[0,0] * m[1,2] * m[2,1] * m[3,3] -
                m[0,1] * m[1,0] * m[2,2] * m[3,3] + m[0,0] * m[1,1] * m[2,2] * m[3,3];
        }

        void Start() {
            UnityEngine.Debug.Log ( "Testing normals" );
            PreTest();
            RunTest();
            PostTest();
            Mesh m = mf.sharedMesh;
            int[] tris = m.triangles;
            for ( int i = 0; i< tris.Length; i+= 3 ) {
                int t = tris[i];
                tris[i] = tris[i+1];
                tris[i+1] = t;
            }
            m.triangles = tris;
            UnityEngine.Debug.Log ( "Flipped mesh order" );

        }

        public override bool PostTest() {
            return true;
        }
    }
}
