using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace C2M2.Visualization.VTK
{
    using static Utils.Math;
    /// <summary>
    /// Overarching manager to control order of events for reading in VTK data, positioning it, rescaling it, building colliders, raycasting, etc
    /// </summary>
    public class VTUObjectBuilder
    {
        public List<VTUObject> BuildVTUObjects(string dataPath, string dataExtension, Gradient gradient, int fileProcessCount)
        {
            // Read in each VTU file
            VTUReader vtuReader = new VTUReader();
            string[] files = Directory.GetFiles(dataPath, dataExtension);
            int frameCount = files.Length;
            List<VTUObject> vtuList = new List<VTUObject>(frameCount);
            float max = 0f;
            float min = 0f;
            if (fileProcessCount > frameCount)
            { // If the user wants to process more files than exist, just render all of the files
                fileProcessCount = frameCount;
            }
            for (int i = 0; i < fileProcessCount; i++)
            {
                vtuList.Add(vtuReader.ParseFile(files[i]));
                vtuList[i].mesh.name = i.ToString();

                max = Max(max, vtuList[i].localMax);
                min = Min(min, vtuList[i].localMin);
            }
            for (int i = 0; i < fileProcessCount; i++)
            {
                vtuList[i].FillColors(max, min, gradient);
            }
            return vtuList;
        }
        public List<VTUObject> BuildVTUObjects(string dataPath, string dataExtension, Gradient gradient) => BuildVTUObjects(dataPath, dataExtension, gradient, int.MaxValue);
    }
}