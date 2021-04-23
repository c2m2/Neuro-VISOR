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
        public Color outlineColor;
        public Color labelColor;

        public RectTransform infoPanel = null;
        public RectTransform infoPanelButton = null;
        private LineRenderer outline;
        public LineRenderer Outline
        {
            get
            {
                return outline;
            }
            set
            {
                outline = value;
                outline.startColor = outlineColor;
                outline.endColor = outlineColor;
            }
        }


        private int maxSamples = 100;
        public int MaxSamples
        {
            get
            {
                return maxSamples;
            }
            set
            {
                if (maxSamples == positions.Count) return;

                if (maxSamples <= 0)
                {
                    Debug.LogError("Cannot have fewer than 1 point on graph");
                    return;
                }

                maxSamples = value;
                
                positions.Capacity = maxSamples;
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
                title.color = labelColor;
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
                yLabel.color = labelColor;
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
                xLabel.color = labelColor;
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
                if (value > 0 && value <= maxXPrec)
                {
                    xPrecision = value;
                    xPrecFormat = "F" + xPrecision;

                    if (cursor != null)
                    {
                        cursor.UpdateFormatString(XPrecision, yPrecision);
                    }
                }
            }
        }
        private string xPrecFormat = "F3";
        private int maxXPrec = 6;

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
                if (value > 0 && value <= maxYPrec)
                {
                    yPrecision = value;
                    yPrecFormat = "F" + yPrecision;

                    if (cursor != null)
                    {
                        cursor.UpdateFormatString(XPrecision, yPrecision);
                    }
                }
            }
        }
        private string yPrecFormat = "F3";
        private int maxYPrec = 6;

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

        public RectTransform rt { get; private set; }

        private void Awake()
        {
            rt = (RectTransform)transform;

            NullChecks();

            InitInfoPanel();

            InitOutline();

            InitInfoPanelButton();
        
            MaxSamples = maxSamples;

            void InitInfoPanel()
            {
                float buttonWidth = graphWidth / 10;
                float margin = buttonWidth / 2.5f;
                closeButton.sizeDelta = new Vector2(buttonWidth - margin, buttonWidth - margin);
                closeButton.anchoredPosition = new Vector3(-(graphWidth / 2) - (buttonWidth / 2), (graphWidth / 2) + (buttonWidth / 2));
                closeButton.GetComponent<BoxCollider>().size = new Vector3(buttonWidth, buttonWidth);

                infoPanel.sizeDelta = new Vector2(graphWidth / 2, rt.sizeDelta.y);

                Vector3 lwh = infoPanel.sizeDelta;

                infoPanel.anchoredPosition = new Vector2((rt.sizeDelta.x + lwh.x) / 2, 0f);

                Image backgroundImg = infoPanel.GetComponentInChildren<Image>();
                backgroundImg.rectTransform.sizeDelta = lwh;

                var infoOutline = infoPanel.GetComponent<LineRenderer>();
                if(infoOutline != null)
                {
                    infoOutline.startColor = outlineColor;
                    infoOutline.endColor = outlineColor;
                }
                foreach(var text in infoOutline.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.color = labelColor;
                }
            }
            void InitInfoPanelButton()
            {
                float buttonWidth = graphWidth / 10;
                float margin = buttonWidth / 2.5f;
                infoPanelButton.sizeDelta = new Vector2(buttonWidth - margin, buttonWidth - margin);
                infoPanelButton.anchoredPosition = new Vector3((graphWidth / 2) + (buttonWidth / 2), (graphWidth / 2) + (buttonWidth / 2));
                foreach (BoxCollider receiver in infoPanel.GetComponentsInChildren<BoxCollider>())
                {
                    receiver.size = new Vector3(buttonWidth, buttonWidth);
                }
            }
            void InitOutline()
            {
                Outline = infoPanel.GetComponent<LineRenderer>();
                Vector3 lwh = infoPanel.sizeDelta;
                Outline.positionCount = 4;
                Outline.SetPositions(new Vector3[] {
                new Vector3(-lwh.x / 2, -lwh.y / 2),
                new Vector3(-lwh.x / 2, lwh.y / 2),
                new Vector3(lwh.x / 2, lwh.y / 2),
                new Vector3(lwh.x / 2, -lwh.y / 2) });
                Outline.loop = true;
                outline.startColor = outlineColor;
                outline.endColor = outlineColor;
            }
            void NullChecks()
            {
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
                        Debug.LogError("No cursor found for LineGrapher.");
                        Destroy(this);
                    }
                    cursor.cursorLabel.color = labelColor;
                }
                if (pointerLines == null)
                {
                    pointerLines = GetComponentInChildren<GraphPointer>();
                    if (pointerLines == null)
                    {
                        Debug.LogError("No pointer lines found for LineGrapher.");
                        Destroy(this);
                    }
                }
                if (closeButton == null)
                {
                    Debug.LogError("No Close Button found.");
                    Destroy(this);
                }
                if (infoPanel == null)
                {
                    Debug.LogError("No info panel found.");
                    Destroy(this);
                }
                if (infoPanelButton == null)
                {
                    Debug.LogError("No info panel open/close button found.");
                    Destroy(this);
                }
            }
        }

        public virtual void AddValue(float x, float y)
        {
            if (positions.Count > MaxSamples)
            {
                // RemoveAt(0) is an O(n) operation and should be removed if possible
                positions.RemoveAt(0);
            }

            positions.Add(new Vector3(x, y));

            // Update max and min
            XMin = positions[0].x;
            XMax = positions[positions.Count - 1].x;

            if (y < YMin) YMin = y;
            else if(y > YMax) YMax = y;

            UpdateScale();

            pointsRenderer.positionCount = positions.Count;

            // Our local ToArray function should be faster than Enumerable's default function
            pointsRenderer.SetPositions(Array.ToArray(positions));
        }

        private void UpdateScale()
        {
            if (XMax == XMin || YMax == YMin) return;

            /// Instead of rescaling the entire array of values each frame, 
            /// this scales the line renderer's transform, 
            /// saving tons of performance over rescaling each point individually
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