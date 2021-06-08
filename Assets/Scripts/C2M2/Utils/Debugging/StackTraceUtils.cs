using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;

namespace C2M2.Utils
{
    public static class StackTraceUtils
    {
        /// <summary>
        /// Print the current method name.
        /// </summary>
        /// <returns> string containing current method name. </returns>
        public static string GetCurrentMethod()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }
        /// <summary>
        /// Print the current method name.
        /// </summary>
        /// <param name="frameIndex"> frameIndex == 0 would print "GetCurrentMethod", frameIndex == 1 would print the name of the method that you call GetCurrentMethod from, == 2 would print the method which called that method, etc </param>
        /// <returns> String containing current method name. </returns>
        public static string GetCurrentMethod(int frameIndex)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(frameIndex);

            return sf.GetMethod().Name;
        }
    }
}