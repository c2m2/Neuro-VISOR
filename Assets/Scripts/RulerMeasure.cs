using C2M2.Simulation;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(MeshSimulation))]
[RequireComponent(typeof(List<TextMeshProUGUI>))]
public class RulerMeasure : MonoBehaviour
{
    public MeshSimulation sim = null;
    public List<TextMeshProUGUI> measurementDisplays;
    private float relativeLength;
    private float originalLength;

    // Start is called before the first frame update
    void Start()
    {
        relativeLength = 0;
        originalLength = transform.lossyScale.z;
        if (sim == null) Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        relativeLength = originalLength/sim.transform.localScale.z;
        
        int magnitude = GetMagnitude(relativeLength);
        string unit = GetUnit(magnitude);
        Tuple<int, float> numberAndRulerSize = GetNumberAndRulerSize(relativeLength, magnitude);

        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, originalLength*numberAndRulerSize.Item2);
        measurementDisplays.ForEach(measurementDisplay => measurementDisplay.text = numberAndRulerSize.Item1 + unit);
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

    private Tuple<int, float> GetNumberAndRulerSize(float relativeLength, int magnitude)
    {
        int unitGroup = magnitude / 3;
        // length is a scaled version of relativelength so it is between 1 and 1000
        float length = (float)(relativeLength / Math.Pow(10, unitGroup));

        // scale is the scientific notation power of 10 to convert adjustlength to a scientific notation coefficient
        int scale = Convert.ToInt32(Math.Pow(10, Math.Floor(Math.Log10(length))));

        int number = Convert.ToInt32(Math.Floor(length / scale)*scale);

        float lengthRatio = number/length;

        return new Tuple<int, float>(number, lengthRatio);
    }

}
