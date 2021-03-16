using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using C2M2.Utils;

namespace C2M2.Visualization
{
    public class LineGrapher : MonoBehaviour
    {
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

        public RectTransform backgroundPanel = null;

        public float graphWidth = 500f;
        public LineRenderer pointsRenderer;
        public int numSamples = 20;
        public List<float> xValues;
        public List<float> yValues;

        private void Start()
        {
            if (pointsRenderer == null)
            {
                Debug.LogError("No renderer given for plot points!");
                Destroy(this);
            }
            xValues = new List<float>(numSamples);
            yValues = new List<float>(numSamples);

            for (int i = 0; i < numSamples; i++)
            {
                xValues.Add(0);
                yValues.Add(0);
            }

            pointsRenderer.positionCount = numSamples;

            XMin = xValues[0];
            XMax = xValues[xValues.Count - 1];
        }

        public float ChangeMax(float newMax) => YMax = newMax;
        public void ChangeMin(float newMin) => YMin = newMin;

        public void AddValue(float y, float x = -1f)
        {
            if(x == -1f)
            {
                x = numSamples;
                xValues[xValues.Count - 2] -= (graphWidth / (numSamples - 1));
            }

            xValues.RemoveAt(0);
            xValues.Add(x);

            XMin = xValues[0];
            XMax = xValues[xValues.Count - 1];

            yValues.RemoveAt(0);
            yValues.Add(y);

            if (y < YMin) YMin = y;
            else if (y > YMax) YMax = y;

            UpdateRender();
        }
        private void UpdateRender()
        {
            if (pointsRenderer == null)
            {
                Debug.LogError("No LineRenderer found on " + pointsRenderer.name + "!");
                return;
            }

            // TODO: Can speed this up by checking new values to resolve rolling max/min
            // Rescale from (Min, Max) to (0, graphWidth)
            List<float> y = yValues.Rescale(0, graphWidth, YMin, YMax);
            List<float> x = xValues.Rescale(0, graphWidth, XMin, XMax);
            Vector3[] points = new Vector3[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                points[i] = new Vector3(x[i], y[i], 0f);
            }

            Debug.Log("Updating positions on " + name);
            pointsRenderer.SetPositions(points);
            
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