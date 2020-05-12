using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace C2M2.Visualization.VTK
{
    public static class VTUColor
    {
        /// <summary> Helper function to get all colors from a string of components </summary>
        /// <param name="components"> Component data </param>
        /// <returns> color32 array corresponding to component data </returns>
        /// TODO: gradient.evaluate returns a color, and implicitely converting to color32 is taking too long. We need a workaround for this
        public static Color32[] GetVTKColors(float max, float min, float[] components, Gradient gradient)
        {
            Color32[] colors32 = new Color32[components.Length];
            Color maxColor = gradient.Evaluate(1);
            Color minColor = gradient.Evaluate(0);
            Color32 lastUniqueColor;
            float lastUnique = components[0];
            //float prevFloat;
            if (max == min)
            { // For if our data is all the same
                maxColor = minColor;
            }
            if (components[0] == max)
            {
                colors32[0] = maxColor;
                lastUniqueColor = maxColor;
            }
            else if (components[0] == min)
            {
                colors32[0] = minColor;
                lastUniqueColor = minColor;
                // gradient.
            }
            else
            {
                lastUniqueColor = gradient.Evaluate((components[0] - min) / (max - min));
                colors32[0] = lastUniqueColor;
            }
            for (int i = 1; i < colors32.Length; i++)
            { // Cache the most recent unique calculation and if our current calculation is equal, assign the last calculated color         
                if (components[i] == lastUnique)
                {
                    colors32[i] = lastUniqueColor;
                }
                else if (components[i] == max)
                {
                    colors32[i] = maxColor;
                    lastUniqueColor = maxColor;
                    lastUnique = max;
                }
                else if (components[i] == min)
                {
                    colors32[i] = minColor;
                    lastUniqueColor = minColor;
                    lastUnique = min;
                }
                else
                {
                    lastUnique = components[i];
                    float time = (lastUnique - min) / (max - min);
                    if (time > 1 || time < 0)
                    {
                        Debug.Log("Out of range. Math.clamp will be called: " + time);
                    }
                    lastUniqueColor = gradient.Evaluate(time);
                    colors32[i] = lastUniqueColor;
                }
            }
            return colors32;
        }
    }
}
