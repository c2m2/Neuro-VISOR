using System.Collections.Generic;
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
            /// <summary> Returns the minimum of a and b </summary>
            public static int Min(int a, int b) => (a < b) ? a : b;
            /// <summary> Returns the minimum of a and b </summary>
            public static float Min(float a, float b) => (a < b) ? a : b;
            /// <summary> Returns the minimum of a and b </summary>
            public static double Min(double a, double b) => (a < b) ? a : b;
            /// <summary> Returns the maximum of a and b </summary>
            public static int Max(int a, int b) => (a > b) ? a : b;
            /// <summary> Returns the maximum of a and b </summary>
            public static float Max(float a, float b) => (a > b) ? a : b;
            /// <summary> Returns the maximum of a and b </summary>
            public static double Max(double a, double b) => (a > b) ? a : b;
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

            #region Array Functions
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
            #region Abs
            /// <summary> Get the absolute value of each array element </summary>
            public static int[] Abs(this int[] array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = array[i] > 0 ? array[i] : -array[i];
                }
                return array;
            }
            /// <summary> Get the absolute value of each array element </summary>
            public static float[] Abs(this float[] array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = array[i] > 0 ? array[i] : -array[i];
                }
                return array;
            }
            /// <summary> Get the absolute value of each array element </summary>
            public static double[] Abs(this double[] array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = array[i] > 0 ? array[i] : -array[i];
                }
                return array;
            }
            #endregion
            #endregion
        }
    }
}
