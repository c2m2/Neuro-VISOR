using C2M2.Simulation;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(MeshSimulation))]
public class RulerMeasure : MonoBehaviour
{
    public MeshSimulation sim = null;
    public Canvas canvas;
    private Vector3 localSize;
    private float rulerLength;

    // Start is called before the first frame update
    void Start()
    {
        localSize = Vector3.zero;
        if (sim == null) Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        localSize = sim.transform.localScale;
        rulerLength = transform.lossyScale.z;

        float relativeLength = ReturnRulerMeshLength();
        int magnitude = GetMagnitude(relativeLength);
        string unit = GetUnit(magnitude);
        foreach (KeyValuePair<float, float> measurement in GetNumbersAndLocation(relativeLength, magnitude))
        {
            AddToRuler(measurement.Key, measurement.Value, unit, canvas);
        }

        //numberValues.text = ToString();
    }

    // returns rulers length relative the the mesh
    private float ReturnRulerMeshLength()
    {
        return rulerLength/localSize.z;
    }

    private int GetMagnitude(float relativeLength)
    {
        double lengthLog10 = Math.Log10(relativeLength);
        return Convert.ToInt32(Math.Floor(lengthLog10));
    }

    private string GetUnit(int magnitude)
    {
        //TODO check if for really big and really small ones which units it should be in terms of
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

    private SortedDictionary<float, float> GetNumbersAndLocation(float relativeLength, int magnitude)
    {

        // length is a scaled version of relativelength so it is between 1 and 1000
        float length = (float)(relativeLength / Math.Pow(10, magnitude));

        // scale is the scientific notation power of 10 to convert adjustlength to a scientific notation coefficient
        int scale = Convert.ToInt32(Math.Pow(10, Math.Floor(Math.Log10(length))));

        float adjustedLength = length / scale;

        List<float> numbers = new List<float>();

        if (adjustedLength < 2)
        {
            numbers.Add(0.5f);
            numbers.Add(1);
        }
        else if (adjustedLength < 3)
        {
            numbers.Add(1);
            numbers.Add(2);
        }
        else if (adjustedLength < 4)
        {
            numbers.Add(1);
            numbers.Add(2);
            numbers.Add(3);
        }
        else if (adjustedLength < 6)
        {
            numbers.Add(1);
            numbers.Add(2);
            numbers.Add(4);
        }
        else if (adjustedLength < 8)
        {
            numbers.Add(1);
            numbers.Add(3);
            numbers.Add(6);
        }
        else if (adjustedLength < 10)
        {
            numbers.Add(2);
            numbers.Add(4);
            numbers.Add(8);
        }

        SortedDictionary<float, float> numbersAndLocations = new SortedDictionary<float, float>();

        foreach (float num in numbers)
        {
            numbersAndLocations.Add(num*scale, num*scale/length);
        }

        return numbersAndLocations;
    }

    private void AddToRuler(float value, float location, string unit, Canvas rulerCanvas)
    {
        string measurement = value + unit;
        //Debug.LogError(value + ":" + location + ":" + unit);
    }
}
