using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphPointer : MonoBehaviour
{
    public Transform[] anchors = null;
    public Vector3 targetPos = Vector3.zero;
    private LineRenderer[] lines = null;

    // Start is called before the first frame update
    void Start()
    {
        if (anchors == null || anchors.Length == 0)
        {
            // If no anchors are given, look for line renderers on child objects
            lines = GetComponentsInChildren<LineRenderer>();
            if (lines == null || lines.Length == 0)
            {
                Debug.LogError("Missing pointer anchor!");
                Destroy(this);
            }
        }

        // Find the line renderer on each anchor point
        lines = new LineRenderer[anchors.Length];
        for(int i = 0; i < anchors.Length; i++)
        {
            lines[i] = anchors[i].GetComponent<LineRenderer>();
            if(lines[i] == null)
            {
                Debug.LogError("Invalid anchor given!");
                Destroy(this);
            }
            lines[i].positionCount = 2;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Focus each line renderer to the target position
        for(int i = 0; i < lines.Length; i++)
        {
            lines[i].SetPositions(new Vector3[] { anchors[i].position, targetPos });
        }
    }
}
