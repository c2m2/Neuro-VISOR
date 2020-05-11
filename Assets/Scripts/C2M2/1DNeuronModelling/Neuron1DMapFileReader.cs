using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class Neuron1DMapFileReader
{
    private static char newSectionToken = '#';
    private static string swcToken = "swc";
    private static string objToken = "obj";
    public static Neuron1DVertMap ReadMapFile(string mapPath)
    {
        // Get the data path from the input extension and streamingAssets folder name      
        Dictionary<int, int> markerTo1D = new Dictionary<int, int>();
        Dictionary<int, List<int>> markerTo3D = new Dictionary<int, List<int>>();
        // If our file does not exist, throw an exception
        if (!File.Exists(mapPath)) { throw new System.Exception("Could not find file " + mapPath); }
        // Open our map file as a stream
        StreamReader reader = new StreamReader(mapPath);
        bool swc = false;
        // Read The map file
        while (reader.Peek() > -1)
        {
            // Read the next line of the file
            string curLine = reader.ReadLine();
            if (curLine[0] == newSectionToken)
            { // If we have found a new section of data
                // If we have found the swc marker, interpret read values as 1D verts
                if (curLine.Contains(swcToken)) { swc = true; }
                // If we have found the obj marker, interpret read values as 3D verts
                else if (curLine.Contains(objToken)) { swc = false; }
            }
            else
            { // If we're reading in vertices,
                // Split the current line by spaces
                string[] splitLine = curLine.Split(' ');
                // Try to interpret the result as int values
                int index = int.Parse(splitLine[0]);
                int marker = int.Parse(splitLine[1]);
                if (swc)
                { // If we're reading in swc/1D vertices
                    // 1D verts should be unique, so we should never have duplicates
                    if (markerTo1D.ContainsKey(marker))
                    { // If we do find a duplicate, just remove it for simplicity and add the new one
                        markerTo1D.Remove(marker);
                    }
                    // Add the new marker to our lookup
                    markerTo1D.Add(marker, index);
                }
                else
                { // If we're reading in obj/3D vertices
                    index--; // We want the 3D indices to start from 0
                    // If our 3D lookup already has verts associated with this marker,
                    if (markerTo3D.ContainsKey(marker))
                    {
                        // And if this marker's existing list doesn't already have this 3D vert, add it
                        if (!markerTo3D[marker].Contains(index)) { markerTo3D[marker].Add(index); }
                    }
                    else
                    { // If our 3D lookup DOES NOT already have verts associated with this marker,
                        // Initialize a new list
                        List<int> newList = new List<int>(1);
                        // Add our first vertex
                        newList.Add(index);
                        // Add our new list to the dictionary
                        markerTo3D.Add(marker, newList);
                    }
                }
            }
        }
        // Build the map from read info
        // Intialize vert dictionaries
        Dictionary<int, List<int>> oneToThree = new Dictionary<int, List<int>>(markerTo1D.Count);
        // TODO: Not a good way to estimate the needed size. The real size is the size of each list
        Dictionary<int, int> threeToOne = new Dictionary<int, int>(markerTo3D.Count);
        foreach (KeyValuePair<int, int> pair1D in markerTo1D)
        { // For each marker,
            // Find the marker that associates a 1D vert to 3D verts
            int marker = pair1D.Key;
            // Make sure that the 3D dict also has this marker
            if (markerTo3D.ContainsKey(marker))
            { // If our 3D dictionary has the same marker as the one just found in the 1D dictionary.
                // Get the 1D vert and the list of 3D verts
                int vert1D = pair1D.Value;
                List<int> verts3D = markerTo3D[marker];
                // Add 3D verts to 1D key
                oneToThree.Add(vert1D, verts3D);
                // Add 1D vert to 3D key
                foreach (int i in verts3D) { threeToOne.Add(i, vert1D); }
            }
            else
            { // If our 3D dictionary does NOT have the same marker as the 1D, then something has gone wrong.
                Debug.LogError("Could not find 3D verts associated with " + marker);
            }
        }
        return new Neuron1DVertMap(oneToThree, threeToOne);
    }
}
