using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace C2M2.Visualization
{
    public class LineGraphCursor : MonoBehaviour
    {
        public LineGrapher lineGraph = null;
        public float cursorWidth = 15;
        public LineRenderer xCursor = null;
        public LineRenderer yCursor = null;
        public Image cursor = null;
        public TextMeshProUGUI cursorLabel = null;
        public bool showText = true;

        private float XMin
        {
            get
            {
                return lineGraph.XMin;
            }
        }
        private float XMax
        {
            get
            {
                return lineGraph.XMax;
            }
        }
        private float YMin
        {
            get
            {
                return lineGraph.YMin;
            }
        }
        private float YMax
        {
            get
            {
                return lineGraph.YMax;
            }
        }
        private float GraphWidth
        {
            get
            {
                return lineGraph.graphWidth;
            }
        }
        private float NumSamples
        {
            get
            {
                return lineGraph.numSamples;
            }
        }
        private float Xorigin
        {
            get
            {
                return lineGraph.localOriginX;
            }
        }
        private float Yorigin
        {
            get
            {
                return lineGraph.localOriginY;
            }
        }
        private Vector3 PosAdj
        {
            get
            {
                return lineGraph.pointsRenderer.transform.localPosition;
            }
        }

        private void Awake()
        {
            if(lineGraph == null)
            {
                lineGraph = GetComponentInParent<LineGrapher>();
                if (lineGraph == null)
                {
                    Debug.LogError("LineGraphCursor not attached to LineGrapher");
                    Destroy(this);
                }
            }
            if (xCursor == null || yCursor == null || cursor == null)
            {
                Debug.LogError("No cursor(s) missing.");
                Destroy(this);
            }

            if(cursorLabel == null && showText)
            {
                Debug.LogError("No text label found for cursor. Attach text or disable showLabel");
                Destroy(this);
            }
        }
        public void AlignCursor(RaycastHit hit)
        {
            xCursor.enabled = true;
            yCursor.enabled = true;
            cursor.enabled = true;

            Vector3 localHit = lineGraph.pointsRenderer.transform.InverseTransformPoint(hit.point);

            Vector3 scaler = new Vector3(
                GraphWidth / (XMax - XMin),
                GraphWidth / (YMax - YMin));

            localHit = new Vector3(localHit.x * scaler.x, localHit.y * scaler.y);
            Vector3 localHitShifted = new Vector3(localHit.x + PosAdj.x - Xorigin, localHit.y + PosAdj.y - Xorigin);

            // Our cursor is x% of the way along the graph
            int xInd = Mathf.RoundToInt((localHitShifted.x / GraphWidth) * NumSamples);
            // int yInd = Mathf.RoundToInt((localHitShifted.y / GraphWidth) * NumSamples);

            Vector3 latchedPos = lineGraph.positions[xInd];

            // Readjust and shift for cursor positions
            latchedPos = new Vector3(latchedPos.x * scaler.x, latchedPos.y * scaler.y);
            latchedPos = new Vector3(latchedPos.x + PosAdj.x - Xorigin, latchedPos.y + PosAdj.y - Xorigin);

            // Update positions of cursor parts based on hit info
            xCursor.SetPositions(new Vector3[] {
                new Vector3(0, latchedPos.y),
                new Vector3(latchedPos.x - cursorWidth, latchedPos.y) });
            yCursor.SetPositions(new Vector3[] {
                new Vector3(latchedPos.x, 0),
                new Vector3(latchedPos.x, latchedPos.y - cursorWidth) });

            cursor.transform.localPosition = new Vector3(latchedPos.x - (GraphWidth / 2), latchedPos.y - (GraphWidth / 2));

            // Update cursor text and its position
        }

        public void CloseCursor(RaycastHit hit)
        {
            xCursor.enabled = false;
            yCursor.enabled = false;
            cursor.enabled = false;
        }
    }
}