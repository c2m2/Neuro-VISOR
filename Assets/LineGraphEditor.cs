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
                return lineGraph.NumSamples;
            }
            set
            {
                lineGraph.NumSamples = value;
                numSampleReading.text = lineGraph.NumSamples.ToString();
            }
        }
        public TextMeshProUGUI numSampleReading = null;

        private void Awake()
        {
            if(lineGraph == null)
            {
                Debug.LogError("No linegraph given to LineGraphEditor.");
                Destroy(this);
            }
            if(numSampleReading == null)
            {
                Debug.LogError("No sample reading given to LineGraphEditor.");
                Destroy(this);
            }

            NumSamples = lineGraph.NumSamples;
        }
    }
}