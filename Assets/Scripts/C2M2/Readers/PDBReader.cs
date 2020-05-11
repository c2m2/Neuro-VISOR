using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace C2M2
{
    namespace Readers
    {
        public static class PDBReader
        {
            public static Sphere[] ReadFile(in string pdbFilePath)
            {
                // Initialize mesh object and vert, tri list
                Mesh objMesh = new Mesh();
                List<Vector3> verts = new List<Vector3>();
                List<int> triangles = new List<int>();
                // Attempt to open file as a StreamReader
                if (!File.Exists(pdbFilePath)) { throw new System.Exception("Could not find file " + pdbFilePath); }
                StreamReader reader = new StreamReader(pdbFilePath);
                // Get file name to copy to mesh name
                string[] fileSplit = pdbFilePath.Split(Path.DirectorySeparatorChar);
                string fileName = fileSplit[fileSplit.Length - 1];
                // Read file until the end
                while (reader.Peek() > -1)
                {
                    // Read the next line of the file
                    string curLine = reader.ReadLine();
                    string[] splitLine = curLine.Split(' ');
                    switch (splitLine[0])
                    {
                        case "v": verts.Add(ReadVertex(splitLine)); break;
                        case "f": triangles.AddRange(ReadTriangle(splitLine)); break;
                        // Ignore comments
                        case "#": break;
                        default: throw new Exception("OBJReadFile can only accept vertex and face lines");
                    }
                }
                objMesh.vertices = verts.ToArray();
                objMesh.triangles = triangles.ToArray();
                objMesh.name = fileName;
                if (objMesh.normals.Length == 0) { objMesh.RecalculateNormals(); }
                objMesh.RecalculateBounds();
                return null;
            }
            private static Vector3 ReadVertex(in string[] splitLine)
            {
                if (splitLine.Length >= 4)
                {
                    float x = 0f, y = 0f, z = 0f;
                    for (int i = 1; i < 4; i++)
                    {
                        try
                        {
                            switch (i)
                            {
                                case 1: x = Single.Parse(splitLine[i]); break;
                                case 2: y = Single.Parse(splitLine[i]); break;
                                case 3: z = Single.Parse(splitLine[i]); break;
                                default: throw new IndexOutOfRangeException();
                            }
                        }
                        catch (FormatException) { throw new FormatException(splitLine[i] + " is not in a valid format to be a Single."); }
                        catch (OverflowException) { throw new OverflowException(splitLine[i] + " is outside the range of a Single."); }
                        catch (ArgumentNullException) { throw new ArgumentNullException(splitLine[i] + " is null"); }
                    }
                    return new Vector3(x, y, z);
                }
                else { throw new Exception("Line must be length 4. Line length: " + splitLine.Length); }
            }
            private static int[] ReadTriangle(in string[] splitLine)
            {
                // Save space for our new triangle indices
                int[] newTris = new int[3];
                for (int i = 1; i < 4; i++)
                {
                    try
                    {
                        switch (i)
                        {
                            case 1: newTris[0] = int.Parse(splitLine[i]) - 1; break;    //OBJ indices start at 1
                            case 2: newTris[1] = int.Parse(splitLine[i]) - 1; break;
                            case 3: newTris[2] = int.Parse(splitLine[i]) - 1; break;
                            default: throw new IndexOutOfRangeException();
                        }
                    }
                    catch (FormatException)
                    { // Faces might have unintended decimals. Try to read them as a float instead
                        Vector3 floatTris = ReadVertex(splitLine);
                        newTris[0] = ((int)floatTris.x) - 1;
                        newTris[1] = ((int)floatTris.y) - 1;
                        newTris[2] = ((int)floatTris.z) - 1;
                    }
                    catch (OverflowException) { throw new OverflowException(splitLine[i] + " is outside the range of an int."); }
                    catch (ArgumentNullException) { throw new ArgumentNullException(splitLine[i] + " is null"); }
                }
                return newTris;
            }
        }
    }
}