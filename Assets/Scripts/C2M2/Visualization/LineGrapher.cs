using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using C2M2.Utils;

namespace C2M2.Visualization
{
    public class LineGrapher : MonoBehaviour
    {
        public RectTransform backgroundPanel = null;
        public LineRenderer pointsRenderer;
        public GraphCursor cursor = null;
        public GraphPointer pointerLines = null;

        private int numSamples = 20;
        public int NumSamples
        {
            get
            {
                return numSamples;
            }
            set
            {
                if(numSamples <= 0)
                {
                    Debug.LogError("Cannot have fewer than 1 point on graph");
                    return;
                }

                numSamples = value;
                positions = new List<Vector3>(numSamples);
                posArr = new Vector3[NumSamples];

                for (int i = 0; i < NumSamples; i++)
                {
                    positions.Add(Vector3.zero);
                }

                pointsRenderer.positionCount = NumSamples;

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

        private float xMin = float.PositiveInfinity;
        public float XMin
        {
            get { return xMin; }
            set
            {
                xMin = value;
                XMinStr = xMin.ToString("F2");
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
                XMaxStr = xMax.ToString("F2");
            }
        }
        public TextMeshProUGUI xMaxLabel;
        private string XMaxStr { set { if (xMaxLabel != null) xMaxLabel.text = value; } }

        private float yMin = float.PositiveInfinity;
        public float YMin
        {
            get { return yMin; }
            set
            {
                yMin = value;
                YMinStr = yMin.ToString();
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
                YMaxStr = yMax.ToString();
            }
        }
        public TextMeshProUGUI yMaxLabel;
        private string YMaxStr { set { if (yMaxLabel != null) yMaxLabel.text = value; } }
        #endregion

        // Keeping one list of Vector3's saves list operation time over storing a separate x and y list
        public List<Vector3> positions;
        // Used to convert list to array for LineRenderer
        private Vector3[] posArr;

        private void Awake()
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