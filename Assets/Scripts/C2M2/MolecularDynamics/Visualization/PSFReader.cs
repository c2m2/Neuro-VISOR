using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

namespace C2M2
{
    namespace MolecularDynamics.Visualization
    {
        public static class PSFReader
        {
            public static PSFFile ReadFile(in string psfFilePath)
            {
                // Initialize mesh object and vert, tri list
                Mesh objMesh = new Mesh();
                List<Vector3> verts = new List<Vector3>();
                List<int> triangles = new List<int>();

                List<int> bonds = new List<int>();
                List<int> angles = new List<int>();
                List<float> mass = new List<float>();
                List<string> types = new List<string>();

                // Attempt to open file as a StreamReader
                if (!File.Exists(psfFilePath)) { throw new System.Exception("Could not find file " + psfFilePath); }
                StreamReader reader = new StreamReader(psfFilePath);

                // Get file name to copy to mesh name
                string[] fileSplit = psfFilePath.Split(Path.DirectorySeparatorChar);
                string fileName = fileSplit[fileSplit.Length - 1];

                bool inBonds = false;
                bool inAngles = false;
	            bool inAtoms = false;
                /*
                var lines = File
                   .ReadLines(@"C:\MyFile.txt")
                   .Skip(10)  // skip first 10 lines 
                   .Take(10); // take next 20 - 10 == 10 lines
                */
                // Read file until the end
                while (reader.Peek() > -1)
                {
                    // Read the next line of the file
                    string curLine = reader.ReadLine();
                    string[] splitLine = curLine.Split(new char[] {' '},StringSplitOptions.RemoveEmptyEntries);
                    bool lineIsHeader = CheckHeader(curLine, splitLine);
                    if (!lineIsHeader)
                    {
                        CheckLine(splitLine);
                    }
                }

                PSFFile psfFile = new PSFFile(bonds.ToArray(), angles.ToArray(), mass.ToArray(), types.ToArray());

                return psfFile;

                bool CheckHeader(string curLine, string[] splitLine)
                {
                    if (curLine.Contains("!"))
                    {
                        if (curLine.Contains("!NBOND"))
                        { // Entering bonds section
                            inBonds = true;
                            inAngles = false;
			                inAtoms = false;

                            int bondCount = int.Parse(splitLine[0]) * 2;
                            bonds.Capacity = bondCount;
                            return true;
                        }
                        else if (curLine.Contains("!NTHETA"))
                        {
                            inAngles = true;
                            inBonds = false;
			                inAtoms = false;

                            int thetaCount = int.Parse(splitLine[0]) * 3;
                            angles.Capacity = thetaCount;
                            return true;
                        }
                        else if (curLine.Contains("!NATOM"))
                        {
                            inAtoms = true;
                            inBonds = false;
			                inAngles = false;

                            int atomCount = int.Parse(splitLine[0]);
                            mass.Capacity = atomCount;
                            // One type per atom
                            types.Capacity = atomCount;
                            return true;
                        }
                        else
                        { // We have reached an unsupported action
                            inAngles = false;
                            inBonds = false;
			                inAtoms = false;
                        }
                    }

                    return false;
                }
                void CheckLine(string[] splitLine)
                {
                    if (inBonds)
                    {
                        if (splitLine.Length % 2 == 0)
                        {
                            for (int i = 0; i < splitLine.Length; i++)
                            {
                                bonds.Add(int.Parse(splitLine[i]));
                            }
                        }
                        else throw new IndexOutOfRangeException("Bond line was of length " + splitLine.Length + "; must be divisible by two");
                    }
                    else if (inAngles)
                    {
                        if (splitLine.Length % 3 == 0)
                        {
                            try
                            {
                                for (int i = 0; i < splitLine.Length; i++)
                                {
                                    angles.Add(int.Parse(splitLine[i]));
                                }
                            }catch(Exception e)
                            {
                                string s = "";
                                foreach(string token in splitLine) { s += token + " "; }
                                Debug.LogError("Line: " + s + "; " + e);
                            }
                        }
                        else throw new IndexOutOfRangeException("Angle line was of length " + splitLine.Length + "; must be divisible by three");
                    }
		            else if (inAtoms)
                    {
                        try
                        {
                            mass.Add(float.Parse(splitLine[7]));
                            types.Add(splitLine[5]);
                        }catch(Exception e)
                        {
                            string s = "";
                            foreach(string token in splitLine) { s += token + " "; }
                            //Debug.LogError("Line: " + s + "; " + e);
                        }
                    }
                }
            }

        }
        public class PSFFile
        {
            public int[] bonds { get; private set; }
            public int[] angles { get; private set; }
            public float[] mass { get; private set; }
            public string[] types { get; private set; }
            public PSFFile(int[] bonds, int[] angles, float[] mass, string[] types)
            {
                this.bonds = bonds;
                this.angles = angles;
	            this.mass = mass;
                this.types = types;
            }
        }
    }
}
