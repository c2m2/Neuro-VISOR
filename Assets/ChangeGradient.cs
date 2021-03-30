using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using C2M2.Utils;

namespace C2M2.Visualization
{
    /// <summary>
    /// Can change the color scale of a simulation, as well as read in gradients from text files
    /// </summary>
    public class ChangeGradient : MonoBehaviour
    {
        public Gradient[] gradients;
        public string[] gradFileNames;
        public string basePath = Application.streamingAssetsPath;
        public string extension = ".txt";
        public string subPath = Path.DirectorySeparatorChar + "Gradients";

        private void Awake()
        {
            ReadGradients();

        }

        private void ReadGradients()
        {
            if(gradFileNames.Length > 0)
            {
                gradients = new Gradient[gradFileNames.Length];

                for(int i = 0; i < gradFileNames.Length; i++)
                {
                    gradients[i] = ReadGradient.Read(basePath + subPath + Path.DirectorySeparatorChar + gradFileNames[i] + extension);
                    string s = "Gradient " + gradFileNames[i] + ": ";
                    GradientColorKey[] newKeys = gradients[i].colorKeys;
                    for(int j = 0; j < newKeys.Length; j++)
                    {
                        s += "\n" + newKeys[j].color + ", time: " + newKeys[j].time;
                    }
                    Debug.Log(s);
                }
            }
        }

    }
}