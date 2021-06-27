using UnityEngine;

namespace C2M2.Utils
{
    public static class LineRendUtils
    {
        /// <summary> Set both endpoint positions of the line renderer at once. </summary>
        /// <returns> True if successful </returns>
        /// <remarks> Designed for use with line renderers with two points, but safe to us on any line renderer </remarks>
        public static bool SetEndpointPositions(this LineRenderer lineRend, Vector3 startPosition, Vector3 endPosition)
        {
            if (lineRend == null || !(lineRend.positionCount > 1)) return false;
            // If the line renderer exists and has at least two points, set endpoint positions
            lineRend.SetPosition(0, startPosition);
            lineRend.SetPosition(lineRend.positionCount - 1, endPosition);
            return true;
        }
        /// <summary> Set both endpoint colors at once </summary>
        /// <returns> True if successful </returns>
        public static bool SetEndpointColors(this LineRenderer lineRend, Color startColor, Color endColor)
        {
            if (lineRend == null || !(lineRend.positionCount > 1)) return false;
            lineRend.startColor = startColor;
            lineRend.endColor = endColor;
            return true;
        }
        /// <summary> Set both endpoint colors to be the same color at once </summary>
        /// <returns> True if successful </returns>
        public static bool SetEndpointColors(this LineRenderer lineRend, Color color) => SetEndpointColors(lineRend, color, color);
    }
}