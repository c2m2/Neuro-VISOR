using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Visualization
{
    public class GraphPointer : MonoBehaviour
    {
        /// <summary>
        /// Anchors on the graph to draw lines from. These transforms MUST possess lineRenderers
        /// </summary>
        public Transform[] anchors = null;

        /// <summary>
        /// Position to render the anchor line to.
        /// </summary>
        [Tooltip("Position to render the anchor line to.")]
        public Vector3 targetPos = Vector3.zero;

        /// <summary>
        /// If true, only renders shortest anchor line
        /// </summary>
        [Tooltip("If true, only renders shortest anchor")]
        public bool onlyRenderShortestAnchor = false;

        private LineRenderer[] lineRends = null;
        public bool UseWorldSpace
        {
            set
            {
                if(lineRends != null && lineRends.Length > 0)
                {
                    foreach(LineRenderer line in lineRends)
                    {
                        line.useWorldSpace = value;
                    }
                }
                else
                {
                    Debug.LogError("Couldn't access lines array.");
                }
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            if (anchors == null || anchors.Length == 0)
            {
                Debug.LogError("Null anchors given to GraphPointer.");
                Destroy(this);
            }
            
            // Ensure each anchor has a LineRenderer
            for (int i = 0; i < anchors.Length; i++)
            {
                if(anchors[i].GetComponent<LineRenderer>() == null)
                {
                    anchors[i].gameObject.AddComponent<LineRenderer>();
                }
            }

            // Find the line renderer on each anchor point
            lineRends = new LineRenderer[anchors.Length];
            for (int i = 0; i < anchors.Length; i++)
            {
                lineRends[i] = anchors[i].GetComponent<LineRenderer>();
                if (lineRends[i] == null)
                {
                    Debug.LogError("Invalid anchor given!");
                    Destroy(this);
                }
                lineRends[i].positionCount = 2;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Resolve all the lines for each lineRenderer
            Vector3[][] lines = new Vector3[anchors.Length][];
            for(int i = 0; i < anchors.Length; i++)
            {
                lines[i] = new Vector3[] { anchors[i].position, targetPos };
            }

            if (onlyRenderShortestAnchor)
                RenderShortestAnchor();
            else
                RenderAllAnchors();

            void RenderAllAnchors()
            {
                // Focus each line renderer to the target position
                for (int i = 0; i < lineRends.Length; i++)
                {
                    lineRends[i].SetPositions(lines[i]);
                }
            }
            void RenderShortestAnchor()
            {
                float shortestMag = float.PositiveInfinity;
                int shortestInd = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    float magnitude = Vector3.Distance(lines[i][0], lines[i][1]);
                    if (magnitude < shortestMag)
                    {
                        shortestMag = magnitude;
                        shortestInd = i;
                    }
                }

                for (int i = 0; i < lineRends.Length; i++)
                {
                    lineRends[i].enabled = false;
                }
                lineRends[shortestInd].enabled = true;
                lineRends[shortestInd].SetPositions(lines[shortestInd]);
            }
        }
    }
}