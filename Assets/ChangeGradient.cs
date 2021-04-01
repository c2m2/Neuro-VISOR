using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using C2M2.Utils;
using TMPro;

namespace C2M2.Visualization
{
    /// <summary>
    /// Can change the color scale of a simulation, as well as read in gradients from text files
    /// </summary>
    public class ChangeGradient : MonoBehaviour
    {
        public GradientDisplay display = null;
        public Gradient[] gradients;

        public bool[] readFromFile;
        public string[] gradFileNames;
        public string basePath = Application.streamingAssetsPath;
        public string extension = ".txt";
        public string subPath = Path.DirectorySeparatorChar + "Gradients";
        public int defaultGrad = 0;
        public int activeGrad = 0;
        public TextMeshProUGUI nameDisplay = null;

        public void NextGrad()
        {
            if (activeGrad + 1 < gradients.Length) activeGrad++;
            else activeGrad = 0; // Wrap around the array

            ApplyGrad();
        }
        public void PrevGrad()
        {
            if (activeGrad - 1 >= 0) activeGrad--;
            else activeGrad = gradients.Length - 1; // Wrap around the array

            ApplyGrad();
        }

        public void ApplyGrad()
        {
            display.sim.ColorLUT.Gradient = gradients[activeGrad];
            if (nameDisplay != null && gradFileNames.Length == gradients.Length)
            {
                nameDisplay.text = gradFileNames[activeGrad];
            }
        }

        private void Awake()
        {
            if (display == null)
            {
                display = GetComponentInParent<GradientDisplay>();
                if (display == null)
                {
                    Destroy(this);
                    Debug.LogError("No GradientDisplay given.");
                }
            }

            ReadGradients();
        }

        private void Start()
        {
            if(defaultGrad >= 0 && defaultGrad < gradients.Length)
            {
                activeGrad = defaultGrad;
            }
            else
            {
                activeGrad = 0;
            }
            ApplyGrad();
        }

        private void ReadGradients()
        {
            if(gradFileNames.Length > 0)
            {
                Gradient[] grads = new Gradient[gradFileNames.Length];

                /// Allows the user to manually override requests to read gradient in order to provide their own.
                /// If an index is requested to be read, the name is used to read the file.
                /// Otherwise the manual overrise is used.
                /// If no read request is given, assumes the file is meant to be read
                if(readFromFile.Length != gradFileNames.Length)
                {
                    bool[] readTemp = new bool[gradFileNames.Length];
                    for(int i = 0; i < readTemp.Length; i++)
                    {
                        readTemp[i] = (i < readFromFile.Length) ? readFromFile[i] : true;
                    }
                    readFromFile = readTemp;
                }

                // Read requested gradietns
                for(int i = 0; i < gradFileNames.Length; i++)
                {
                    if (readFromFile[i])
                    {
                        grads[i] = ReadGradient.Read(basePath + subPath + Path.DirectorySeparatorChar + gradFileNames[i] + extension);
                    }
                    else
                    {
                        if (i < gradients.Length) grads[i] = gradients[i];
                        else Debug.LogError("No manual gradient provided");
                    }
                }
            }
        }

    }
}