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
        public bool showLabel = true;
        public int xLabelPrecision = 2;
        public int yLabelPrecision = 2;
        private RectTransform labelBackground = null;
        private string formatString = "({0}, {1})";

        private RaycastHit lastHit;


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
                return lineGraph.NumSamples;
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

        private bool locked = false;

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

            if(cursorLabel == null && showLabel)
            {
                Debug.LogError("No text label found for cursor. Attach text or disable showLabel");
                Destroy(this);
            }
            labelBackground = (RectTransform)cursorLabel.GetComponentInChildren<Image>().transform;

            ToggleCursor(false);

            formatString = "({0:F" + xLabelPrecision + "}, {1:F" + yLabelPrecision + "})";
        }
        public void AlignCursor(RaycastHit hit)
        {
            // Make sure cursor parts are enabled
            ToggleCursor(true);

            if (locked) return;

            lastHit = hit;

            UpdateCursor(lastHit);
        }

        private void UpdateCursor(RaycastHit hit)
        {
            Vector3 scaler = new Vector3(
                GraphWidth / (XMax - XMin),
                GraphWidth / (YMax - YMin));

            // Get the cursor's position on the graph
            Vector3 truePos = GetTruePosition(hit.point);

            // Get the cursor's nearest value in the graph
            int ind = Mathf.RoundToInt((truePos.x / GraphWidth) * NumSamples);
            Vector3 labelValue = lineGraph.positions[ind];

            // Latch the cursor to a value on the graph
            Vector3 latchedPos = GetLatchedPosition(labelValue);

            // Set the cursor's position
            SetCursorPosition(latchedPos);

            // Update the label position and its displayed value
            UpdateLabel(cursor.transform.localPosition, labelValue);

            Vector3 GetTruePosition(Vector3 hitPoint)
            {
                // Scale hit position to graph space
                Vector3 localHit = lineGraph.pointsRenderer.transform.InverseTransformPoint(hitPoint);

                localHit = new Vector3(localHit.x * scaler.x, localHit.y * scaler.y);

                // Shift position and return
                return new Vector3(localHit.x + PosAdj.x - Xorigin, localHit.y + PosAdj.y - Xorigin);
            }
            Vector3 GetLatchedPosition(Vector3 pos)
            {
                // Readjust and shift for cursor positions
                return new Vector3((pos.x * scaler.x) + PosAdj.x - Xorigin,
                    (pos.y * scaler.y) + PosAdj.y - Yorigin);
            }
            void SetCursorPosition(Vector3 pos)
            {
                // Update positions of cursor parts based on hit info
                xCursor.SetPositions(new Vector3[] {
                new Vector3(0, pos.y),
                new Vector3(pos.x - cursorWidth, pos.y) });

                yCursor.SetPositions(new Vector3[] {
                new Vector3(pos.x, 0),
                new Vector3(pos.x, pos.y - cursorWidth) });

                cursor.transform.localPosition = new Vector3(pos.x - (GraphWidth / 2), pos.y - (GraphWidth / 2));
            }
            void UpdateLabel(Vector3 cursorPos, Vector3 val)
            {
                cursorLabel.text = string.Format(formatString, val.x, val.y);
                Vector3 borderSize = new Vector3(cursorLabel.textBounds.size.x * 1.25f, cursorLabel.textBounds.size.y * 1.25f);

                // If we have a background, match its size to the border size
                if (labelBackground != null) labelBackground.sizeDelta = borderSize;

                // Resolve where to place label relative to the cursor
                float shiftAmtX = cursorWidth + (borderSize.x / 2);
                float shiftAmtY = cursorWidth + (borderSize.y / 2);

                // We are in the right side of the graph, put label to the left of cursor
                if (cursorPos.x > 0) shiftAmtX = -shiftAmtX;

                // we are in the top of the graph, put label below cursor
                if (cursorPos.y > 0) shiftAmtY = -shiftAmtY;

                cursorLabel.transform.localPosition = new Vector3(cursorPos.x + shiftAmtX, cursorPos.y + shiftAmtY);
            }
        }

        public void CloseCursor(RaycastHit hit)
        {
            if (locked) return;
            ToggleCursor(false);
        }

        public void ToggleCursor(bool enabled)
        {
            xCursor.enabled = enabled;
            yCursor.enabled = enabled;
            cursor.enabled = enabled;
            if (showLabel) cursorLabel.gameObject.SetActive(enabled);
        }

        private Coroutine lockRoutine = null;
        public void ToggleCursorLock()
        {
            // Toggle lock on/off on a press
            locked = !locked;

            // If we are entering a new lock,
            if (locked)
            {
                // Lock the cursor on and start the LockedState routine
                ToggleCursor(true);
                lockRoutine = StartCoroutine(LockedState());
            }
            else
            {
                StopCoroutine(lockRoutine);
                ToggleCursor(false);
            }
        }

        // While locked, continue to update the cursor to match the last known hit position
        private IEnumerator LockedState()
        {
            while (true)
            {
                UpdateCursor(lastHit);
                yield return new WaitForFixedUpdate();
            }
        }
    }
}