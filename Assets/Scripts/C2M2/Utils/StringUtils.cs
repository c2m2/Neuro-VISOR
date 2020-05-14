using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Globalization;

namespace C2M2.Utils
{
    public static class StringUtils
    {
        public static int LastIndexOf(this StringBuilder sb, char find, bool ignoreCase = false, int startIndex = -1, CultureInfo culture = null)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            if (startIndex == -1) startIndex = sb.Length - 1;
            if (startIndex < 0 || startIndex >= sb.Length) throw new ArgumentException("startIndex must be between 0 and sb.Lengh-1", nameof(sb));
            if (culture == null) culture = CultureInfo.InvariantCulture;

            int lastIndex = -1;
            if (ignoreCase) find = Char.ToUpper(find, culture);
            for (int i = startIndex; i >= 0; i--)
            {
                char c = ignoreCase ? Char.ToUpper(sb[i], culture) : (sb[i]);
                if (find == c)
                {
                    lastIndex = i;
                    break;
                }
            }
            return lastIndex;
        }
        /*private static int logCharLimit = 512; // Unity's Debug.Log character limit
        public static void DebugLogAll(string longString)
        {
            int remainingChars = longString.Length;
            if(remainingChars < logCharLimit)
            {
                Debug.Log(longString);
            }
            else
            {
                int currentMult = 0;
                int currentPos = currentMult * logCharLimit;
                while(remainingChars > logCharLimit)
                {
                    Debug.Log(longString.Substring(currentMult*logCharLimit, logCharLimit));
                    currentMult++;
                }

            }
        }*/
    }
}
