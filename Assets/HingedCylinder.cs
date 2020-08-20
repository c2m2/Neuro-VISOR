using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Utils;
namespace C2M2.NeuronalDynamics.Interaction {
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class HingedCylinder : MonoBehaviour
    {
        MeshFilter mf;
        public float radius = 0.2f;
        public int resolution = 8;
        public float length = 1f;

        public Mesh mesh { get; private set; }
        public int[] triangles { get { return mesh.triangles; } }
        public Vector3[] vertices { get { return mesh.vertices; } }
        public Vector3[] CenterRow { get { return vertices.Subset(resolution); } }
        public Vector3[] TopRow { get { return vertices.Subset(resolution * 2, resolution); } }
        public Vector3[] BottomRow { get { return vertices.Subset(resolution * 3, 2 * resolution); } }
        
        private void Awake()
        {
            mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = BuildMesh();

        }

        private Mesh BuildMesh()
        {
            List<Triangle> tris = new List<Triangle>();
            Vector3[] vertices = new Vector3[3 * resolution];

            float angleStep = 360 / resolution;
            float halfLength = length / 2;

            for(int i = 0; i < resolution; i++)
            {
                float curAngle = angleStep * i;

                vertices[i] = new Vector3(radius * Mathf.Cos(curAngle), 0f, radius * Mathf.Sin(curAngle));
                vertices[i + resolution] = new Vector3(vertices[i].x, halfLength, vertices[i].z);
                vertices[i + 2*resolution] = new Vector3(vertices[i].x, -halfLength, vertices[i].z);

                tris.Add(new Triangle(i, i + resolution, i + 1));
                tris.Add(new Triangle(i, i + 2 * resolution + 1, i+2*resolution));
                tris.Add(new Triangle(i + 1, i + resolution, i + resolution + 1));
                tris.Add(new Triangle(i, i + 1, i + 2 * resolution + 1));
            }

            Triangle[] triangles = tris.ToArray();

            int j = resolution - 1;
            triangles[triangles.Length - 4] = new Triangle(j, j + resolution, 0);
            triangles[triangles.Length - 3] = new Triangle(j, 2 * resolution, j + 2 * resolution);
            triangles[triangles.Length - 2] = new Triangle(0, j + resolution, resolution);
            triangles[triangles.Length - 1] = new Triangle(j, 0, 2*resolution);

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = ToUnityArray(triangles);
            mesh.name = "CylinderRes" + resolution;

            return mesh;    
        }

        public int[] ToUnityArray(Triangle[] tris)
        {
            int[] unityArr = new int[tris.Length * 3];
            int j = 0;
            for (int i = 0; i < tris.Length; i++)
            {
                unityArr[j] = tris[i].v1;
                unityArr[j + 1] = tris[i].v2;
                unityArr[j + 2] = tris[i].v3;
                j += 3;
            }

            return unityArr;
        }
    }

    public struct Triangle
    {
        public int v1 { get; private set; }
        public int v2 { get; private set; }
        public int v3 { get; private set; }
        public Triangle(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }
   
}