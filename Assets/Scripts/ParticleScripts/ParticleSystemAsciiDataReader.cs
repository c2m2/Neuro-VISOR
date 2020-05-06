using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ParticleSystemAsciiDataReader : MonoBehaviour
{
    public TextAsset[] assets;
    public GameObject RoomObject;

    //TODO: This is dumb change this
    public int reduceBy = 2;

    private string assetString = string.Empty;
    private string currentLine = string.Empty;
    private StringReader sR;

    //Script can currently read 4 data points per line
    private string[] currentLineSplit = new string[4];

    //Keep lists with all positions and colors from all files, i.e. "frames" of an experiment animation
    public List<DataSet> dataSetList = new List<DataSet>();
    private List<DataSet> backupdataSetList = new List<DataSet>(); 

    public float colorFloatMax = 0f;
    public float colorFloatMin = 0f;

    public void InitializeParticleData()
    {
        //Get room bounds for resizing
        MeshRenderer renderer = RoomObject.GetComponent<MeshRenderer>();
        Vector3 roomBoundsMax = new Vector3(renderer.bounds.max.x, renderer.bounds.max.y, renderer.bounds.max.z);
        Vector3 roomBoundsMin = new Vector3(renderer.bounds.min.x, renderer.bounds.min.y, renderer.bounds.min.z);

        //Create initial string reader
        CreateStringReader(0);

        //prime the while loop
        int p = 0;
        DataSet particleSet = new DataSet();
        while (p < assets.Length)
        {
            //Get positions and color float values from ascii file
            InitializeInformationLists(particleSet);         

            // FindColorFloatMaxandMin(p);
            //Could change this to other maps with a switch
            //TODO, figure this out after going through every text asset so that your max and min are actually global
            particleSet.ColorMap_RedWhiteBlue();           

            //resize each dataset to the room, then downscale it further by percent
            particleSet.PositionsBoundsResize(roomBoundsMax, roomBoundsMin);
            particleSet.PositionsPercentageResize(0.3f);

            //Save the original before resizing
            backupdataSetList.Add(particleSet);
            particleSet.ReduceLists(reduceBy);
            particleSet.ReduceLists(reduceBy);
         //  particleSet.ReduceLists(reduceBy);
       //    particleSet.ReduceLists(reduceBy);
           // particleSet.ReduceLists(reduceBy);

            particleSet.BackupDataSet();
            
            //Add to our list of "frames"
            dataSetList.Add(particleSet);

            CloseStringReader(sR);
            
            p++;
            if (p < assets.Length)
            {
                CreateStringReader(p);
                particleSet = new DataSet();
            }            
        }
        p = 0;
    }

    //Break ascii file into strings and split into string arrays, then add information to appropriate information lists
    private void InitializeInformationLists(DataSet dataSet)
    {
        //Store datapoints into Vector3, then assign Vector3 to positions list
        Vector3 currentPosition = new Vector3();

        currentLine = sR.ReadLine();
        if (currentLine != null)
        {
            currentLineSplit = currentLine.Split(' ');
        }

        while (currentLine != null && currentLineSplit != null)
        {
            DataPoint dataPoint = new DataPoint();
            for (int u = 0; u < 4; u += 4)
            {
                if (!currentLineSplit[u].Equals(string.Empty))
                {
                    currentPosition.x = (float.Parse(currentLineSplit[u]));
                }

                if (!currentLineSplit[u + 1].Equals(string.Empty))
                {
                    currentPosition.y = float.Parse(currentLineSplit[u + 1]);
                }

                if (!currentLineSplit[u + 2].Equals(string.Empty))
                {
                    currentPosition.z = float.Parse(currentLineSplit[u + 2]);
                }

                if (!currentLineSplit[u + 3].Equals(string.Empty))
                {
                    dataPoint.scalarValue = float.Parse(currentLineSplit[u + 3]);
                }
                dataPoint.position = currentPosition;

                dataSet.dataList.Add(dataPoint);
                dataPoint = new DataPoint();
            }

            currentLine = sR.ReadLine();
            if (currentLine != null)
            {
                currentLineSplit = currentLine.Split(' ');
            }
        }
    }

    private void CreateStringReader(int assetIndex)
    {
        //Put text file into string
        assetString = assets[assetIndex].text;

        //Create a StringReader to parse the text file more effectively with Read() and ReadLine()
        sR = new StringReader(assetString);
    }

    private void CloseStringReader(StringReader sR)
    {
        sR.Close();
        assetString = string.Empty;
    }

    public void Isoquant(DataSet dataSet, float low, float high)
    {

    }

}