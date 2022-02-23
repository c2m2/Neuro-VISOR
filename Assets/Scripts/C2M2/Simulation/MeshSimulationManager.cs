using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Visualization;
namespace C2M2.Simulation
{
    [RequireComponent(typeof(ColorLUT))]
    public class MeshSimulationManager : MonoBehaviour
    {
        //TODO: We need to enforce unit and unitScaler. That is, we need to ensure that every existing simulation uses the same
        // unit/unitScaler
        /// <summary>
        /// Unit display string that can be manually set by the user
        /// </summary>
        [Tooltip("Unit display string that can be manually set by the user")]
        public string unit = "mV";
        /// <summary>
        /// Can be used to manually convert Gradient Display values to match unit string
        /// </summary>
        [Tooltip("Can be used to manually convert Gradient Display values to match unit string")]
        public float unitScaler = 1000f;

        /// <summary>
        /// Alter the precision of the color scale display
        /// </summary>
        [Tooltip("Alter the precision of the color scale display")]
        public int displayPrecision = 2;

        [Tooltip("Must be set if extremaMethod is set to GlobalExtrema")]
        private float globalMax = float.NegativeInfinity;
        public float GlobalMax
        {
            get { return globalMax; }
            set { if(value > globalMax) globalMax = value; }
        }
        [Tooltip("Must be set if extremaMethod is set to GlobalExtrema")]
        private float globalMin = float.PositiveInfinity;
        public float GlobalMin
        {
            get { return globalMin; }
            set { if (value < globalMin) globalMin = value; }
        }
        private static ColorLUT.ExtremaMethod extremaMethod { get; set; } = ColorLUT.ExtremaMethod.GlobalExtrema;

        public ColorLUT colorLUT { get; private set; } = null;
        protected virtual void Awake()
        {
            colorLUT = GetComponent<ColorLUT>();
            // Initialize the color lookup table
            colorLUT.Gradient = GameManager.instance.defaultGradient;
            colorLUT.extremaMethod = extremaMethod;
            colorLUT.HasChanged = true;
        }
    }
}