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
        public Color cellBackgroundCol;
        public Color highlightCol;

        private void Awake()
        {
            NullChecks();

           // xPrecisionReading.text = lineGraph.XPrecision.ToString();
           // yPrecisionReading.text = lineGraph.YPrecision.ToString();

            void NullChecks()
            {
                if (lineGraph == null)
                {
                    lineGraph = GetComponentInParent<LineGrapher>();
                    if (lineGraph == null)
                    {
                        Debug.LogError("No linegraph given to LineGraphEditor.");
                        Destroy(this);
                    }
                }
                if (numSampleReading == null)
                {
                    Debug.LogError("No sample reading given to LineGraphEditor.");
                    Destroy(this);
                }
                /*
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
                */
            }
        }

        private void Start()
        {
            NumSamples = lineGraph.MaxSamples;
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

        // Returns a value between [-2, 2] depending on how thumbsticks are held, and how far they are held.
        private float ThumbstickState
        {
            get 
            {
                if (GameManager.instance.VRActive)
                    return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y + OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
                else if (Input.GetKey(KeyCode.UpArrow)) return 1f;
                else if (Input.GetKey(KeyCode.DownArrow)) return -1f;
                else return 0;
            }
        }
        public void ShiftNumSamples()
        {
            int shiftAmt = Mathf.RoundToInt(10f * ThumbstickState);

            if(NumSamples + shiftAmt > 1 && NumSamples + shiftAmt < 2000)
            {
                NumSamples += shiftAmt;
            }
        }

        public void DefaultCol(Image img) => img.color = cellBackgroundCol;
        public void HighlightCol(Image img) => img.color = highlightCol;
    }
}