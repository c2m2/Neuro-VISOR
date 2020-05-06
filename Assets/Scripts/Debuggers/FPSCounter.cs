using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

namespace C2M2
{
    using static Utilities.MathUtilities;
    /// <summary>
    /// 
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        public int sampleSize = 60;
        public int AverageFPS { get; private set; }
        public int HighestFPS { get; private set; }
        public int LowestFPS { get; private set; }
        public string averageFPSString { get; private set; }
        public string highestFPSString { get; private set; }
        public string lowestFPSString { get; private set; }
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
            UpdateTexts();
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
            AverageFPS = sum / sampleSize;
            HighestFPS = highest;
            LowestFPS = lowest;
        }
        private void UpdateTexts()
        {
            averageFPSString = staticNumStrings[Clamp(AverageFPS, 0, 100)];
            highestFPSString = staticNumStrings[Clamp(HighestFPS, 0, 100)];
            lowestFPSString = staticNumStrings[Clamp(LowestFPS, 0, 100)];
        }
        public override string ToString() => String.Format(formatString, highestFPSString, averageFPSString, lowestFPSString);
    }
}