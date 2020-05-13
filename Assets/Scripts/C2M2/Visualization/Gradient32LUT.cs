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
    public class Gradient32LUT : MonoBehaviour
    {
        /// <summary>
        /// Should max/min for each time frame be decided by that time frame, a preset 
        /// </summary>
        public enum ExtremaMethod { LocalExtrema, GlobalExtrema, RollingExtrema }
        public ExtremaMethod extremaMethod = ExtremaMethod.RollingExtrema;
        public float globalMax = Mathf.NegativeInfinity;
        public float globalMin = Mathf.Infinity;

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
                gradientLUT = BuildLUT(gradient, lutRes);
            }
        }

        private Gradient gradient;
        public Gradient Gradient
        {
            get { return gradient; }
            set
            {
                gradient = value;
                gradientLUT = BuildLUT(gradient, lutRes);
            }
        }
        // Gradient look-up-table greatly reduces time expense and memory
        public Color32[] gradientLUT { get; private set; } = null;

        /// <summary> Given the extrema method, color an entire array of scalers using the LUT </summary>
        public Color32[] Evaluate(in float[] scalars)
        {
            if (scalars == null || scalars.Length == 0) return null;

            // If we haven't built the LUT yet, and we have a gradient, build the LUT
            if (gradientLUT == null && gradient != null) gradientLUT = BuildLUT(gradient, lutRes);

            // Store a local pointer so we can manipulate scalers
            float[] scalarsScaled = scalars;
            // Rescale array based on extrema values
            scalarsScaled = RescaleArray(scalarsScaled, extremaMethod);

            Color32[] colors32 = new Color32[scalarsScaled.Length];
            for (int i = 0; i < scalarsScaled.Length; i++)
            {
                colors32[i] = Evaluate(scalarsScaled[i]);
            }

            return colors32;
        }
        /// <summary>
        /// Calculate color at a given time.
        /// </summary>
        public Color32 Evaluate(float time) => gradientLUT[Clamp((int)time, 0, (lutRes - 1))];

        private Color32[] BuildLUT(Gradient gradient, int lutRes)
        {
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
        private float[] RescaleArray(float[] scalars, ExtremaMethod extremaMethod)
        {
            float oldMin = 0;
            float oldMax = 0;
            switch (extremaMethod)
            {
                case (ExtremaMethod.LocalExtrema):
                    oldMin = scalars.Min();
                    oldMax = scalars.Max();
                    break;
                case (ExtremaMethod.GlobalExtrema):

                    // The user requested a custom global max, but didn't set one.
                    if (globalMax == Mathf.NegativeInfinity || globalMin == Mathf.Infinity)
                    {
                        Debug.LogWarning("Global extrema requested but not preset. Local extrema used instead");
                        globalMin = scalars.Min();
                        globalMax = scalars.Max();
                    }
                    oldMin = globalMin;
                    oldMax = globalMax;
                    break;
                case (ExtremaMethod.RollingExtrema):
                    // If localMax > globalMax, replace globalMax
                    globalMax = Max(globalMax, scalars.Max());
                    globalMin = Min(globalMin, scalars.Min());
                    oldMin = globalMin;
                    oldMax = globalMax;
                    break;
            }
            // Rescale based on extrema
            scalars.RescaleArray(0f, (lutRes - 1), oldMin, oldMax);
            return scalars;
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