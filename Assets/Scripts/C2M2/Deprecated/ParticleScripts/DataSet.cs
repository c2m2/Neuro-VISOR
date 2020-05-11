using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// TODO: You should actually make the struct a Particle, and give it a position and colorfloat and color, and then make a list of particles and maybe that can be its own data type
/// </summary>
public class DataSet {

    public List<DataPoint> dataList;
    private List<DataPoint> dataListBackup;



    //**CONSTRUCTORS**

    public DataSet()
    {
        dataList = new List<DataPoint>();
        BackupDataSet();
    }

    //Custom positions, color, colorfloat 
    public DataSet(List<DataPoint> dataList)
    {
        this.dataList = dataList;
        BackupDataSet();
    }

    //**RESIZING METHODS**

    //Take in room bounds and resize particle field accordingly
    /// For each dimension we use the following equation to scale a range [min, max] to [a, b]
    /// 
    ///         (b - a)(x - min)
    /// f(x) =  ----------------  + a
    ///            max - min
    ///            
    /// where, for instance in the y dimension, holderY = y, min = localYMin, max = localYMax,
    /// a = globalYMax, & b = globalYMin
    /// Then repeat for each positionList in the list of lists
    public void PositionsBoundsResize(Vector3 globalMaxValues, Vector3 globalMinValues)
    {
        if (globalMaxValues.x > globalMinValues.x && globalMaxValues.y > globalMinValues.y && globalMaxValues.z > globalMinValues.z)
        {
            Debug.Log("Resizing particle field...");

            float holderX = 0f;
            float holderY = 0f;
            float holderZ = 0f;

            int i = 0;

            Vector3 localMaxValues = new Vector3(dataList.Max(point => point.position.x), dataList.Max(point => point.position.y), dataList.Max(point => point.position.z));
            Vector3 localMinValues = new Vector3(dataList.Min(point => point.position.x), dataList.Min(point => point.position.y), dataList.Min(point => point.position.z));

            while (i < dataList.Count)
            {
                //Resize x
                holderX = dataList.ElementAt(i).position.x;
                holderX = ((((globalMaxValues.x - globalMinValues.x) * (holderX - localMinValues.x)) /
                    (localMaxValues.x - localMinValues.x)) + globalMinValues.x);

                //Resize y
                holderY = dataList.ElementAt(i).position.y;
                holderY = ((((globalMaxValues.y - globalMinValues.y) * (holderY - localMinValues.y)) /
                    (localMaxValues.y - localMinValues.y)) + globalMinValues.y);

                //Resize z
                holderZ = dataList.ElementAt(i).position.z;
                holderZ = ((((globalMaxValues.z - globalMinValues.z) * (holderZ - localMinValues.z)) /
                    (localMaxValues.z - localMinValues.z)) + globalMinValues.z);

                dataList.ElementAt(i).position = new Vector3(holderX, holderY, holderZ);

                i++;
            }

            Debug.Log("Done resizing particle field");
        }
    }

    //Resize data set by percent
    public void PositionsPercentageResize(float resizePercent)
    {
        Debug.Log("Resizing particle field by percent...");

        float holderX = 0f;
        float holderY = 0f;
        float holderZ = 0f;

        int i = 0;

        while (i < dataList.Count)
        {
            holderX = dataList.ElementAt(i).position.x;
            holderY = dataList.ElementAt(i).position.y;
            holderZ = dataList.ElementAt(i).position.z;

            holderX *= resizePercent;
            holderY *= resizePercent;
            holderZ *= resizePercent;

            dataList.ElementAt(i).position = new Vector3(holderX, holderY, holderZ);
            i++;
        }

        Debug.Log("Done resizing particle field");
    }



    //**COLOR MAPPING METHODS**

