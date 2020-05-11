using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateSpheres : MonoBehaviour
{

    public GameObject spherePrefab;
    public GameObject parent;
    public int numberOfSpheres = 50;
    public float maxScale = 1f;
    public bool renderBackFaces = false;
    [Range(0, 1)]
    public float alphaValue = 0.5f;
    public bool offsetPositions = false;
    [Range(0, 1)]
    public float maxRandomPositionOffset = 0.2f;
    
    

    // Use this for initialization
    void Start()
    {
        float scaleIncrement = (maxScale / numberOfSpheres);    //If max scale is 1 and there are 50 spheres, then scale increment is 0.02 so smallest scale = 0.02, 2nd smallest = 0.04, up to 1

        //Make numberOfSpheres sphere prefabs
        for (int p = 0; p < numberOfSpheres; p++)
        {
            //Make new object instance
            GameObject holder;
            holder = Object.Instantiate(spherePrefab, parent.transform);
            holder.transform.localScale = new Vector3(((scaleIncrement * p) + scaleIncrement), ((scaleIncrement * p) + scaleIncrement), ((scaleIncrement * p) + scaleIncrement));
            if (offsetPositions)
            {
                holder.transform.position = new Vector3(Random.Range(-maxRandomPositionOffset, maxRandomPositionOffset), Random.Range(-maxRandomPositionOffset, maxRandomPositionOffset), Random.Range(-maxRandomPositionOffset, maxRandomPositionOffset)); //Add a random offset position
            }
            else
            {
                holder.transform.position = new Vector3(0f, 0f, 0f);
            }
          
            

            holder.name = "Sphere " + (p + 1);

            //Make new mesh renderer instance to control material color
            MeshRenderer meshR = new MeshRenderer();
            meshR = holder.GetComponent<MeshRenderer>();
            //Random bright color
            Color color = new Color();
            //int random = (int)(Random.Range(1, 3.999f));
            if (p % 3 == 0)
            {
                //Red with transparency
                color = new Color(1, 0, 0, alphaValue);
            }
            else if (p % 3 == 1)
            {
                //Blue with transparency
                color = new Color(0, 0, 1, alphaValue);
            }
            else if (p % 3 == 2)
            {
                //Yellow with transparency
                color = new Color(1, 0.92f, 0.016f, alphaValue);
            }

            meshR.material.color = color;

            if (renderBackFaces)
            {
                //Make inside out duplicate
                GameObject insideOut;
                insideOut = Object.Instantiate(holder, holder.transform);
                insideOut.transform.localScale = new Vector3(1f, 1f, 1f);

                insideOut.transform.position = holder.transform.position;
                insideOut.name = holder.name + " Inside";

                //Flip inside out sphere inside out
                Mesh insideOutMesh = insideOut.GetComponent<MeshFilter>().mesh;
                Vector3[] newNormals = new Vector3[insideOutMesh.normals.Length];
                for (int i = 0; i < insideOutMesh.normals.Length; i++)
                {
                    newNormals[i] = -insideOutMesh.normals[i];
                }
                insideOutMesh.normals = newNormals;

                //Flip triangles as well
                for (int m = 0; m < insideOutMesh.subMeshCount; m++)
                {
                    int[] triangles = insideOutMesh.GetTriangles(m);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        int temp = triangles[i + 0];
                        triangles[i + 0] = triangles[i + 1];
                        triangles[i + 1] = temp;
                    }
                    insideOutMesh.SetTriangles(triangles, m);
                }
            }
        }

    }

}
