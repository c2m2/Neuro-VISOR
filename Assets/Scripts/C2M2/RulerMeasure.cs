using C2M2.Simulation;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(List<Canvas>))]
[RequireComponent(typeof(List<float>))]
public class RulerMeasure : MonoBehaviour
{
    public MeshSimulation sim = null;
    public List<Canvas> measurementDisplays;
    public List<int> numbers;
    private List<MarkedDisplay> markedDisplays = new List<MarkedDisplay>();
    private readonly int markerCount = 100; //Maximum number of markers
    private float initialRulerLength;
    private float scaledRulerLength;
    private float markerSpacingPercent; //minimum spacing between each marker and beginning and end of ruler in percent of rulers length

    // Start is called before the first frame update
    void Start()
    {
        numbers.Sort();
        initialRulerLength = transform.lossyScale.z;
        CreateMarkers();
    }

    // Update is called once per frame
    void Update()
    {
        float markerSpacing = 0.05f; //minimum spacing between each marker and beginning and end of ruler
        markerSpacingPercent = markerSpacing * initialRulerLength / transform.lossyScale.z;
        if (sim != null)
        {
            float rulerLength = transform.lossyScale.z / sim.transform.localScale.z;
            float firstMarkerLength = markerSpacingPercent * rulerLength;

            int magnitude = GetMagnitude(firstMarkerLength); //number of zeros after first digit
            string siPrefixGroupText = GetUnit(magnitude);

            int siPrefixGroup = (int)Math.Floor(magnitude / 3.0);
            // scaledFirstMarkerLength is a scaled version of firstMarkerLength so it is between 1 and 1000
            float scaledFirstMarkerLength = (float)(firstMarkerLength / Math.Pow(10, siPrefixGroup * 3));
            scaledRulerLength = (float)(rulerLength / Math.Pow(10, siPrefixGroup * 3));
            UpdateMarkers(scaledFirstMarkerLength);
            //TODO Put Units somewhere
        }
    }

    private int GetMagnitude(float length)
    {
        double lengthLog10 = Math.Log10(length);
        return Convert.ToInt32(Math.Floor(lengthLog10));
    }

    private string GetUnit(int magnitude)
    {
        if (magnitude < -3)
        {
            // takes the magnitude and puts it in terms of nm by adding three. Then divides by 3 to get unit group and rounds down.
            int eTerm = (magnitude + 3) / 3;
            return " e" + 3 * eTerm + " nm";
        }
        else if (magnitude < 0) return " nm";
        else if (magnitude < 3) return " μm";
        else if (magnitude < 6) return " mm";
        else if (magnitude < 9) return " m";
        else if (magnitude <= 12) return " km";
        else if (magnitude > 12)
        {
            // takes the magnitude and puts it in terms of km by subtracting twelve. Then divides by 3 to get unit group and rounds down.
            int eTerm = (magnitude - 12) / 3;
            return " e" + 3 * eTerm + " km";
        }
        else
        {
            Debug.LogError("Invalid length inputted");
            return null;
        }
    }

    private void CreateMarkers()
    {
        foreach (Canvas measurementDisplay in measurementDisplays)
        {
            List<TextMeshProUGUI> markers = new List<TextMeshProUGUI>();
            for (int i = 0; i < markerCount; i++)
            {
                GameObject gObj = new GameObject();
                gObj.transform.SetParent(measurementDisplay.transform);
                TextMeshProUGUI markerText = gObj.AddComponent<TextMeshProUGUI>();
                markerText.alignment = TextAlignmentOptions.Center;
                markerText.rectTransform.localPosition = new Vector3(0, 0, 0);
                markerText.fontSize = 0.04f;
                markerText.color = Color.black;
                gObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
                gObj.transform.localScale = Vector3.one;
                markers.Add(markerText);
            }
            MarkedDisplay markedDisplay = new MarkedDisplay(measurementDisplay, markers);
            markedDisplays.Add(markedDisplay);
        }
    }

    private void UpdateMarkers(float scaledFirstMarkerLength)
    {
        int interval = 0;
        int currentNumber = 0;
        while(interval == 0)
        {
            if (currentNumber >= numbers.Count)
            {
                Debug.LogError("No Ruler Marker Large Enough");
                interval = 1000;
            }
            else if (numbers[currentNumber] > scaledFirstMarkerLength)
                {
                    interval = numbers[currentNumber];
                }
            currentNumber++;
        }

        foreach (MarkedDisplay markedDisplay in markedDisplays)
        {
            int markerNumber = 0;
            for (float i = 0; i <= scaledRulerLength; i+=interval)
            {
                float rulerPoint = (i/scaledRulerLength) - 0.5f;
            
                if (markedDisplay.markers.Count <= markerNumber)
                {
                    //Occurs when there are more markers then the preset limit
                    break;
                }
                markedDisplay.markers[markerNumber].text = "‒ " + i + " ‒";
                markedDisplay.markers[markerNumber].rectTransform.localPosition = new Vector3(rulerPoint, 0, 0);
                markedDisplay.markers[markerNumber].transform.localScale = new Vector3(markedDisplay.markers[markerNumber].transform.localScale.x, initialRulerLength / transform.lossyScale.z, markedDisplay.markers[markerNumber].transform.localScale.z);

                markedDisplay.markers[markerNumber].gameObject.SetActive(true);
                markerNumber++;
            }
            while (markerNumber < markedDisplay.markers.Count)
            {
                markedDisplay.markers[markerNumber].gameObject.SetActive(false);
                markerNumber++;
            }
            
        }
    }

    private class MarkedDisplay
    {
        public Canvas display;
        public List<TextMeshProUGUI> markers;

        public MarkedDisplay(Canvas display, List<TextMeshProUGUI> markers)
        {
            this.display = display;
            this.markers = markers;
        }
    }

}
