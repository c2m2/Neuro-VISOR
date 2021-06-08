using UnityEngine;
using System;
namespace C2M2.Interaction.Adjacency
{
    /// <summary> Find Gaussian curve values in 3D space at a given distance from origin </summary>
    [System.Serializable]
    public class GaussianDistanceCalculator : MonoBehaviour
    {
        /// <summary> height of the curve </summary>
        public double height { get; set; } = 1;
        /// <summary> Standard deviation of the curve </summary>
        private double stdDev = 0.01;
        public double StdDev
        {
            get { return stdDev; }
            set
            {
                stdDev = value;
                stdDevSq = stdDev * stdDev;
                stdDevCb = stdDevSq * stdDev;
                twoXstdDevSq = 2 * stdDevSq;
            }
        }
        #region ShortcutVariables
        /// <summary> = 2.7182818284590451 </summary>
        private static double e = Math.E;
        /// <summary> = stdDev^2 </summary>
        private double stdDevSq = 1;
        /// <summary> = 2 * stdDev^2 </summary>
        private double twoXstdDevSq = 2;
        /// <summary> = stdDev^3 </summary>
        private double stdDevCb = 1;
        #endregion
        /// <summary> Gaussian constructor </summary>
        /// <param name="height"> Height of the curve's peak </param>
        /// <param name="stdDev"> The standard deviation, sometimes called the Gaussian RMS width. Controls the width of the "bell" </param>
        public void SetGaussian(double height, double stdDev)
        {
            this.height = height;
            StdDev = stdDev;
        }
        /// <summary> Find the probability of getting a value == distance </summary>
        /// <param name="distance"> distance from the origin/mean </param> 
        /// <returns> value of gaussian function at given distance </returns>
        /// Represents the function:
        /// 
        ///           height
        ///    ________________________                  
        ///             distance^2
        ///       ________________________
        ///                             
        ///        2 * standardDeviation^2
        ///     e
        ///     
        public double CalculateGaussian(double distance) => CalculateGaussianDistSq(distance * distance);
        public double CalculateGaussian(float dist) => CalculateGaussian((double)dist);
        /// <summary> Identitcal to CalculateGaussian, but provide the distance squared beforehand to speed calculation</summary>
        /// <param name="distSq"> distance from the origin squared </param>
        /// <returns> value of gaussian function at given distance </returns>
        public double CalculateGaussianDistSq(double distSq)
        {
            return height / Math.Pow(e, (distSq / twoXstdDevSq));
        }
        /// <summary>
        /// CalculateGaussian solved for distance
        /// </summary>
        /// <param name="probability"> quantile must be greater than 0 and less than 1, noninclusive.  </param>
        /// <returns> Distance d at which CalculateGaussian(d) == probability, scaled by StdDev </returns>
        public float DistanceThreshold(float probability)
        {
            if (probability <= 0f) { probability = 0.00000001f; }
            else if (probability >= 1f) { probability = 0.99999999f; }
            return (float)StdDev * Mathf.Sqrt(2 * Mathf.Log(1 / probability));
        }
    }
}