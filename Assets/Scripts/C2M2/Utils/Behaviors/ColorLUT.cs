using UnityEngine;
using System;
namespace C2M2.Visualization
{
    using Utils;
    using static Utils.Math;
    /// <summary>
    /// Create a fast and memory-friendly color lookup table from a gradient
    /// </summary>
    /// <remarks>
    /// For large applications, like resolving the color of every vertex of a large mesh surface,
    /// this script will be much faster than Gradient.Evaluate().
    /// </remarks>
    public class ColorLUT : MonoBehaviour
    {
        /// <summary>
        /// Should max/min for each time frame be decided by that time frame, a preset 
        /// </summary>
        public enum ExtremaMethod { LocalExtrema, GlobalExtrema, RollingExtrema }
        public ExtremaMethod extremaMethod = ExtremaMethod.RollingExtrema;
        private float globalMax = float.NegativeInfinity;
        public float GlobalMax
        {
            get
            {
                return globalMax;
            }
            set
            {
                globalMax = value;
                HasChanged = true;
            }
        }
        private float globalMin = float.PositiveInfinity;
        public float GlobalMin
        {
            get
            {
                return globalMin;
            }
            set
            {
                globalMin = value;
                HasChanged = true;
            }
        }

        /// <summary>
        /// If true, LUTGradient will reserve memory for the color array the first time Evaluate() is called
        /// </summary>
        public bool poolMemory = true;
        private Color32[] memPool = null;

        private bool hasChanged = false;
        /// <summary>
        /// Flag is set true when globalMin, globalMax, or gradient have changed.
        /// </summary>
        /// <remarks>
        /// Flag must be manually reset to false. See C2M2.Visualization.GradientDisplay for example.
        /// </remarks>
        public bool HasChanged
        {
            get { return hasChanged; }
            set
            {
                hasChanged = value;
            }
        }

        /// <summary>
        /// Resolution of the lookup table. Increase for finer-grained color evaluations
        /// </summary>
        private int lutRes = 256;
        public int LutRes
        {
            get { return lutRes; }
            set
            {
                lutRes = value;
                lut = BuildLUT(gradient, lutRes);
            }
        }

        private Gradient gradient = null;
        public Gradient Gradient
        {
            get { return gradient; }
            set
            {
                gradient = value;
                lut = BuildLUT(gradient, lutRes);
                HasChanged = true;
            }
        }
        // Gradient look-up-table greatly reduces time expense and memory
        private Color32[] lut { get; set; } = null;
        public Color32[] LUT
        {
            get { return lut; }
            set
            {
                lut = value;
                HasChanged = true;
            }
        }


        /// <summary> Given the extrema method, color an entire array of scalers using the LUT </summary>
        public Color32[] Evaluate(float[] unscaledValues) //TODO investigate this more. Performance is bad. Also extrema method and user controlling min and max are problematic
        {
            if (unscaledValues == null || unscaledValues.Length == 0) return null;

            // If we haven't built the LUT yet, and we have a gradient, build the LUT
            if (lut == null)
                lut = BuildLUT(gradient, lutRes);
            // Rescale array based on extrema values
            float[] scaledTimes = RescaleArray(unscaledValues, extremaMethod);

            Color32[] cols;
            if (poolMemory)
            {
                // Initialize the memory pool if necessary
                if(memPool == null || memPool.Length != scaledTimes.Length)
                {
                    memPool = new Color32[scaledTimes.Length];
                }
                cols = memPool;
            }
            else
            {
                cols = new Color32[scaledTimes.Length];
            }

            for (int i = 0; i < scaledTimes.Length; i++)
            {
                cols[i] = lut[Math.Clamp((int)scaledTimes[i], 0, lutRes - 1)];
            }

            return cols;
        }
        /// <summary>
        /// Calculate color of a single value
        /// </summary>
        public Color32 Evaluate(float unscaledValue)
        {
            // If we haven't built the LUT yet, and we have a gradient, build the LUT
            if (lut == null)
                lut = BuildLUT(gradient, lutRes);

            float[] scalars = new float[] { unscaledValue };

            // Todo: this only rescales based on global extrema method
            scalars.RescaleArray(0f, lutRes - 1, GlobalMin, GlobalMax);

            return lut[Math.Clamp((int)scalars[0], 0, lutRes - 1)];
        }

        // Build a LUT without a gradient object by manually assigning color keys
        public Color32[] BuildLUT(Color32[] colorKeys)
        {
            lut = colorKeys;
            return lut;
        }

        private Color32[] BuildLUT(Gradient gradient, int lutRes)
        {
            if(gradient == null)
            {
                throw new NullReferenceException("gradient is null!");
            }

            Color32[] gradientLUT = new Color32[lutRes];
            // Subtract one from lutRes so we can divide across the gradient's range
            lutRes--;

            for (int i = 0; i < lutRes+1; i++)
            {
                gradientLUT[i] = gradient.Evaluate((float)i / lutRes);
            }

            return gradientLUT;
        }

        private float[] RescaleArray(float[] scalars, ExtremaMethod extremaMethod)
        {
            // Rescale based on extrema
            (float, float) minMax = GetMinMax(scalars, extremaMethod);
            scalars.RescaleArray(0f, lutRes - 1, minMax.Item1, minMax.Item2);
            return scalars;
        }

        public (float, float) GetMinMax(float[] scalars, ExtremaMethod extremaMethod)
        {
            switch (extremaMethod)
            {
                case ExtremaMethod.LocalExtrema:
                    return (scalars.Min(), scalars.Max());
                case ExtremaMethod.GlobalExtrema:

                    // The user requested a custom global max, but didn't set one.
                    if (GlobalMax == float.NegativeInfinity || GlobalMin == float.PositiveInfinity)
                    {
                        Debug.LogWarning("Global extrema requested but not preset. Local extrema used instead");
                        GlobalMin = scalars.Min();
                        GlobalMax = scalars.Max();
                    }
                    return (GlobalMin, GlobalMax);
                case ExtremaMethod.RollingExtrema:
                    // If localMax > globalMax, replace globalMax
                    GlobalMax = Max(GlobalMax, scalars.Max());
                    GlobalMin = Min(GlobalMin, scalars.Min());
                    return (GlobalMin, GlobalMax);
                default:
                    return (0, 0);
            }
        }
    }
    public class Gradient32LUTNotFoundException : Exception
    {
        public Gradient32LUTNotFoundException() : base() { }
        public Gradient32LUTNotFoundException(string message) : base(message) { }
        public Gradient32LUTNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
    public class GradientNotFoundException : Exception
    {
        public GradientNotFoundException() : base() { }
        public GradientNotFoundException(string message) : base(message) { }
        public GradientNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}