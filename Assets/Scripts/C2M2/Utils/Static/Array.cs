using System.Collections.Generic;
using System;
using System.Text;

namespace C2M2
{
    namespace Utils
    {
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
            #region FillArray
            /// <summary> Fill a preallocated array of type T with identical values of type T. </summary>
            public static void FillArray<T>(this T[] array, T value)
            {
                for (int i = 0; i < array.Length; i++) { array[i] = value; }
            }
            /// <summary> Fill an unallocated array of type T with identical values of type T. </summary>
            public static void FillArray<T>(this T[] array, T value, int size)
            {
                array = new T[size];
                FillArray(array, value);
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
            #region Max
            /// <summary> Find the maximum value of a given integer array. Should run faster than LINQ.max </summary>
            public static int Max(this int[] array)
            {
                int max = int.MinValue;
                for (int i = 0; i < array.Length; i++) { if (array[i] > max) { max = array[i]; } }
                return max;
            }
            /// <summary> Find the maximum value of a given float array. Should run faster than LINQ.max </summary>
            public static float Max(this float[] array)
            {
                float max = float.MinValue;
                for (int i = 0; i < array.Length; i++) { if (array[i] > max) { max = array[i]; } }
                return max;
            }
            /// <summary> Find the maximum value of a given double array. Should run faster than LINQ.max </summary>
            public static double Max(this double[] array)
            {
                double max = double.MinValue;
                for (int i = 0; i < array.Length; i++) { if (array[i] > max) { max = array[i]; } }
                return max;
            }
            public static int Max(this List<int> list)
            {
                int max = int.MinValue;
                for (int i = 0; i < list.Count; i++) { if (list[i] > max) { max = list[i]; } }
                return max;
            }
            public static float Max(this List<float> list)
            {
                float max = float.MinValue;
                for (int i = 0; i < list.Count; i++) { if (list[i] > max) { max = list[i]; } }
                return max;
            }
            public static double Max(this List<double> list)
            {
                double max = double.MinValue;
                for (int i = 0; i < list.Count; i++) { if (list[i] > max) { max = list[i]; } }
                return max;
            }
            #endregion
            #region Min
            /// <summary> Find the minimum of a given integer array </summary>
            public static int Min(this int[] array)
            {
                int min = int.MaxValue;
                for (int i = 0; i < array.Length; i++) { if (min > array[i]) { min = array[i]; } }
                return min;
            }
            /// <summary> Find the minimum of a given float array </summary>
            public static float Min(this float[] array)
            {
                float min = float.MaxValue;
                for (int i = 0; i < array.Length; i++) { if (min > array[i]) { min = array[i]; } }
                return min;
            }
            /// <summary> Find the minimum of a given double array </summary>
            public static double Min(this double[] array)
            {
                double min = double.MaxValue;
                for (int i = 0; i < array.Length; i++) { if (min > array[i]) { min = array[i]; } }
                return min;
            }
            public static int Min(this List<int> list)
            {
                int min = int.MaxValue;
                for (int i = 0; i < list.Count; i++) { if (list[i] < min) { min = list[i]; } }
                return min;
            }
            public static float Min(this List<float> list)
            {
                float min = float.MaxValue;
                for (int i = 0; i < list.Count; i++) { if (list[i] < min) { min = list[i]; } }
                return min;
            }
            public static double Min(this List<double> list)
            {
                double min = double.MaxValue;
                for (int i = 0; i < list.Count; i++) { if (list[i] < min) { min = list[i]; } }
                return min;
            }
            #endregion
            #region Avg
            /// <summary> Find the average of a given integer array </summary>
            public static int Avg(this int[] array)
            {
                int sum = 0;
                for (int i = 0; i < array.Length; i++) { sum += array[i]; }
                return sum / array.Length;
            }
            /// <summary> Find the average of a given float array </summary>
            public static float Avg(this float[] array)
            {
                float sum = 0;
                for (int i = 0; i < array.Length; i++) { sum += array[i]; }
                return sum / array.Length;
            }
            /// <summary> Find the average of a given double array </summary>
            public static double Avg(this double[] array)
            {
                double sum = 0;
                for (int i = 0; i < array.Length; i++) { sum += array[i]; }
                return sum / array.Length;
            }
            #endregion
            #region Sum
            /// <summary> Sum all elements of a given integer array </summary>
            public static int Sum(this int[] array)
            {
                int sum = 0;
                for (int i = 0; i < array.Length; i++) { sum += array[i]; }
                return sum;
            }
            /// <summary> Sum all elements of a given float array </summary>
            public static float Sum(this float[] array)
            {
                float sum = 0;
                for (int i = 0; i < array.Length; i++) { sum += array[i]; }
                return sum;
            }
            /// <summary> Sum all elements of a given double array </summary>
            public static double Sum(this double[] array)
            {
                double sum = 0;
                for (int i = 0; i < array.Length; i++) { sum += array[i]; }
                return sum;
            }
            #endregion
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

            public static List<int> ToList(this int[] array)
            {
                List<int> list = new List<int>(array.Length);
                for (int i = 0; i < array.Length; i++) { list[i] = array[i]; }
                return list;
            }

            public static List<float> ToList(this float[] array)
            {
                List<float> list = new List<float>(array.Length);
                for (int i = 0; i < array.Length; i++) { list[i] = array[i]; }
                return list;
            }
            public static List<double> ToList(this double[] array)
            {
                List<double> list = new List<double>(array.Length);
                for (int i = 0; i < array.Length; i++) { list[i] = array[i]; }
                return list;
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
            /// <summary> Rescale an array from (oldMin, oldMax) to (newMin, newMax) </summary>
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
            /// <summary> Rescale an array from (oldMin, oldMax) to (newMin, newMax) </summary>
            public static void RescaleArray(this float[] array, float newMin, float newMax) => RescaleArray(array, array.Min(), array.Max(), newMin, newMax);
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
            public static string ToStringFull(this double[] array)
            {
                StringBuilder sb = new StringBuilder(array.Length * 10);
                for (int i = 0; i < array.Length; i++) { sb.AppendLine(i + " " + array[i]); }
                return sb.ToString();
            }
        }
    }
}
