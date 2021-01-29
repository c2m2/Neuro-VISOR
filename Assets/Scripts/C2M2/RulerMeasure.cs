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
    public List<float> numbers;
    private List<MarkedDisplay> markedDisplays = new List<MarkedDisplay>();
    private float relativeLength;
    private float initialRulerLength;
    private float markerSpacing = 0.05f; //minimum spacing between each marker and beginning and end of ruler
    private float markerSpacingPercent; //minimum spacing between each marker and beginning and end of ruler in percent of rulers length

    // Start is called before the first frame update
    void Start()
    {
        relativeLength = 0;
        initialRulerLength = transform.lossyScale.z;
        CreateMarkers();
    }

    // Update is called once per frame
    void Update()
    {
        markerSpacingPercent = markerSpacing * initialRulerLength / transform.lossyScale.z;
        if (sim != null)
        {
            relativeLength = (1 - markerSpacingPercent) * (transform.lossyScale.z / sim.transform.localScale.z);

            int magnitude = GetMagnitude(relativeLength); //number of zeros after first digit
            string unit = GetUnit(magnitude);

            int siPrefixGroup = (int)Math.Floor(magnitude / 3.0);
            // length is a scaled version of relativelength so it is between 1 and 1000
            float length = (float)(relativeLength / Math.Pow(10, siPrefixGroup * 3));
            UpdateMarkers(length, unit);
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
            List<Marker> markers = new List<Marker>();
            foreach (float number in numbers)
            {
                GameObject gObj = new GameObject();
                gObj.transform.SetParent(measurementDisplay.transform);
                TextMeshProUGUI markerText = gObj.AddComponent<TextMeshProUGUI>();
                markerText.alignment = TextAlignmentOptions.Center;
                markerText.rectTransform.localPosition = new Vector3(0, 0, 0);
                markerText.fontSize = 0.03f;
                markerText.color = Color.black;
                gObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
                gObj.transform.localScale = Vector3.one;
                markers.Add(new Marker(number, markerText));
            }
            MarkedDisplay markedDisplay = new MarkedDisplay(measurementDisplay, markers);
            markedDisplays.Add(markedDisplay);
        }
    }

    private void UpdateMarkers(float scaledRulerLength, string unit)
    {
        foreach (MarkedDisplay markedDisplay in markedDisplays)
        {
            float previousSuccessfulLengthRatio = 0;
            foreach (Marker marker in markedDisplay.markers)
            {
                float lengthRatio = (1 - markerSpacingPercent) * (marker.number / scaledRulerLength);
                if (lengthRatio >= markerSpacingPercent && lengthRatio <= (1 - markerSpacingPercent) && lengthRatio - previousSuccessfulLengthRatio > markerSpacingPercent)
                {
                    float rulerPoint = lengthRatio - .5f; // converts lengthRatio which goes from 0 to 1 to a point on the ruler which goes from -0.5 to +0.5
                    marker.textmesh.rectTransform.localPosition = new Vector3(rulerPoint, 0, 0);
                    marker.textmesh.text = "― " + marker.number + " " + unit + " ―";
                    marker.textmesh.transform.localScale = new Vector3(marker.textmesh.transform.localScale.x, initialRulerLength / transform.lossyScale.z, marker.textmesh.transform.localScale.z);
                    marker.textmesh.gameObject.SetActive(true);
                    previousSuccessfulLengthRatio = lengthRatio;
                }
                else
                {
                    marker.textmesh.gameObject.SetActive(false);
                }
            }
            
        }
    }

    private class MarkedDisplay
    {
        public Canvas display;
        public List<Marker> markers;

        public MarkedDisplay(Canvas display, List<Marker> markers)
        {
            this.display = display;
            this.markers = markers;
        }
    }


    private class Marker
    {
        public float number;
        public TextMeshProUGUI textmesh;

        public Marker(float number, TextMeshProUGUI textmesh)
        {
            this.number = number;
            this.textmesh = textmesh;
        }
    }

}
