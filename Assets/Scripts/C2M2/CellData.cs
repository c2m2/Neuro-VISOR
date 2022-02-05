using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CellData
{
    public Vector3 pos;
    public string vrnFileName;
    public Gradient gradient;
    public float globalMin;
    public float globalMax;
    public int refinementLevel;
    public double timeStep;
    public double endTime;
    public double raycastHitValue;
    public string unit;
    public float unitScaler;
    public int colorMarkerPrecision;
}
