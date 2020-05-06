using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class james_mesh_color : MonoBehaviour
{
    // Start is called before the first frame update
    MeshFilter theCylinderMesh;

    void Start()
    {
        theCylinderMesh = GetComponent<MeshFilter>();
        Mesh cylMesh = theCylinderMesh.mesh;
        Color32[] listOfColors;

        if (cylMesh.colors32.Length == 0)
        {
            listOfColors = new Color32[cylMesh.vertexCount];
        }
        else
        {
            listOfColors = cylMesh.colors32;
        }

        //Now color half of the vertices red the other half blue
        for( int i = 0; i<listOfColors.Length; i++)
        {

            if( i < Mathf.Ceil(listOfColors.Length/2))
            {
                listOfColors[i] = Color.red;
            }
            else
            {
                listOfColors[i] = Color.blue;
            }
        }

        //reassign the colors
        cylMesh.colors32 = listOfColors;
    }

    // Update is called once per frame
    //void Update()
    // {
        
    // }
}
