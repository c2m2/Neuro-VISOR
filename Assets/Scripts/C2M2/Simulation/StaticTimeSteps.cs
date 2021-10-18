using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticTimeSteps : MonoBehaviour
{
    public static float updateVisualizationTime = 0.002f;
    public float timeSteps = 0.002f;

    public void Update()
    {
        updateVisualizationTime = timeSteps;
    }
}
