using C2M2.Simulation;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(MeshSimulation))]
[RequireComponent(typeof(List<Canvas>))]
[RequireComponent(typeof(List<int>))]
public class RulerMeasure : MonoBehaviour
{
    public MeshSimulation sim = null;
    public List<Canvas> measurementDisplays;
    public List<int> numbers;
    private List<Tuple<TextMeshProUGUI, int>> markers = new List<Tuple<TextMeshProUGUI, int>>();
    private float relativeLength;

    // Start is called before the first frame update
    void Start()
    {
        if (sim == null) Destroy(gameObject);
        relativeLength = 0;
        CreateMarkers();
    }

    // Update is called once per frame
    void Update()
    {
        relativeLength = transform.lossyScale.z / sim.transform.localScale.z;
        
        int magnitude = GetMagnitude(relativeLength);
        string unit = GetUnit(magnitude);

        int unitGroup = magnitude / 3;
        // length is a scaled version of relativelength so it is between 1 and 1000
        float length = (float)(relativeLength / Math.Pow(10, unitGroup));
        UpdateMarkers(length, unit);
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
        foreach(Canvas measurementDisplay in measurementDisplays)
        {
            foreach(int number in numbers)
            {
                GameObject marker = new GameObject();
                marker.transform.SetParent(measurementDisplay.transform);
                TextMeshProUGUI markerText = marker.AddComponent<TextMeshProUGUI>();
                markerText.alignment = TextAlignmentOptions.Center;
                markerText.rectTransform.localPosition = new Vector3(0, 0, 0);
                markerText.fontSize = 0.1f;
                markerText.color = Color.black;
                marker.transform.localRotation = Quaternion.Euler(0,0,90);
                marker.transform.localScale = Vector3.one;
                //maybe need to set width and height
                markers.Add(new Tuple<TextMeshProUGUI, int>(markerText, number)); //maybe change to passing gameobject
            }
            
        }
    }

    private void UpdateMarkers(float scaledRulerLength, string unit)
    {
        foreach (Tuple<TextMeshProUGUI, int> marker in markers)
        {
            int markerNumber = marker.Item2;
            TextMeshProUGUI markerText = marker.Item1;

            float lengthRatio = markerNumber / scaledRulerLength;
            if (lengthRatio >= .1 && lengthRatio < .9) //update to make minimum and maximum changeable
            {
                markerText.rectTransform.localPosition = new Vector3(2*(lengthRatio-.5f), 0, 0); //since center of ruler is 0
                markerText.text = "- " + markerNumber + " " + unit;
                markerText.gameObject.SetActive(true);
                //markerText.gameObject.GetComponent<MeshRenderer>().enabled = true;
            }
            else
            {
                markerText.gameObject.SetActive(false);
                //markerText.gameObject.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

}
