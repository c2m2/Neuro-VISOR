using UnityEngine;
using System.Collections;

public class UVAnimation : MonoBehaviour
{

    public int uvTileY = 5; // texture sheet columns 
    public int uvTileX = 4; // texture sheet rows

    public int fps = 30;
    private int index;

    void Update()
    {
        //calculate the index
        index = (int)(Time.time * fps);

        //repeat when when exhausting all frames
        index = index % (uvTileY * uvTileX);

        //size of each tile  
        Vector2 size = new Vector2(1.0f / uvTileY, 1.0f / uvTileX);

        //split into horizontal and vertical indexes
        var uIndex = index % uvTileX;
        var vIndex = index / uvTileX;

        //build the offset   
        //v coordinate is at the bottom of the image in openGL, so we invert it
        Vector2 offset = new Vector2(uIndex * size.x, 1.0f - size.y - vIndex * size.y);

        GetComponent<Renderer>().material.SetTextureOffset("_MainTex", offset);
        GetComponent<Renderer>().material.SetTextureScale("_MainTex", size);
    }
}