    //Iterate through color floats and translate them into colors
    public void ColorMap_KelvinScale()
    {
        float max = dataList.Max(point => point.scalarValue);
        float min = dataList.Min(point => point.scalarValue);

        float adjustedScaleFloat = 0f;

        //Adjust to 1000K-40000K scale then use builtin Unity function to color
        foreach (DataPoint point in dataList)
        {
            adjustedScaleFloat = (((40000 - 1000) * (point.scalarValue - min) / (max - min)) + 1000);
            point.color = Mathf.CorrelatedColorTemperatureToRGB(adjustedScaleFloat);
        }
    }


    /// For each colorfloat we use the following equation to scale a range [min, max] to [a, b]
    /// 
    ///         (b - a)(x - min)
    /// f(x) =  ----------------  + a
    ///            max - min
    ///            
    /// where, for instance in the y dimension, holderY = y, min = localYMin, max = localYMax,
    /// a = globalYMax, & b = globalYMin
    /// Then repeat for each positionList in the list of lists
    //Max value is pure red (1, 0, 0, 100), min value is pure blue (0, 0, 1, 100)
    //We need to scale softly down like so:
    //      max         (max - min)/2           min
    //(1, 0, 0, 100) -> (1, 1, 1, 100) -> (0, 0, 1, 100)
    //      red             white               blue
    //    FF0000           FFFFFF              0000FF
    
    //I think that we need to split the float values into two halves, lower value half and upper value half
    //Then you scale the bottom half to the range x = (0, 1) and whatever value it has assign that value to both the R and G values in the returned color (and keep B = 1)
    //In the top half you scale to the range x = (0, 1), take 1 - x to invert it, and assign that value to G and B values of the returned color and keep R = 1.
    public void ColorMap_RedWhiteBlue()
    {
        //Find max, min, and mid point
        float max = dataList.Max(point => point.scalarValue);
        float min = dataList.Min(point => point.scalarValue);
        float mid = (max - min) / 2f;
        float scaledValue;

        //Color every point in the set
        foreach(DataPoint point in dataList)
        {
            //Split the values into two groups: below the middle value and above the middle value
            if((point.scalarValue >= min) && (point.scalarValue <= mid))  //Bottom half of range (mid is now the maximum for this range)
            {
                scaledValue = (((point.scalarValue - min)) / (mid - min));
                point.color = new Color(scaledValue, scaledValue, 1, 100);
            }else if((point.scalarValue > mid) && (point.scalarValue <= max)) //Top half of range (mid is now the minimum for this range)
            {
                scaledValue = 1 - (((point.scalarValue - mid)) / (max - mid));
                point.color = new Color(1, scaledValue, 1, scaledValue);
            }
            else
            {
                Debug.Log("Error: point.ColorFloat outside of range");
            }
        }
    }



    //**REDUCTION METHODS**

    //If reduceCount = 2, remove one out of every 2 elements. If reduceCount = 3, remove one out of every 3 elements, etc
    public void ReduceLists(int reduceCount)
    {
        int i;

        for (i = 0; i < dataList.Count; i += reduceCount)
        {
            dataList.RemoveAt(i);
            i--;
        }
    }

    public void IsolateQuantity(float lowRange, float highRange)
    {
        Debug.Log("Isolating Quantity...");

        RestoreOriginalValues();

        if(lowRange <= highRange)
        {
            int i;

            for(i = 0; i < dataList.Count; i++)
            {
                if(dataList.ElementAt(i).scalarValue < lowRange || dataList.ElementAt(i).scalarValue > highRange)
                {
                    dataList.RemoveAt(i);
                    i--;
                }
            }

            Debug.Log("Done Isolating Quantity.");
        }
        else
        {
            Debug.Log("High value must be larger than low value");
        }
    }



    //**RESTORATION METHODS**

    public void RestoreOriginalValues()
    {
        Debug.Log("Restoring dataList");
        dataList = new List<DataPoint>(dataListBackup);
        Debug.Log("dataList Restored");
    }

    public void BackupDataSet()
    {
        dataListBackup = new List<DataPoint>(dataList);
    }

}
