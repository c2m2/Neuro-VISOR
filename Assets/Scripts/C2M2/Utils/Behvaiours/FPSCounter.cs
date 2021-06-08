using UnityEngine;
using System;

namespace C2M2.Utils.DebugUtils
{
    using static Math;
    /// <summary>
    /// Records and stores frame rendering time data over multiple seconds
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        public int sampleSize = 60;
        /// <summary>
        /// Highest FPS over sample size quickly queried from static string array
        /// </summary>
        public string highStr
        {
            get
            {
                return staticNumStrings[Clamp(high, 0, 100)];
            }
        }
        /// <summary>
        /// Average FPS over sample size quickly queried from static string array
        /// </summary>
        public string avgStr
        {
            get
            {
                return staticNumStrings[Clamp(avg, 0, 100)];
            }
        }
        /// <summary>
        /// Lowest FPS over sample size quickly queried from static string array
        /// </summary>
        public string lowStr
        {
            get
            {
                return staticNumStrings[Clamp(low, 0, 100)];
            }
        }
        /// <summary>
        /// Average FPS over the sample size
        /// </summary>
        public int avg;
        /// <summary>
        /// Highest FPS over the sample size
        /// </summary>
        public int high;
        /// <summary>
        /// Lowest FPS over the sample size
        /// </summary>
        public int low;
        private int[] fpsBuffer;
        private int fpsBufferIndex;

        private static string formatString = "High: {0}\nAvg: {1}\nLow: {2}";
        static string[] staticNumStrings = {
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09",
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29",
            "30", "31", "32", "33", "34", "35", "36", "37", "38", "39",
            "40", "41", "42", "43", "44", "45", "46", "47", "48", "49",
            "50", "51", "52", "53", "54", "55", "56", "57", "58", "59",
            "60", "61", "62", "63", "64", "65", "66", "67", "68", "69",
            "70", "71", "72", "73", "74", "75", "76", "77", "78", "79",
            "80", "81", "82", "83", "84", "85", "86", "87", "88", "89",
            "90", "91", "92", "93", "94", "95", "96", "97", "98", "99",
            "100+"
        };

        private void Update()
        {
            if (fpsBuffer == null || fpsBuffer.Length != sampleSize)
            {
                InitializeBuffer();
            }
            UpdateBuffer();
            CalculateFPS();
        }
        private void InitializeBuffer()
        {
            if (sampleSize <= 0) sampleSize = 1;
            fpsBuffer = new int[sampleSize];
            fpsBufferIndex = 0;
        }
        private void UpdateBuffer()
        {
            fpsBuffer[fpsBufferIndex++] = (int)(1f / Time.unscaledDeltaTime);
            if (fpsBufferIndex >= sampleSize) fpsBufferIndex = 0;
        }
        private void CalculateFPS()
        {
            int sum = 0;
            int highest = 0;
            int lowest = int.MaxValue;
            for (int i = 0; i < sampleSize; i++)
            {
                int fps = fpsBuffer[i];
                sum += fps;
                highest = Max(highest, fps);
                lowest = Min(lowest, fps);
            }
            avg = sum / sampleSize;
            high = highest;
            low = lowest;
        }
        public override string ToString() => String.Format(formatString, highStr, avgStr, lowStr);
    }
}