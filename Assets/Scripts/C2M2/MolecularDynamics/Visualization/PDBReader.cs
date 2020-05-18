using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace C2M2
{
    namespace MolecularDynamics.Visualization
    {
        public static class PDBReader
        {
            public static PDBFile ReadFile(in string pdbFilePath)
            {
                // Initialize list object
                List<Vector3> Pos = new List<Vector3>();

                // Attempt to open file as a StreamReader
                if (!File.Exists(pdbFilePath)) { throw new System.Exception("Could not find file " + pdbFilePath); }
                StreamReader reader = new StreamReader(pdbFilePath);

                // Get file name to copy to list name
                string[] fileSplit = pdbFilePath.Split(Path.DirectorySeparatorChar);
                string fileName = fileSplit[fileSplit.Length - 1];

                bool inPos = false;
                // Read file until the end
                //while (reader.Peek() > -1)
		        for (int i = 0; i < 16; i++)
                {
                    // Read the next line of the file
                    string curLine = reader.ReadLine();
	                //Debug.Log(curLine);
                    string[] splitLine = curLine.Split(new char[] {' '},StringSplitOptions.RemoveEmptyEntries); //delimiter is any white space
                    CheckHeader(splitLine);
                    CheckLine(splitLine);            
                }

                PDBFile pdbFile = new PDBFile(Pos.ToArray());

                return pdbFile;

                void CheckHeader(string[] splitLine)
                {
                    if (splitLine[0]=="ATOM")
                    { // Entering atom section
                        inPos = true;
                        //int atomCount = int.Parse(splitLine[1]);
                        Pos.Capacity = 1000;//atomCount;
                    }
                }
                void CheckLine(string[] splitLine)
                {
                    if (inPos)
                    {
                        float x = float.Parse(splitLine[5]);
                        float y = float.Parse(splitLine[6]);
			            float z = float.Parse(splitLine[7]);
			            Pos.Add(new Vector3(x,y,z));
                        /*for (int i = 5; i < 8; i++)
                        {
                            
                            Pos.Add(float.Parse(splitLine[i]));
                        }*/
                    }
                }
            }

        }
        public class PDBFile
        {
            public Vector3[] pos { get; private set; }
            public PDBFile(Vector3[] pos)
            {
                this.pos = pos;
            }
        }
    }
}
