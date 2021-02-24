using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
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
    public class LUTGradient : MonoBehaviour
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
                hasChanged = true;
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
                hasChanged = true;
            }
        }

        /// <summary>
        /// If true, LUTGradient will reserve memory for the color array the first time Evaluate() is called
        /// </summary>
        public bool poolMemory = true;
        private Color32[] memPool = null;

        /// <summary>
        /// Flag is set true when globalMin, globalMax, or gradient have changed.
        /// </summary>
        /// <remarks>
        /// Flag must be manually reset to false. See C2M2.Visualization.GradientDisplay for example.
        /// </remarks>
        public bool hasChanged { get; set; } = false;

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

        private Gradient gradient;
        public Gradient Gradient
        {
            get { return gradient; }
            set
            {
                gradient = value;
                lut = BuildLUT(gradient, lutRes);
                hasChanged = true;
            }
        }
        // Gradient look-up-table greatly reduces time expense and memory
        public Color32[] lut { get; private set; } = null;

        /// <summary> Given the extrema method, color an entire array of scalers using the LUT </summary>
        public Color32[] Evaluate(float[] unscaledTimes)
        {
            if (unscaledTimes == null || unscaledTimes.Length == 0) return null;

            // If we haven't built the LUT yet, and we have a gradient, build the LUT
            if (lut == null)
                lut = BuildLUT(gradient, lutRes);

            // Store a local pointer so we can manipulate scalers
            //float[] scalars = times;
            // Rescale array based on extrema values
            unscaledTimes = RescaleArray(unscaledTimes, extremaMethod);

            Color32[] cols;
            if (poolMemory)
            {
                // Initialize the memory pool if necessary
                if(memPool == null || memPool.Length != unscaledTimes.Length)
                {
                    memPool = new Color32[unscaledTimes.Length];
                }
                cols = memPool;
            }
            else
            {
                cols = new Color32[unscaledTimes.Length];
            }

            for (int i = 0; i < unscaledTimes.Length; i++)
            {
                cols[i] = lut[Math.Clamp((int)unscaledTimes[i], 0, lutRes - 1)];
            }

            return cols;
        }
        /// <summary>
        /// Calculate color at a given time.
        /// </summary>
        public Color32 Evaluate(float unscaledTime)
        {
            // If we haven't built the LUT yet, and we have a gradient, build the LUT
            if (lut == null)
                lut = BuildLUT(gradient, lutRes);

            float[] scalars = new float[] { GlobalMin, unscaledTime, GlobalMax };

            // Todo: this only rescales based on global extrema method
            scalars.RescaleArray(0f, (lutRes - 1), GlobalMin, GlobalMax);

            Debug.Log("unscaledtime: " + unscaledTime + "\nGlobalMin: " + GlobalMin + "\nGlobalMax: " + GlobalMax + "\nScaledtime: " + scalars[1]);

            return lut[Math.Clamp((int)scalars[1], 0, lutRes-1)];
        }

        private Color32[] BuildLUT(Gradient gradient, int lutRes)
        {
            if(gradient == null)
            {
                throw new NullReferenceException("gradient is null!");
            }

            Color32[] gradientLUT = new Color32[lutRes];
            int maxInd = lutRes;
            // Subtract one from lutRes so we can divide across the gradient's range
            lutRes--;

            for (int i = 0; i < maxInd; i++)
            {
                gradientLUT[i] = gradient.Evaluate((float)i / lutRes);
            }

            return gradientLUT;
        }

        public float oldMin { get; private set; } = 0;
        public float oldMax { get; private set; } = 0;
        private float[] RescaleArray(float[] scalars, ExtremaMethod extremaMethod)
        {
            oldMin = 0;
            oldMax = 0;
            GetMinMax(scalars, extremaMethod);
            // Rescale based on extrema
            scalars.RescaleArray(0f, (lutRes - 1), oldMin, oldMax);
            return scalars;
        }

        public void GetMinMax(float[] scalars, ExtremaMethod extremaMethod)
        {
            switch (extremaMethod)
            {
                case (ExtremaMethod.LocalExtrema):
                    oldMin = scalars.Min();
                    oldMax = scalars.Max();
                    break;
                case (ExtremaMethod.GlobalExtrema):

                    // The user requested a custom global max, but didn't set one.
                    if (GlobalMax == float.NegativeInfinity || GlobalMin == float.PositiveInfinity)
                    {
                        Debug.LogWarning("Global extrema requested but not preset. Local extrema used instead");
                        GlobalMin = scalars.Min();
                        GlobalMax = scalars.Max();
                    }
                    oldMin = GlobalMin;
                    oldMax = GlobalMax;
                    break;
                case (ExtremaMethod.RollingExtrema):
                    // If localMax > globalMax, replace globalMax
                    GlobalMax = Max(GlobalMax, scalars.Max());
                    GlobalMin = Min(GlobalMin, scalars.Min());
                    oldMin = GlobalMin;
                    oldMax = GlobalMax;
                    break;
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