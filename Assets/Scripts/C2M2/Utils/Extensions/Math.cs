using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace C2M2
{
    namespace Utils
    {
        /// <summary>
        /// Quick, static methods for finding max, min, and clamped scalar values and arrays
        /// </summary>
        /// <remarks>
        /// Should vastly outperform System.Linq methods
        /// </remarks>
        public static class Math
        {
            #region Min
            public static int Min(int a, int b) => (a < b) ? a : b;
            public static float Min(float a, float b) => (a < b) ? a : b;
            public static double Min(double a, double b) => (a < b) ? a : b;
            #endregion
            #region Max
            public static int Max(int a, int b) => (a > b) ? a : b;
            public static float Max(float a, float b) => (a > b) ? a : b;
            public static double Max(double a, double b) => (a > b) ? a : b;
            public static float Max(this Vector3 a) => Max(Max(a.x, a.y), a.z);
            public static float Min(this Vector3 a) => Min(Min(a.x, a.y), a.z);
            #endregion
            #region Clamp
            /// <summary> 
            /// If value < min, returns min,
            /// If min <= value <= max, returns value,
            /// If value > max, returns max
            /// </summary>
            public static int Clamp(int value, int min, int max) => Max(min, Min(value, max));
            /// <summary> 
            /// If value < min, returns min,
            /// If min <= value <= max, returns value,
            /// If value > max, returns max
            /// </summary>
            public static float Clamp(float value, float min, float max) => Max(min, Min(value, max));
            /// <summary> 
            /// If value < min, returns min,
            /// If min <= value <= max, returns value,
            /// If value > max, returns max
            /// </summary>
            public static double Clamp(double value, double min, double max) => Max(min, Min(value, max));

            /// <summary>
            /// Clamp a Vector3 betwen a min and a max Vector3
            /// </summary>
            public static Vector3 Clamp(this Vector3 value, Vector3 min, Vector3 max) => 
                new Vector3(
                    Clamp(value.x, min.x, max.x),
                    Clamp(value.y, min.y, max.y),
                    Clamp(value.z, min.z, max.z));
            #endregion

            #region Array Functions
            #region Max
            public static int Max(this int[] array)
            {
                int max = int.MinValue;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] > max)
                        max = array[i];
                }
                return max;
            }
            public static float Max(this float[] array)
            {
                float max = float.MinValue;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] > max)
                    {
                        max = array[i];
                    }
                }
                return max;
            }
            public static double Max(this double[] array)
            {
                double max = double.MinValue;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] > max)
                    {
                        max = array[i];
                    }
                }
                return max;
            }
            public static int Max(this List<int> list)
            {
                int max = int.MinValue;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] > max)
                    {
                        max = list[i];
                    }
                }
                return max;
            }
            public static float Max(this List<float> list)
            {
                float max = float.MinValue;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] > max)
                    {
                        max = list[i];
                    }
                }
                return max;
            }
            public static double Max(this List<double> list)
            {
                double max = double.MinValue;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] > max)
                    {
                        max = list[i];
                    }
                }
                return max;
            }
            #endregion
            #region Min
            public static int Min(this int[] array)
            {
                int min = int.MaxValue;
                for (int i = 0; i < array.Length; i++)
                {
                    if (min > array[i])
                    {
                        min = array[i];
                    }
                }
                return min;
            }
            public static float Min(this float[] array)
            {
                float min = float.MaxValue;
                for (int i = 0; i < array.Length; i++)
                {
                    if (min > array[i])
                    {
                        min = array[i];
                    }
                }
                return min;
            }
            public static double Min(this double[] array)
            {
                double min = double.MaxValue;
                for (int i = 0; i < array.Length; i++)
                {
                    if (min > array[i])
                    {
                        min = array[i];
                    }
                }
                return min;
            }
            public static int Min(this List<int> list)
            {
                int min = int.MaxValue;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] < min)
                    {
                        min = list[i];
                    }
                }
                return min;
            }
            public static float Min(this List<float> list)
            {
                float min = float.MaxValue;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] < min)
                    {
                        min = list[i];
                    }
                }
                return min;
            }
            public static double Min(this List<double> list)
            {
                double min = double.MaxValue;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] < min)
                    {
                        min = list[i];
                    }
                }
                return min;
            }
            #endregion
            #region Avg
            public static float Avg(this int[] array)
            {
                int sum = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    sum += array[i];
                }
                return (float)sum / array.Length;
            }
            public static float Avg(this float[] array)
            {
                float sum = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    sum += array[i];
                }
                return sum / array.Length;
            }
            public static double Avg(this double[] array)
            {
                double sum = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    sum += array[i];
                }
                return sum / array.Length;
            }
            public static float Avg(this List<int> list)
            {
                int sum = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    sum += list[i];
                }
                return (float)sum / list.Count;
            }
            public static float Avg(this List<float> list)
            {
                float sum = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    sum += list[i];
                }
                return sum / list.Count;
            }
            public static double Avg(this List<double> list)
            {
                double sum = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    sum += list[i];
                }
                return sum / list.Count;
            }
            #endregion
            #region StdDev
            public static float StdDev(this int[] array)
            {
                float avg = array.Avg();
                float sumSqDiff = 0f;
                for (int i = 0; i < array.Length; i++)
                {
                    sumSqDiff += (array[i] - avg) * (array[i] - avg);
                }
                return Mathf.Sqrt(sumSqDiff / array.Length);
            }
            public static float StdDev(this float[] array)
            {
                float avg = array.Avg();
                float sumSqDiff = 0f;
                for(int i = 0; i < array.Length; i++)
                {
                    sumSqDiff += (array[i] - avg) * (array[i] - avg);
                }
                return Mathf.Sqrt(sumSqDiff / array.Length);
            }
            public static float StdDev(this double[] array)
            {
                double avg = array.Avg();
                double sumSqDiff = 0f;
                for (int i = 0; i < array.Length; i++)
                {
                    sumSqDiff += (array[i] - avg) * (array[i] - avg);
                }
                return Mathf.Sqrt((float)sumSqDiff / array.Length);
            }
            public static float StdDev(this List<int> list)
            {
                float avg = list.Avg();
                float sumSqDiff = 0f;
                for (int i = 0; i < list.Count; i++)
                {
                    sumSqDiff += (list[i] - avg) * (list[i] - avg);
                }
                return Mathf.Sqrt(sumSqDiff / list.Count);
            }
            public static float StdDev(this List<float> list)
            {
                float avg = list.Avg();
                float sumSqDiff = 0f;
                for (int i = 0; i < list.Count; i++)
                {
                    sumSqDiff += (list[i] - avg) * (list[i] - avg);
                }
                return Mathf.Sqrt(sumSqDiff / list.Count);
            }
            public static float StdDev(this List<double> list)
            {
                double avg = list.Avg();
                double sumSqDiff = 0f;
                for (int i = 0; i < list.Count; i++)
                {
                    sumSqDiff += (list[i] - avg) * (list[i] - avg);
                }
                return Mathf.Sqrt((float)sumSqDiff / list.Count);
            }
            #endregion
            #region Sum
            public static int Sum(this int[] array)
            {
                int sum = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    sum += array[i];
                }
                return sum;
            }
            public static float Sum(this float[] array)
            {
                float sum = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    sum += array[i];
                }
                return sum;
            }
            public static double Sum(this double[] array)
            {
                double sum = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    sum += array[i];
                }
                return sum;
            }
            public static int Sum(this List<int> list)
            {
                int sum = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    sum += list[i];
                }
                return sum;
            }
            public static float Sum(this List<float> list)
            {
                float sum = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    sum += list[i];
                }
                return sum;
            }
            public static double Sum(this List<double> list)
            {
                double sum = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    sum += list[i];
                }
                return sum;
            }
            #endregion
            #region Abs
            public static int[] Abs(this int[] array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = array[i] > 0 ? array[i] : -array[i];
                }
                return array;
            }
            public static float[] Abs(this float[] array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = array[i] > 0 ? array[i] : -array[i];
                }
                return array;
            }
            public static double[] Abs(this double[] array)
            {
                for (int i = 0; i < array.Length; i++) array[i] = array[i] > 0 ? array[i] : -array[i];
                return array;
            }
            public static List<int> Abs(this List<int> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = list[i] > 0 ? list[i] : -list[i];
                }
                return list;
            }
            public static List<float> Abs(this List<float> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = list[i] > 0 ? list[i] : -list[i];
                }
                return list;
            }
            public static List<double> Abs(this List<double> list)
            {
                for (int i = 0; i < list.Count; i++) list[i] = list[i] > 0 ? list[i] : -list[i];
                return list;
            }
            #endregion
            #region Rescale
            // when you have value c between a and b and you want a value x between y and z
            // newVal = (oldVal - oldMin) * (newMax - newMin) / (oldMax - oldMin) + oldMin
            // newVal = (oldVal - oldMin) * (newRange) / (oldRange) + oldMin
            public static float[] Rescale(this float[] array, float newMin, float newMax, float oldMin, float oldMax)
            {
                float oldRange = oldMax - oldMin;
                float newRange = newMax - newMin;
                for(int i = 0; i < array.Length; i++)
                {
                    array[i] = (array[i] - oldMin) * (newRange) / (oldRange) + oldMin;
                }
                return array;
            }

            // when you have value c between a and b and you want a value x between y and z
            // newVal = (oldVal - oldMin) * (newMax - newMin) / (oldMax - oldMin) + oldMin
            // newVal = (oldVal - oldMin) * (newRange) / (oldRange) + oldMin
            public static List<float> Rescale(this List<float> array, float newMin, float newMax, float oldMin, float oldMax)
            {
                float oldRange = oldMax - oldMin;
                float newRange = newMax - newMin;
                Debug.Log("Rescaling from (" + oldMin + ", " + oldMax + ") to (" + newMin + ", " + newMax + ")...");
                List<float> newVals = new List<float>(array.Count);
                if (oldRange == 0) Debug.LogError("Old Range is 0, cannot divide by 0!");
                else
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        newVals.Add((array[i] - oldMin) * newRange / oldRange + oldMin);
                    }
                }
                return newVals;
            }
            #endregion
            #endregion



            public static Vector3 Dot(this Vector3 a, Vector3 b)
            {
                return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
            }
            /// <summary>
            /// Run each function in this class and compare the performance to System.Linq's matching methods
            /// </summary>
            public static void CompareToLinq(int numTrials = 1000, int arraySize = 1000, 
                bool testMin = true, bool testMax = true, bool testClamp = true, 
                bool testArrMax = true, bool testArrMin = true, bool testArrAvg = true,
                bool testArrStdDev = true, bool testArrSum = true, bool testArrAbs = true,
                float maxTimeSeconds = 30f)
            {
                // Individual min

                // Individual max

                // Individual clamp

                // Array min

                // Array max

                // Array avg

                // Array StdDev

                // Array sum

                // Array abs
            }
        }
    }
}
