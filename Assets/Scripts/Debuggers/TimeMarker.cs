using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TimeMarker
{
    private int initialTime = 0;
    private int finalTime = 0;
    private string[] strings = { "TimeMarker \"", "\": ", " ms" };

    public void TakeInitialTime()
    {
        initialTime = Environment.TickCount;
    }
    /// <summary> Take the final time marker. </summary>
    /// <param name="print"> true = print the time difference </param>
    /// <param name="identifier"> identifying string for this time marker </param>
    public void TakeFinalTime(bool print, string identifier)
    {
        finalTime = Environment.TickCount;
        if (print)
        {
            PrintTotal(identifier);
            initialTime = 0;
            finalTime = 0;
        }      
    }

    public void PrintTotal(string identifier)
    {
        Debug.Log(strings[0] + identifier + strings[1] + (finalTime - initialTime) + strings[2]);
    }

}
