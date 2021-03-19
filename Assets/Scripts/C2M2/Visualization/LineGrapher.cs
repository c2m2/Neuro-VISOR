using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using C2M2.Utils;

namespace C2M2.Visualization
{
    public class LineGrapher : MonoBehaviour
    {
        public RectTransform backgroundPanel = null;
        public LineRenderer pointsRenderer;
        public GraphCursor cursor = null;
        public GraphPointer pointerLines = null;
        public RectTransform closeButton = null;

        public RectTransform infoPanel = null;
        public RectTransform infoPanelButton = null;


        private int numSamples = 750;
        public int NumSamples
        {
            get
            {
                return numSamples;
            }
            set
            {
                if (numSamples == positions.Count) return;

                if (numSamples <= 0)
                {
                    Debug.LogError("Cannot have fewer than 1 point on graph");
                    return;
                }

                numSamples = value;

                List<Vector3> newPosL = new List<Vector3>(numSamples);

                // If we decrease the number of samples, we now have fewer graph points 
                if(numSamples < positions.Count)
                {
                    // Take the most recent samples
                    for(int i = 0; i < numSamples; i++)
                    {
                        newPosL.Add(positions[i]);
                    }
                }
                // If we increase the number of samples, we now have more graph points
                if(numSamples > positions.Count)
                {
                    // Copy the samples we have at the end, set the rest to 0
                    for(int i = 0; i < numSamples - positions.Count; i++)
                    {
                        newPosL.Add(Vector3.zero);
                    }
                    int j = 0;
                    for(int i = numSamples - positions.Count; i < numSamples; i++)
                    {
                        newPosL.Add(positions[j]);
                        j++;
                    }
                }

                pointsRenderer.positionCount = numSamples;

                positions = newPosL;
                posArr = new Vector3[numSamples];

                XMin = positions[0].x;
                XMax = positions[NumSamples - 1].x;
            }
        }

        public float graphWidth = 500f;
        public float localOriginX = 50f;
        public float localOriginY = 50f;
        public float globalLineWidth = 4f;

        #region GraphText
        public TextMeshProUGUI title;
        private string titleStr = "";
        public string TitleStr
        {
            get { return titleStr; }
            set
            {
                titleStr = value;
                title.text = titleStr;
            }
        }

        public TextMeshProUGUI yLabel;
        private string yLabelStr = "";
        public string YLabelStr
        {
            get { return yLabelStr; }
            set
            {
                yLabelStr = value;
                yLabel.text = yLabelStr;
            }
        }

        public TextMeshProUGUI xLabel;
        private string xLabelStr = "";
        public string XLabelStr
        {
            get { return xLabelStr; }
            set
            {
                xLabelStr = value;
                xLabel.text = xLabelStr;
            }
        }

        private int xPrecision = 3;
        public int XPrecision
        {
            get
            {
                return xPrecision;
            }
            set
            {
                xPrecision = value;
                xPrecFormat = "F" + xPrecision;

                if (cursor != null)
                {
                    cursor.UpdateFormatString(XPrecision, yPrecision);
                }
            }
        }
        private string xPrecFormat = "F3";

        private float xMin = float.PositiveInfinity;
        public float XMin
        {
            get { return xMin; }
            set
            {
                xMin = value;
                XMinStr = xMin.ToString(xPrecFormat);
            }
        }
        public TextMeshProUGUI xMinLabel;
        private string XMinStr { set { if (xMinLabel != null) xMinLabel.text = value; } }

        private float xMax = float.NegativeInfinity;
        public float XMax
        {
            get { return xMax; }
            set
            {
                xMax = value;
                XMaxStr = xMax.ToString(xPrecFormat);
            }
        }
        public TextMeshProUGUI xMaxLabel;
        private string XMaxStr { set { if (xMaxLabel != null) xMaxLabel.text = value; } }

        private int yPrecision = 3;
        public int YPrecision
        {
            get
            {
                return yPrecision;
            }
            set
            {
                yPrecision = value;
                yPrecFormat = "F" + yPrecision;

                if(cursor != null)
                {
                    cursor.UpdateFormatString(XPrecision, yPrecision);
                }
            }
        }
        private string yPrecFormat = "F3";

        private float yMin = float.PositiveInfinity;
        public float YMin
        {
            get { return yMin; }
            set
            {
                yMin = value;
                YMinStr = yMin.ToString(yPrecFormat);
            }
        }
        public TextMeshProUGUI yMinLabel;
        private string YMinStr { set { if (yMinLabel != null) yMinLabel.text = value; } }

        private float yMax = float.NegativeInfinity;
        public float YMax
        {
            get { return yMax; }
            set
            {
                yMax = value;
                YMaxStr = yMax.ToString(yPrecFormat);
            }
        }
        public TextMeshProUGUI yMaxLabel;
        private string YMaxStr { set { if (yMaxLabel != null) yMaxLabel.text = value; } }
        #endregion

