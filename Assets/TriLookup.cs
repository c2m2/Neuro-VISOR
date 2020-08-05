using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriLookup : MonoBehaviour
{
    public Dictionary<int, List<int>> vertToTris;
    private Mesh mesh;
    public Mesh Mesh
    {
        get { return mesh; }
        set
        {
            if(value != null)
            {
                mesh = value;

                // Store local copies
                Vector3[] verts = mesh.vertices;
                int[] tris = mesh.triangles;

                // Initialize space
                vertToTris = new Dictionary<int, List<int>>(verts.Length); // One list of triangles for each vert
                for(int i = 0; i < verts.Length; i++)
                {
                    vertToTris[i] = new List<int>();
                }

                int triIndex = 0;
                for(int i = 0; i < tris.Length; i+=3)
                {
                    int v1 = tris[i];
                    int v2 = tris[i + 1];
                    int v3 = tris[i + 2];

                    vertToTris[v1].Add(triIndex);
                    vertToTris[v2].Add(triIndex);
                    vertToTris[v3].Add(triIndex);

                    triIndex++;
                }
            }
        }
    }
    private void Awake()
    {
        MeshFilter mf = GetComponent<MeshFilter>() ?? GetComponentInParent<MeshFilter>();
        if (mf == null) return;
        Mesh mesh = mf.sharedMesh ?? mf.mesh;
        if (mesh == null) return;
        this.mesh = mesh;
    }
}
