namespace C2M2
{
    namespace Utilities
    {
        /// <summary>
        /// Quick, static methods for finding max, min
        /// </summary>
        public static class MathUtilities
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
        }
    }
}