        // Keeping one list of Vector3's saves list operation time over storing a separate x and y list
        public List<Vector3> positions;
        // Used to convert list to array for LineRenderer
        private Vector3[] posArr;

        public RectTransform rt { get; private set; }

        private void Awake()
        {
            rt = (RectTransform)transform;
            if (pointsRenderer == null)
            {
                Debug.LogError("No renderer given for plot points!");
                Destroy(this);
            }
            if (cursor == null)
            {
                cursor = GetComponentInChildren<GraphCursor>();
                if (cursor == null)
                {
                    Debug.LogWarning("No cursor found for LineGrapher.");
                }
            }

            if (pointerLines == null)
            {
                pointerLines = GetComponentInChildren<GraphPointer>();
                if (pointerLines == null)
                {
                    Debug.LogWarning("No pointer lines found for LineGrapher.");
                }
            }

            if(closeButton != null)
            {
                float buttonWidth = graphWidth / 10;
                float margin = buttonWidth / 2.5f;
                closeButton.sizeDelta = new Vector2(buttonWidth - margin, buttonWidth - margin);
                closeButton.anchoredPosition = new Vector3(-(graphWidth / 2) - (buttonWidth / 2), (graphWidth / 2) + (buttonWidth / 2));
                closeButton.GetComponent<BoxCollider>().size = new Vector3(buttonWidth, buttonWidth);
            }

            if(infoPanel != null)
            {
                infoPanel.sizeDelta = new Vector2(graphWidth / 2, rt.sizeDelta.y);

                Vector3 lwh = infoPanel.sizeDelta;

                infoPanel.anchoredPosition = new Vector2((rt.sizeDelta.x +lwh.x) / 2, 0f);

                Image backgroundImg = infoPanel.GetComponentInChildren<Image>();
                backgroundImg.rectTransform.sizeDelta = lwh;

                LineRenderer lr = infoPanel.GetComponent<LineRenderer>();
                
                lr.positionCount = 4;
                lr.SetPositions(new Vector3[] {
                    new Vector3(-lwh.x / 2, -lwh.y / 2),
                    new Vector3(-lwh.x / 2, lwh.y / 2),
                    new Vector3(lwh.x / 2, lwh.y / 2),
                    new Vector3(lwh.x / 2, -lwh.y / 2) } );
                lr.loop = true;
            }

            if(infoPanelButton != null)
            {
                float buttonWidth = graphWidth / 10;
                float margin = buttonWidth / 2.5f;
                infoPanelButton.sizeDelta = new Vector2(buttonWidth - margin, buttonWidth - margin);
                infoPanelButton.anchoredPosition = new Vector3((graphWidth / 2) + (buttonWidth / 2), (graphWidth / 2) + (buttonWidth / 2));
                foreach(BoxCollider receiver in infoPanel.GetComponentsInChildren<BoxCollider>())
                {
                    receiver.size = new Vector3(buttonWidth, buttonWidth);
                }
            }

            NumSamples = numSamples;
        }

        public void AddValue(float x, float y)
        {
            // RemoveAt(0) is an O(n) operation and should be removed if possible
            positions.RemoveAt(0);
            positions.Add(new Vector3(x, y));

            // Update max and min
            XMin = positions[0].x;
            XMax = positions[NumSamples - 1].x;
            if (y < YMin) YMin = y;
            else if(y > YMax) YMax = y;

            UpdateScale();

            // By using posArr, we only need to make a new Vector3 once in AddValue,
            // instead of creating numSamples new Vector3 structs here
            for (int i = 0; i < NumSamples; i++)
            {
                posArr[i] = positions[i];
            }

            pointsRenderer.SetPositions(posArr);
        }

        private void UpdateScale()
        {
            // Instead of rescaling the entire array of values each frame, 
            // this scales the line renderer's transform, saving tons of performance
            float xScaler = graphWidth / (XMax - XMin);
            float yScaler = graphWidth / (YMax - YMin);
            float xOrigin = localOriginX - (XMin * xScaler);
            float yOrigin = localOriginY - (YMin * yScaler);

            pointsRenderer.transform.localScale = new Vector3(xScaler, yScaler, 1f);
            pointsRenderer.transform.localPosition = new Vector3(xOrigin, yOrigin, 0f);

            float lineWidth = globalLineWidth / Math.Max(xScaler, yScaler);
            pointsRenderer.startWidth = lineWidth;      
        }

        public void SetLabels(string title = "", string xLabel = "", string yLabel = "")
        {
            if (title != "") TitleStr = title;
            if (xLabel != "") XLabelStr = xLabel;
            if (yLabel != "") YLabelStr = yLabel;
        }

        public void DestroyPlot()
        {
            Destroy(gameObject);
        }
    }
}