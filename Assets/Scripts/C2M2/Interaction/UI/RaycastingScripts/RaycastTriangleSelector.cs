using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Color a mesh's triangles as you raycast to it
/// </summary>
public class RaycastTriangleSelector : MonoBehaviour
{
    #region PUBLIC_MEMBERS
        /// <summary> Color when hit </summary>
        [Tooltip("Color when hit")]
        public Color hitColor;
    #endregion
    #region PRIVATE_MEMBERS
        /// <summary> Mesh filter for the object </summary>
        private MeshFilter meshf;
        /// <summary> Mesh of the object </summary>
        private Mesh mesh;
        /// <summary> Color array of the object </summary>
        private Color32[] colors32;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        meshf = GetComponent<MeshFilter>();
        mesh = meshf.mesh;
        colors32 = new Color32[mesh.vertices.Length];
        mesh.colors32.CopyTo(this.colors32, 0);
        //mesh.colors32 = this.colors32;

        //MeshTriangleNeighbors.GetNeighbors(mesh);
    }

    public void HitTriangle(RaycastHit hit)
    {
        Debug.Log("HitTriangle Called");
        int triangleIndex = hit.triangleIndex;
        Debug.Log("tirangleIndex: " + triangleIndex);
        triangleIndex *= 3;
        Debug.Log("tirangleIndex * 3: " + triangleIndex);
        //Debug.L
        Debug.Log("colors32[" + mesh.triangles[triangleIndex] + "]" + colors32[mesh.triangles[triangleIndex]]);
        this.colors32[mesh.triangles[triangleIndex]] = hitColor;
        this.colors32[mesh.triangles[triangleIndex + 1]] = hitColor;
        this.colors32[mesh.triangles[triangleIndex + 2]] = hitColor;
 
        mesh.colors32 = this.colors32;
    }

    //Interpolate raycast triangle and worldspace hit point to find nearest hit vertex and color it
    public void HitVertex(RaycastHit hit)
    {
        int triangleIndex = hit.triangleIndex * 3;
        int vertexIndex = mesh.triangles[triangleIndex];

        //Store local hit point & each vertex position
        Vector3[] v = { transform.InverseTransformPoint(hit.point), mesh.vertices[vertexIndex], mesh.vertices[vertexIndex + 1], mesh.vertices[vertexIndex + 2] };

        float[] dist = {-1, Vector3.Distance(v[0], v[1]), Vector3.Distance(v[0], v[2]), Vector3.Distance(v[0], v[3]) };

        //Find the shortest distance between the 3 vertices and the hit pont
        dist[0] = Mathf.Min(dist[1], dist[2]);
        dist[0] = Mathf.Min(dist[0], dist[3]);

        mesh.colors32 = this.colors32;

    }
}
