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

        public bool readFromFiles = false;
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

            if (readFromFiles)
            {
                ReadGradients();
            }
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
                gradients = new Gradient[gradFileNames.Length];

                for(int i = 0; i < gradFileNames.Length; i++)
                {
                    gradients[i] = ReadGradient.Read(basePath + subPath + Path.DirectorySeparatorChar + gradFileNames[i] + extension);
                    GradientColorKey[] newKeys = gradients[i].colorKeys;
                }
            }
        }

    }
}