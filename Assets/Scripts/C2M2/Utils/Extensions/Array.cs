using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace C2M2
{
    namespace Utils
    {
        /// <summary>
        /// Utilities with quick but manual array operations for merging, filling, converting to lists, etc.
        /// </summary>
        /// <remarks>
        /// Relevant methods should vastly outperform System.Linq methods
        /// </remarks>
        public static class Array
        {
            /// <summary> Returns a new array with endArray concatenated onto the end of beginningArray </summary>
            public static T[] MergeArrays<T>(this T[] beginningArray, T[] endArray)
            {
                T[] newArray = new T[beginningArray.Length + endArray.Length];
                System.Array.Copy(beginningArray, newArray, beginningArray.Length);
                System.Array.Copy(endArray, 0, newArray, beginningArray.Length, endArray.Length);
                return newArray;
            }
            #region Fill
            /// <summary> Fill a preallocated array of type T with identical values of type T. </summary>
            public static T[] Fill<T>(this T[] array, T value)
            {
                for (int i = 0; i < array.Length; i++) { array[i] = value; }
                return array;
            }
            /// <summary> Fill an unallocated array of type T with identical values of type T. </summary>
            public static T[] Fill<T>(this T[] array, T value, int size)
            {
                array = new T[size];
                return Fill(array, value);
            }
            #endregion
            #region FillArrayRandom
            static Random rand = new Random();
            /// <summary> Fills float array with random numbers from min [inclusive] to max [inclusive] </summary>
            public static void FillArrayRandom(this int[] array, int min, int max) { for (int i = 0; i < array.Length; i++) { array[i] = UnityEngine.Random.Range(min, max); } }
            /// <summary> Fills float array with random numbers from min [inclusive] to max [inclusive] </summary>
            public static void FillArrayRandom(this float[] array, float min, float max) { for (int i = 0; i < array.Length; i++) { array[i] = UnityEngine.Random.Range(min, max); } }
            /// <summary> Fills double array with random numbers from min [inclusive] to max [inclusive] </summary>
            /// TODO: rand.NextDouble() makes a number between 0 and 1, so min is not considered rn
            public static void FillArrayRandom(this double[] array, float min, float max) { for (int i = 0; i < array.Length; i++) { array[i] = rand.NextDouble() * max; } }
            /// <summary> Fills double array with random numbers from min [inclusive] to max [inclusive] </summary>
            public static void FillArrayRandom(this double[] array, double min, double max) => FillArrayRandom(array, (float)min, (float)max);
            #endregion
            #region Converters
            #region Array<->List
            public static int[] ToArray(this List<int> list)
            {
                int[] array = new int[list.Count];
                for (int i = 0; i < array.Length; i++) { array[i] = list[i]; }
                return array;
            }

            public static float[] ToArray(this List<float> list)
            {
                float[] array = new float[list.Count];
                for(int i = 0; i < array.Length; i++) { array[i] = list[i]; }
                return array;
            }

            public static double[] ToArray(this List<double> list)
            {
                double[] array = new double[list.Count];
                for (int i = 0; i < array.Length; i++) { array[i] = list[i]; }
                return array;
            }
            public static Vector3[] ToArray(this List<Vector3> list)
            {
                Vector3[] array = new Vector3[list.Count];
                for (int i = 0; i < array.Length; i++) { array[i] = list[i]; }
                return array;
            }

            public static List<int> ToList(this int[] array)
            {
                List<int> list = new List<int>(array.Length);
                for (int i = 0; i < array.Length; i++) { list.Add(array[i]); }
                return list;
            }

            public static List<float> ToList(this float[] array)
            {
                List<float> list = new List<float>(array.Length);
                for (int i = 0; i < array.Length; i++) { list.Add(array[i]); }
                return list;
            }
            public static List<double> ToList(this double[] array)
            {
                List<double> list = new List<double>(array.Length);
                for (int i = 0; i < array.Length; i++) { list.Add(array[i]); }
                return list;
            }

            #endregion
            /// <summary> Cast an array of doubles into an array of floats </summary>
            public static float[] ToFloat(this double[] array)
            {
                if (array == null) return null;
                float[] floats = new float[array.Length];
                for (int i = 0; i < floats.Length; i++)
                {
                    floats[i] = (float)array[i];
                }
                return floats;
            }
            #endregion
            #region Reverse
            public static int[] Reverse(this int[] array)
            {
                for (int i = 0; i < array.Length / 2; i++)
                {
                    int tmp = array[i];
                    array[i] = array[array.Length - i - 1];
                    array[array.Length - i - 1] = tmp;
                }
                return array;
            }
            public static float[] Reverse(this float[] array)
            {
                for (int i = 0; i < array.Length / 2; i++)
                {
                    float tmp = array[i];
                    array[i] = array[array.Length - i - 1];
                    array[array.Length - i - 1] = tmp;
                }
                return array;
            }
            public static double[] Reverse(this double[] array)
            {
                for (int i = 0; i < array.Length / 2; i++)
                {
                    double tmp = array[i];
                    array[i] = array[array.Length - i - 1];
                    array[array.Length - i - 1] = tmp;
                }
                return array;
            }
            #endregion
            #region Rescale
            /// <summary> Rescale each member of an array from (oldMin, oldMax) to (newMin, newMax) </summary>
            public static void RescaleArray(this float[] array, float newMin, float newMax, float oldMin, float oldMax)
            {              
                if (oldMin == 0 && oldMax == 0)
                { // We cant divide by 0
                    return;
                }
                else
                {
                    float oldRange = (oldMax - oldMin);
                    float newRange = (newMax - newMin);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = (((array[i] - oldMin) * newRange) / oldRange) + newMin;
                    }
                }
            }
            /// <summary> Rescale each member of an array from (oldMin, oldMax) to (newMin, newMax) </summary>
            public static void RescaleArray(this float[] array, float newMin, float newMax) => RescaleArray(array, array.Min(), array.Max(), newMin, newMax);
            #endregion
            #region Times
            /// <summary> Multiplies every value in this array by s </summary>
            public static float[] Times(this float[] array, float s)
            {
                for (int i = 0; i < array.Length; i++) { array[i] *= s; }
                return array;
            }
            /// <summary> Multiplies every value in this array by s </summary>
            public static double[] Times(this double[] array, double s)
            {
                for (int i = 0; i < array.Length; i++) { array[i] *= s; }
                return array;
            }
            #endregion

            #region Subset
            /// <summary>
            /// Returns a subset of this array from [from] (inclusive) to [to] (exclusive)
            /// </summary>
            /// <param name="from"> Beginning index (included in returned array) </param>
            /// <param name="to"> Final index + 1 (not included in returned array) </param>
            /// <returns></returns>
            public static T[] Subset<T>(this T[] array, int to, int from = 0)
            {
                // 1 <= to <= array.Length
                // 0 <= from < to
                if (to < 1 || to > array.Length) throw new IndexOutOfRangeException("to out of bounds.");
                if (from < 0 || from >= to) throw new IndexOutOfRangeException("from out of bounds.");

                T[] subset = new T[to - from];
                int s = 0;
                for(int i = from; i < to; i++)
                {
                    subset[s] = array[i];
                    s++;
                }
                return subset;
            }
            #endregion

            #region Extrema
            /// <summary>
            /// Returns a list of indices of the array/list's local extrema in the order in which they are found.
            /// </summary>
            /// <param name="includeEnds"> If true, also returns the first and last index of the array/list. </param>
            /// <param name="onlyMaxima"> If true, only returns local maxima. </param>
            /// <param name="onlyMinima"> If true, only returns local minima. </param>
            /// <returns></returns>
            public static List<int> GetExtrema(this double[] array, bool includeEnds = false, bool onlyMaxima = false, bool onlyMinima = false)
            {
                List<int> extrema = new List<int>();

                if (includeEnds) extrema.Add(0);

                for(int i = 1; i < array.Length-1; i++)
                {
                    if(array[i] > array[i-1] && array[i] > array[i + 1])
                    { // Found a local maxima
                        // If we only want to keep the minima, don't add a maxima
                        if (onlyMinima == false)
                        {
                            extrema.Add(i);
                        }
                    }else if(array[i] < array[i-1] && array[i] < array[i + 1])
                    { // Found a local minima                          
                       
                        // If we only want to keep the maxima, don't add a minima
                        if (onlyMaxima == false)
                        {
                            extrema.Add(i);
                        }
                    }
                }

                if (includeEnds) extrema.Add(array.Length - 1);

                return extrema;
            }

            /// <summary>
            /// Returns a list of indices of the array/list's local extrema in the order in which they are found.
            /// </summary>
            /// <param name="includeEnds"> If true, also returns the first and last index of the array/list. </param>
            /// <param name="onlyMaxima"> If true, only returns local maxima. </param>
            /// <param name="onlyMinima"> If true, only returns local minima. </param>
            /// <returns></returns>
            public static List<int> GetExtrema(this List<double> list, bool includeEnds = false, bool onlyMaxima = false, bool onlyMinima = false)
            {
                List<int> extrema = new List<int>();

                if (includeEnds) extrema.Add(0);

                for (int i = 1; i < list.Count - 1; i++)
                {
                    if (list[i] > list[i - 1] && list[i] > list[i + 1])
                    { // Found a local maxima
                        // If we only want to keep the minima, don't add a maxima
                        if (onlyMinima == false)
                        {
                            extrema.Add(i);
                        }
                    }
                    else if (list[i] < list[i - 1] && list[i] < list[i + 1])
                    { // Found a local minima                          

                        // If we only want to keep the maxima, don't add a minima
                        if (onlyMaxima == false)
                        {
                            extrema.Add(i);
                        }
                    }
                }

                if (includeEnds) extrema.Add(list.Count - 1);

                return extrema;
            }
            #endregion

            /// <summary>
            /// Returns a list of indices of the array/list's local extrema in the order in which they are found.
            /// </summary>
            /// <param name="includeEnds"> If true, also returns the first and last index of the array/list. </param>
            /// <param name="usePivots"> If true, will use the first index of a found extrema: {0, 0.25, 1, 1, 1, 0.25} would return 2 as one extrema index. </param>
            /// <param name="onlyMaxima"> If true, only returns local maxima. </param>
            /// <param name="onlyMinima"> If true, only returns local minima. </param>
            /// <returns></returns>
            public static List<int> GetExtrema(this List<float> list, bool includeEnds = false, bool usePivots = false, bool onlyMaxima = false, bool onlyMinima = false)
            {
                List<int> extrema = new List<int>();

                if (includeEnds) extrema.Add(0);

                for (int i = 1; i < list.Count - 1; i++)
                {
                    // true if { (i-1), maxima, (i+1) } 
                    bool isMaxima = (list[i] > list[i - 1]) && (list[i] > list[i + 1]);
                    // true if { (i-1), minima, (i+1) } 
                    bool isMinima = (list[i] < list[i - 1]) && (list[i] < list[i + 1]);

                    if (usePivots)
                    {
                        // true if a regular maxima OR { (i-1), maxima, maxima } 
                        isMaxima = isMaxima || ((list[i] > list[i-1]) && (list[i] == list[i+1]));
                        // true if a regular minima OR { (i-1), minima, minima } 
                        isMinima = isMinima || ((list[i] < list[i - 1]) && (list[i] == list[i + 1]));
                    }

                    if (isMaxima)
                    { // Found a local maxima
                        // If we only want to keep the minima, don't add a maxima
                        if (onlyMinima == false)
                        {
                            extrema.Add(i);
                        }
                    }
                    else if (isMinima)
                    { // Found a local minima                          

                        // If we only want to keep the maxima, don't add a minima
                        if (onlyMaxima == false)
                        {
                            extrema.Add(i);
                        }
                    }
                }

                if (includeEnds) extrema.Add(list.Count - 1);

                return extrema;
            }

            public static string ToStringFull(this double[] array)
            {
                StringBuilder sb = new StringBuilder(array.Length * 10);
                for (int i = 0; i < array.Length; i++) { sb.AppendLine(i + " " + array[i]); }
                return sb.ToString();
            }
        }
    }
}
