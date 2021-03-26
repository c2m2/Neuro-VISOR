using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace C2M2.Visualization {
    public class LineGraphEditor : MonoBehaviour
    {
        public LineGrapher lineGraph = null;
        public int NumSamples
        {
            get
            {
                return lineGraph.MaxSamples;
            }
            set
            {
                lineGraph.MaxSamples = value;
                numSampleReading.text = lineGraph.MaxSamples.ToString();
            }
        }
        public TextMeshProUGUI numSampleReading = null;
        public TextMeshProUGUI xPrecisionReading = null;
        public TextMeshProUGUI yPrecisionReading = null;

        private void Awake()
        {
            NullChecks();

            NumSamples = lineGraph.MaxSamples;
            xPrecisionReading.text = lineGraph.XPrecision.ToString();
            yPrecisionReading.text = lineGraph.YPrecision.ToString();

            void NullChecks()
            {
                if (lineGraph == null)
                {
                    Debug.LogError("No linegraph given to LineGraphEditor.");
                    Destroy(this);
                }
                if (numSampleReading == null)
                {
                    Debug.LogError("No sample reading given to LineGraphEditor.");
                    Destroy(this);
                }
                if (xPrecisionReading == null)
                {
                    Debug.LogError("No X Precision reading given to LineGraphEditor.");
                    Destroy(this);
                }
                if (yPrecisionReading == null)
                {
                    Debug.LogError("No Y Precision reading given to LineGraphEditor.");
                    Destroy(this);
                }
            }
        }

        public void XPrecisionAdd(RaycastHit hit)
        {
            lineGraph.XPrecision++;
            xPrecisionReading.text = lineGraph.XPrecision.ToString();
        }
        public void XPrecisionSub(RaycastHit hit)
        {
            lineGraph.XPrecision--;
            xPrecisionReading.text = lineGraph.XPrecision.ToString();
        }
        public void YPrecisionAdd(RaycastHit hit)
        {
            lineGraph.YPrecision++;
            yPrecisionReading.text = lineGraph.YPrecision.ToString();
        }
        public void YPrecisionSub(RaycastHit hit)
        {
            lineGraph.YPrecision--;
            yPrecisionReading.text = lineGraph.YPrecision.ToString();
        }
    }
}