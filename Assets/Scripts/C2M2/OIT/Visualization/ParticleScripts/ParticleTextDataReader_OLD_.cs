using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace C2M2.OIT {
    [RequireComponent(typeof(MeshFilter))]
    public class ParticleTextDataReader : MonoBehaviour
    {
        public TextAsset assets;

        private string assetString = string.Empty;
        private string currentLine = string.Empty;
        private string[] currentLineSplit = new string[4];

        private Vector3[] newPositions;
        private Color[] newColors;

        public float colorFloatMax = 0f;
        public float colorFloatMin = 0f;

        private float[] colorFloats;

        private StringReader sR;

        //  LinkedList<Color[]> colorArrayList = new LinkedList<Color[]>();
        // LinkedListNode<Color[]> referenceNode;

        // public int[] newTriangles;
        ///UV is only useful if you want to apply textures to the object.  For now it is not necessary but
        ///there are ways to automatically compute UV within Unity.
        // public Vector2[] newUV;
        // We need the normals too

        public void Initialize()
        {
            InitializeDataFile(0);

            InitializeInformationArrays();

            FindColorFloatMaxandMin();
            FillColorArrays();

            ResizeParticleField();

            CloseStringReader(sR);
        }

        public void UpdateColors()
        {
            FillColorArrays();
        }

        //Take in room bounds and resize particle field accordingly
        public void ResizeParticleField()
        {

        }

        private void InitializeInformationArrays()
        {
            //InitializeToPositions();

            //Second number is the total number of points

            int pointsTotalNumber = 125000;
            newPositions = new Vector3[pointsTotalNumber];
            colorFloats = new float[pointsTotalNumber];
            newColors = new Color[pointsTotalNumber];

            int i = 0;
            ///There are three points per line, three coordinates per point.
            ///This reads the text file line by line, takes each line and breaks it into 9 strings,
            ///Takes those 9 strings and converts them to 9 floats while assigning them to vertices.
            ///
            while (i < newPositions.Length)
            {
                currentLine = sR.ReadLine();
                currentLineSplit = currentLine.Split(' ');

                for (int u = 0; u < 4; u += 4)
                {
                    if (!currentLineSplit[u].Equals(string.Empty))
                    {
                        newPositions[i].x = (float.Parse(currentLineSplit[u]));
                    }

                    if (!currentLineSplit[u + 1].Equals(string.Empty))
                    {
                        newPositions[i].y = float.Parse(currentLineSplit[u + 1]);
                    }

                    if (!currentLineSplit[u + 2].Equals(string.Empty))
                    {
                        newPositions[i].z = float.Parse(currentLineSplit[u + 2]);
                    }

                    if (!currentLineSplit[u + 3].Equals(string.Empty))
                    {
                        colorFloats[i] = float.Parse(currentLineSplit[u + 3]);
                    }

                    i++;
                }

            }
        }

        private void FindColorFloatMaxandMin()
        {
            //colorFloatMax = colorFloats.Max();
            //colorFloatMin = colorFloats.Min();

            colorFloatMax = 3;
            colorFloatMin = -3;

            if (colorFloatMax == 0f)
            {
                //approximate to a really small num for division purposes.
                //The result will still produce all 1000 Kelvin elements (The minimum color value)
                colorFloatMax = 0.000000000000000001f;
            }

        }

        private void FillColorArrays()
        {
            int i = 0;

            float adjustedScaleFloat = 0f;

            Debug.Log("colorFloats.Length = " + colorFloats.Length);

            while (i < colorFloats.Length)
            {
                // This scaling method fails if every element is 0.
                adjustedScaleFloat = (((40000 - 1000) * (colorFloats[i] - colorFloatMin) / (colorFloatMax - colorFloatMin)) + 1000);
                newColors[i] = Mathf.CorrelatedColorTemperatureToRGB(adjustedScaleFloat);

                i++;
            }

            i = 0;
        }

        private void InitializeDataFile(int assetIndex)
        {

            //Put text file into string
            assetString = assets.text;

            //Create a StringReader to parse the text file more effectively with Read() and ReadLine()
            sR = new StringReader(assetString);
        }

        private void CloseStringReader(StringReader sR)
        {
            sR.Close();
            assetString = string.Empty;
        }

        public Vector3[] GetPositionsArray()
        {
            return newPositions;
        }

        public Color[] GetColorsArray()
        {
            return newColors;
        }
    }
}