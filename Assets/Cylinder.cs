using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cylinder : MonoBehaviour
{
    // number of vertices in each circle
    public int resolution = 10;
    public float radius = 0.2f;
    public float length = 1f;
    MeshFilter mf;
    private Vector3 center = Vector3.zero;

    private void Awake()
    {
        mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = BuildCylinder();
    }

    private Mesh BuildCylinder()
    {
        Vector3[] vertices = new Vector3[resolution * 3];
        Vector3[] leftVertices = new Vector3[resolution];
        Vector3[] rightVertices = new Vector3[resolution];
        List<Triangle> tris = new List<Triangle>();
        float angleStep = 360 / resolution;
        int firstHalfInd = resolution;
        int secondHalfInd = 2 * resolution;

        //Vector3[] middleVerts = new Vector3[resolution];
        float halfLength = length / 2;
        for (int i = 0; i < resolution; i++)
        {
            float angle = angleStep * i;
            float curX = center.x + radius * Mathf.Cos(angle);
            float curY = center.y + radius * Mathf.Sin(angle);
            vertices[i] = new Vector3(curX, curY, 0f);
            vertices[i + firstHalfInd] = new Vector3(curX, curY, halfLength);
            vertices[i + secondHalfInd] = new Vector3(curX, curY, -halfLength);
            Triangle triA = new Triangle(i, i+1, i + firstHalfInd);
            Triangle triB = new Triangle(i, i + firstHalfInd, i + firstHalfInd + 1);
            tris.Add(triA);
            tris.Add(triB);
            Triangle triC = new Triangle(i, i + 1, i + secondHalfInd);
            Triangle triD = new Triangle(i, i + secondHalfInd, i + secondHalfInd + 1);
            tris.Add(triA);
            tris.Add(triB);
        }
        Triangle[] triArr = tris.ToArray();
        triArr[triArr.Length-4].v2 = 0;
        triArr[triArr.Length - 3].v3 = firstHalfInd;
        triArr[triArr.Length - 2].v2 = 0;
        triArr[triArr.Length - 1].v3 = secondHalfInd;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = ToUnityTris(triArr);
        return mesh;
    }
    private struct Triangle
    {
        public int v1, v2, v3;
        public Triangle(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }


    }
    private int[] ToUnityTris(Triangle[] tris)
    {
        int[] unityTris = new int[tris.Length * 3];
        int t = 0;
        for (int i = 0; i < tris.Length; i++)
        {
            unityTris[t] = tris[i].v1;
            unityTris[t + 1] = tris[i].v2;
            unityTris[t + 2] = tris[i].v3;
            t += 3;
        }
        return unityTris;

    }

}
