using C2M2.Simulation;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(MeshSimulation))]
public class RulerMeasure : MonoBehaviour
{
    public MeshSimulation sim = null;
    public TextMeshProUGUI textTMP;
    private float relativeLength;
    private float rulerLength;

    // Start is called before the first frame update
    void Start()
    {
        relativeLength = 0;
        if (sim == null) Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        relativeLength = 1/sim.transform.localScale.z; //~200 is in world space, 1 should be replaced with local size of ruler's parent
        // should rename
        
        int magnitude = GetMagnitude(relativeLength);
        string unit = GetUnit(magnitude);
        Tuple<int, float> data = GetNumberAndRulerSize(relativeLength, magnitude);

        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, data.Item2);
        textTMP.text = data.Item1 + unit;

        //TODO change to only update when simulation changes size
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

    private Tuple<int, float> GetNumberAndRulerSize(float relativeLength, int magnitude)
    {
        int unitGroup = magnitude / 3;
        // length is a scaled version of relativelength so it is between 1 and 1000
        float length = (float)(relativeLength / Math.Pow(10, unitGroup));

        // scale is the scientific notation power of 10 to convert adjustlength to a scientific notation coefficient
        int scale = Convert.ToInt32(Math.Pow(10, Math.Floor(Math.Log10(length))));

        int number = Convert.ToInt32(Math.Floor(length / scale)*scale);

        float length2 = number/length;

        return new Tuple<int, float>(number, length2);
    }

}
