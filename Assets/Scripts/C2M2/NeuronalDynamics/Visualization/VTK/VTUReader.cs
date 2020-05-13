using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml;

namespace C2M2.NeuronalDynamics.Visualization.VTK
{
    /// <summary>
    /// Translates 1 or more VTK files into Unity meshes
    /// </summary>
    /// TODO: This needs to be able to take general 'types'. Each element should be able to be int32, int64, float32, float64, etc
    ///         We should also have a setting called "geometry static", which when enabled should skip reading new points, cells, offsets, & types, & should ONLY read new pointData
    public class VTUReader
    {
        /// <summary> Parse a given VTU file and store it in a mesh </summary>
        /// <param name="vtuFile"> Sample VTU file </param>
        /// <param name="mesh"> Read-in VTU data will be stored in a Unity mesh </param>
        public VTUObject ParseFile(string vtuFile)
        {
            FileStream stream = File.Open(vtuFile, FileMode.Open);
            Vector3[] points = new Vector3[0];
            int[] cellData = new int[0], offsetData = new int[0];
            byte[] typeData = new byte[0];
            float[] componentData = new float[0];
            using (XmlReader reader = XmlReader.Create(stream))
            {
                int numberOfPoints = 0, numberOfCells = 0;
                bool inPoints = false, inCells = false, inPointData = false;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "DataArray":
                                if (inPoints)
                                {
                                    string type, format = "";
                                    int numberOfComponents = 0;
                                    while (reader.MoveToNextAttribute())
                                    {
                                        if (reader.Name == "type")
                                            type = reader.Value;
                                        else if (reader.Name == "NumberOfComponents")
                                            numberOfComponents = int.Parse(reader.Value);
                                        else if (reader.Name == "format")
                                            format = reader.Value;
                                    }
                                    reader.MoveToContent();
                                    if (format == "binary")
                                        points = TranslatePointsBinary(reader, numberOfPoints);
                                    else
                                        points = TranslatePoints(format, reader.ReadElementContentAsString(), numberOfPoints);
                                }
                                else if (inCells)
                                {
                                    string type, name = "", format = "";
                                    while (reader.MoveToNextAttribute())
                                    {
                                        if (reader.Name == "type")
                                            type = reader.Value;
                                        else if (reader.Name == "Name")
                                            name = reader.Value;
                                        else if (reader.Name == "format")
                                            format = reader.Value;
                                    }
                                    reader.MoveToContent();
                                    switch (name)
                                    {
                                        case "connectivity":
                                            if (format == "binary")
                                                cellData = TranslateCellDataBinary(reader, numberOfCells);
                                            else
                                                cellData = TranslateCellData(format, reader.ReadElementContentAsString());
                                            break;
                                        case "offsets":
                                            if (format == "binary")
                                                offsetData = TranslateOffsetDataBinary(reader, numberOfCells);
                                            else
                                                offsetData = TranslateOffsetData(format, reader.ReadElementContentAsString());
                                            break;
                                        case "types":
                                            if (format == "binary")
                                                typeData = TranslateTypeDataBinary(reader, numberOfCells);
                                            else
                                                typeData = TranslateTypeData(format, reader.ReadElementContentAsString());
                                            break;
                                        default:
                                            Debug.LogError("No name found in DataArray in Cells");
                                            break;
                                    }

                                }
                                else if (inPointData)
                                {
                                    string type, name = "", format = "";
                                    while (reader.MoveToNextAttribute())
                                    {
                                        if (reader.Name == "type")
                                            type = reader.Value;
                                        else if (reader.Name == "Name")
                                            name = reader.Value;
                                        else if (reader.Name == "format")
                                            format = reader.Value;
                                    }
                                    reader.MoveToContent();
                                    if (format == "binary")
                                        componentData = TranslateComponentDataBinary(reader, numberOfPoints);
                                    else
                                        componentData = TranslateComponentData(format, reader.ReadElementContentAsString());
                                }
                                break;
                            case "Piece":
                                if (reader.HasAttributes)
                                {
                                    while (reader.MoveToNextAttribute())
                                    {
                                        if (reader.Name == "NumberOfPoints")
                                        {
                                            numberOfPoints = int.Parse(reader.Value);
                                        }
                                        else if (reader.Name == "NumberOfCells")
                                        {
                                            numberOfCells = int.Parse(reader.Value);
                                        }
                                    }
                                }
                                break;
                            case "Points":
                                inPoints = true;
                                inCells = false;
                                inPointData = false;
                                break;
                            case "Cells":
                                inCells = true;
                                inPoints = false;
                                inPointData = false;
                                break;
                            case "PointData":
                                inPointData = true;
                                inPoints = false;
                                inCells = false;
                                break;
                            default:
                                break;
                        }
                    }
                }
                reader.Close();
            }
            stream.Close();
            // Finalize VTUObject and return it
            return new VTUObject(vtuFile, points, BuildTriangles(offsetData, typeData, cellData).ToArray(), componentData);
        }
        #region Private Methods
        private static int[] TranslateCellDataBinary(XmlReader reader, int numberOfCells)
        {
            int skipCount = 1;   // We need to skip 1 int of metadeta
            byte[] cellBytes = new byte[((numberOfCells * 3) + skipCount) * 4];
            reader.ReadElementContentAsBase64(cellBytes, 0, cellBytes.Length);
            int[] cellData = new int[(cellBytes.Length / 4) - skipCount];
            // Translate cells into bytes & skip the first byte
            int shiftedI;
            for (int i = 0; i < cellData.Length; i++)
            {
                shiftedI = (i + skipCount) * 4;
                cellData[i] = cellBytes[shiftedI] | (cellBytes[shiftedI + 1] << 8) | (cellBytes[shiftedI + 2] << 16) | (cellBytes[shiftedI + 3] << 24);
                //cellData[i] = BitConverter.ToInt32(cellBytes, ((i + skipCount) * 4));
            }
            return cellData;
        }
        private static int[] TranslateCellData(string format, string cellRawString)
        {
            int[] cellData;
            if (format.Equals("binary", StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] cellBytes = Convert.FromBase64String(cellRawString);
                Debug.Log("cellBytes.Length: " + cellBytes.Length);
                //cellData = Enumerable.Range(0, cellBytes.Length / 4).Select(i => BitConverter.ToInt32(cellBytes, i * 4)).Skip(1).ToArray();
                int skipCount = 1;   // We need to skip 1 int of metadeta
                cellData = new int[(cellBytes.Length / 4) - skipCount];
                skipCount *= 4;
                // Translate cells into bytes & skip the first byte
                for (int i = 0; i < cellData.Length; i++)
                {
                    cellData[i] = BitConverter.ToInt32(cellBytes, (i * 4) + skipCount);
                }
            }
            else
            {
                string[] cellSubstrings = cellRawString.Split(' ');
                cellData = new int[cellSubstrings.Length];
                for (int i = 0; i < cellSubstrings.Length; i++)
                {
                    cellData[i] = int.Parse(cellSubstrings[i]);
                }
            }
            Debug.Log("cellData.Length: " + cellData.Length);
            return cellData;
        }
        private static byte[] TranslateTypeDataBinary(XmlReader reader, int numberOfCells)
        {
            int skipCount = 4;
            byte[] typeBytes = new byte[numberOfCells + skipCount];
            //byte[] typeData = Convert.FromBase64String(typeRawString);
            reader.ReadElementContentAsBase64(typeBytes, 0, typeBytes.Length);
            byte[] typeData = new byte[numberOfCells];
            for (int i = 0; i < numberOfCells; i++)
            {
                typeData[i] = typeBytes[i + skipCount];
            }
            return typeData;
        }
        private static byte[] TranslateTypeData(string format, string typeRawString)
        {
            byte[] typeData;
            if (format.Equals("binary", StringComparison.InvariantCultureIgnoreCase))
            {
                //typeData = Convert.FromBase64String(typeRawString).Skip(4).ToArray();      // This is all we need to do here, since type will always take the range 1-25
                typeData = Convert.FromBase64String(typeRawString);
                int skipCount = 4;
                byte[] typeDataRefined = new byte[typeData.Length - skipCount];
                for (int i = 0; i < typeData.Length - skipCount; i++)
                {
                    typeDataRefined[i] = typeData[i + skipCount];
                }
                typeData = typeDataRefined;
            }
            else
            {
                string[] typeSubstrings = typeRawString.Split(' ');
                typeData = new byte[typeSubstrings.Length];
                for (int i = 0; i < typeSubstrings.Length; i++)
                {
                    typeData[i] = byte.Parse(typeSubstrings[i]);
                }
            }
            return typeData;
        }
        private static int[] TranslateOffsetDataBinary(XmlReader reader, int numberOfCells)
        {
            int skipCount = 1;   // We need to skip 1 int of metadeta
            byte[] offsetBytes = new byte[(numberOfCells + skipCount) * 4];
            reader.ReadElementContentAsBase64(offsetBytes, 0, offsetBytes.Length);
            int[] offsetData = new int[(offsetBytes.Length / 4) - skipCount];
            // Translate cells into bytes & skip the first byte
            int shiftedI;
            for (int i = 0; i < offsetData.Length; i++)
            {
                shiftedI = (i + skipCount) * 4;
                offsetData[i] = offsetBytes[shiftedI] | (offsetBytes[shiftedI + 1] << 8) | (offsetBytes[shiftedI + 2] << 16) | (offsetBytes[shiftedI + 3] << 24);
            }
            return offsetData;
        }
        private static int[] TranslateOffsetData(string format, string offsetRawString)
        {
            int[] offsetData;
            if (format.Equals("binary", StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] offsetBytes = Convert.FromBase64String(offsetRawString);
                //offsetData = Enumerable.Range(0, offsetBytes.Length / 4).Select(i => BitConverter.ToInt32(offsetBytes, i * 4)).Skip(1).ToArray();
                int skipCount = 1;   // We need to skip 1 int of metadeta
                offsetData = new int[(offsetBytes.Length / 4) - skipCount];
                skipCount *= 4;
                // Translate cells into bytes & skip the first byte
                for (int i = 0; i < offsetData.Length; i++)
                {
                    offsetData[i] = BitConverter.ToInt32(offsetBytes, (i * 4) + skipCount);
                }
            }
            else
            {
                string[] offsetSubstrings = offsetRawString.Split(' ');
                offsetData = new int[offsetSubstrings.Length];
                for (int i = 0; i < offsetSubstrings.Length; i++)
                {
                    offsetData[i] = int.Parse(offsetSubstrings[i]);
                }
            }
            return offsetData;
        }
        private static float[] TranslateComponentDataBinary(XmlReader reader, int numberOfPoints)
        {
            int skipCount = 1;   // We need to skip 1 int of metadeta
            byte[] componentBytes = new byte[(numberOfPoints + skipCount) * 4];
            reader.ReadElementContentAsBase64(componentBytes, 0, componentBytes.Length);
            float[] componentData = new float[numberOfPoints];
            // Translate cells into bytes & skip the first byte
            for (int i = 0; i < componentData.Length; i++)
            {
                componentData[i] = BitConverter.ToSingle(componentBytes, (i + skipCount) * 4);
            }
            return componentData;
        }
        private static float[] TranslateComponentData(string format, string componentRawString)
        {
            float[] componentData;
            if (format.Equals("binary", StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] componentBytes = Convert.FromBase64String(componentRawString);
                //componentData = Enumerable.Range(0, componentBytes.Length / 4).Select(i => BitConverter.ToSingle(componentBytes, i * 4)).Skip(1).ToArray();
                int skipCount = 1;   // We need to skip 1 int of metadeta
                componentData = new float[(componentBytes.Length / 4) - skipCount];
                skipCount *= 4;
                // Translate cells into bytes & skip the first byte
                for (int i = 0; i < componentData.Length; i++)
                {
                    componentData[i] = BitConverter.ToSingle(componentBytes, (i * 4) + skipCount);
                }
            }
            else
            {
                string[] componentSubstrings = componentRawString.Split(' ');
                componentData = new float[componentSubstrings.Length];
                for (int i = 0; i < componentSubstrings.Length; i++)
                {
                    componentData[i] = float.Parse(componentSubstrings[i]);
                }
            }
            return componentData;
        }
        private static Vector3[] TranslatePointsBinary(XmlReader reader, int numberOfPoints)
        {
            int skipCount = 1;   // We need to skip 1 int of metadeta
            byte[] pointBytes = new byte[((numberOfPoints * 3) + skipCount) * 4]; // numberOfPoints * 3 values per point * 4 bytes per value
                                                                                  // Convert the byte arrays to appropriate types
                                                                                  //float[] pointFloats = Enumerable.Range(0, pointBytes.Length / 4).Select(i => BitConverter.ToSingle(pointBytes, i * 4)).Skip(1).ToArray();
            reader.ReadElementContentAsBase64(pointBytes, 0, pointBytes.Length);
            float[] pointFloats = new float[numberOfPoints * 3];
            // Translate cells into bytes & skip the first byte
            for (int i = 0; i < pointFloats.Length; i++)
            {
                pointFloats[i] = BitConverter.ToSingle(pointBytes, ((i + skipCount) * 4));
            }
            // Convert the point array into a Vector3 array that is palatable to the mesh
            Vector3[] points = new Vector3[numberOfPoints];
            for (int i = 0; i < pointFloats.Length; i += 3)
            {
                /// This line is important: Unity uses a different coordinate system than VTK: Right hand vs. left hand coordinate system!
                points[i / 3] = new Vector3(pointFloats[i], pointFloats[i + 2], pointFloats[i + 1]);
            }
            return points;
        }
        private static Vector3[] TranslatePoints(string format, string pointRawString, int numberOfPoints)
        {
            Vector3[] pointData;
            if (format.Equals("binary", StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] pointBytes = Convert.FromBase64String(pointRawString);
                // Convert the byte arrays to appropriate types
                //float[] pointFloats = Enumerable.Range(0, pointBytes.Length / 4).Select(i => BitConverter.ToSingle(pointBytes, i * 4)).Skip(1).ToArray();


                int skipCount = 1;   // We need to skip 1 int of metadeta
                float[] pointFloats = new float[(pointBytes.Length / 4) - skipCount];
                skipCount *= 4;
                // Translate cells into bytes & skip the first byte
                for (int i = 0; i < pointFloats.Length; i++)
                {
                    pointFloats[i] = BitConverter.ToSingle(pointBytes, (i * 4) + skipCount);
                }

                // Convert the point array into a Vector3 array that is palatable to the mesh
                pointData = new Vector3[numberOfPoints];
                for (int i = 0; i < pointFloats.Length; i += 3)
                {
                    /// This line is important: Unity uses a different coordinate system than VTK: Right hand vs. left hand coordinate system!
                    pointData[i / 3] = new Vector3(pointFloats[i], pointFloats[i + 2], pointFloats[i + 1]);
                }
            }
            else
            {
                // Get individual data pieces from grand strings
                string[] pointSubstrings = pointRawString.Split(' ');
                // Request memory to store data in palatable types
                pointData = new Vector3[numberOfPoints];
                // Translate piece strings into relevant data types
                for (int i = 0; i < pointSubstrings.Length; i += 3)
                {
                    /// This line is important: Unity uses a different coordinate system than VTK: Right hand vs. left hand coordinate system!
                    pointData[i / 3] = new Vector3(float.Parse(pointSubstrings[i]), float.Parse(pointSubstrings[i + 2]), float.Parse(pointSubstrings[i + 1]));
                }
            }

            return pointData;
        }
        private static List<int> BuildTriangles(int[] offsetData, byte[] typeData, int[] cellData)
        {
            /// TRIANGLE BUILDING
            //TODO: In the case of triangles, you don't need a list. # of triangles = # of cells
            List<int> triangles = new List<int>(cellData.Length);
            //int[] triangles;
            int currentOffset, p0, p1, p2, p3;
            for (int i = 0; i < offsetData.Length; i++)
            {
                currentOffset = offsetData[i];
                switch (typeData[i])
                {
                    // VTK_TRIANGLE
                    case 5:
                        p0 = currentOffset - 3;
                        p1 = currentOffset - 2;
                        p2 = currentOffset - 1;
                        triangles.Add(cellData[p0]);
                        triangles.Add(cellData[p1]);
                        triangles.Add(cellData[p2]);
                        //triangles.AddRange(new int[] { , cellData[p1], cellData[p2] });
                        break;
                    // VTK_TETRA
                    case 10:
                        p0 = currentOffset - 4;
                        p1 = currentOffset - 3;
                        p2 = currentOffset - 2;
                        p3 = currentOffset - 1;
                        /// this is very important: matches the VTK coordinate system: see vtk file format
                        /// TODO: This is only relevant for tetrahedrons, not triangles like our retina model
                        triangles.AddRange(new int[] { cellData[p0], cellData[p1], cellData[p2] });
                        triangles.AddRange(new int[] { cellData[p0], cellData[p2], cellData[p3] });
                        triangles.AddRange(new int[] { cellData[p0], cellData[p3], cellData[p1] });
                        triangles.AddRange(new int[] { cellData[p1], cellData[p3], cellData[p2] });
                        break;
                }
            }
            return triangles;
        }
        #endregion
    }
}

