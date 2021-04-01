using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace C2M2.Utils
{
    /// <summary>
    /// Can read a gradient from a text file.
    /// </summary>
    /// <remarks>
    /// Gradient format is assumed to be as follows by default:
    /// r g b
    /// 
    /// optionally alphas can be given in the format
    /// r g b a
    /// </remarks>
    public static class ReadGradient
    {
        const char delimiter = ' ';

        public static Gradient Read(string fileName, char delimiter = delimiter)
        {
            using (var reader = new StreamReader(fileName))
            {
                List<float> rList = new List<float>();
                List<float> gList = new List<float>();
                List<float> bList = new List<float>();
                List<float> aList = new List<float>();

                ReadFile();

                // Simplify color and alpha list however possible
                SimplifyGradient();

                return BuildGradient(BuildCols(), aList);

                void ReadFile()
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] split = line.Split(delimiter);

                        if (split.Length >= 3)
                        {
                            float r, g, b, a;

                            bool foundR = float.TryParse(split[0], out r);
                            bool foundG = float.TryParse(split[1], out g);
                            bool foundB = float.TryParse(split[2], out b);

                            if (foundR && foundG && foundB)
                            {
                                rList.Add(r);
                                gList.Add(g);
                                bList.Add(b);
                            }

                            // See if there's an a value given
                            if (split.Length == 4)
                            {
                                bool foundA = float.TryParse(split[3], out a);
                                if (foundA && foundR && foundG && foundB)
                                {
                                    aList.Add(a);
                                }
                            }
                        }
                    }

                    if (rList.Count != gList.Count || rList.Count != bList.Count)
                    {
                        string e = "r, b, and g counts do not match. Found "
                            + rList.Count + " r values, "
                            + gList.Count + " g values, "
                            + bList.Count + " b values.";
                        throw new Exception(e);
                    }

                    if (rList.Count < 2)
                    {
                        Debug.LogWarning("Not enough color keys given, reverting to default colors");
                        rList = new List<float>(2);
                        gList = new List<float>(2);
                        bList = new List<float>(2);

                        float[] def = new float[] { 1f, 1f };
                        rList.AddRange(def);
                        gList.AddRange(def);
                        bList.AddRange(def);
                    }
                    if (aList.Count < 2)
                    {
                        aList = new List<float>(2);
                        aList.AddRange(new float[] { 1f, 1f });
                    }
                }
                void SimplifyGradient()
                {
                    int maxSimplifications = 5;
                    
                    if (rList.Count > 8)
                    {
                        string warn = "Gradients can only contain 8 keys, " + fileName + " contains " + rList.Count + ".";
                        while (rList.Count > 8)
                        {
                            if (maxSimplifications > 0)
                            {
                                SimplifyCols();
                                maxSimplifications--;

                                warn += "\nSimplified down to " + rList.Count + " keys.\n";
                            }
                            else
                            {
                                string s = "Gradient " + fileName;
                                for (int k = 0; k < rList.Count; k++)
                                {
                                    s += "\n" + k + ": (" + rList[k] + ", " + gList[k] + ", " + bList[k] + ")";
                                }
                                Debug.Log(s);

                                warn += "\nCould not simplify " + fileName + " to 8 color keys, removing random elements.";
                                while (rList.Count > 8)
                                {
                                    int randInd = UnityEngine.Random.Range(1, rList.Count - 1);
                                    rList.RemoveAt(randInd);
                                    gList.RemoveAt(randInd);
                                    bList.RemoveAt(randInd);
                                }
                            }
                        }
                        Debug.LogWarning(warn);
                    }else if (rList.Count < 2)
                    {
                        Debug.LogWarning("Not enough color keys given, reverting to default colors");
                        rList = new List<float>(2);
                        gList = new List<float>(2);
                        bList = new List<float>(2);

                        float[] def = new float[] { 1f, 1f };
                        rList.AddRange(def);
                        gList.AddRange(def);
                        bList.AddRange(def);
                    }

                    // Simplify alpha array, if necessary
                    if (aList.Count > 8)
                    {
                        string warn = aList.Count + " alpha keys found. Removing random keys until only 8 are given.";
                        while (aList.Count > 8)
                        {
                            int randInd = UnityEngine.Random.Range(1, aList.Count - 1);
                            aList.RemoveAt(randInd);
                        }
                    }
                    else if (aList.Count < 2)
                    {
                        aList = new List<float>(2);
                        aList.AddRange(new float[] { 1f, 1f });
                    }

                    void SimplifyCols()
                    {
                        List<int> rExt = rList.GetExtrema(true, true);
                        // Ends are marked by rExt, so we don't need duplciate keys in g and b
                        List<int> gExt = gList.GetExtrema(false, true);
                        List<int> bExt = bList.GetExtrema(false, true);

                        // Gather all extrema into one unique list
                        List<int> allExtrema = new List<int>(rExt.Count + gExt.Count + bExt.Count);

                        foreach (int ext in rExt)
                        {
                            allExtrema.Add(ext);
                        }
                        foreach (int ext in gExt)
                        {
                            // If this color is already marked as a key by red list, don't add the duplicate
                            if (!allExtrema.Contains(ext))
                            {
                                allExtrema.Add(ext);
                            }
                        }
                        foreach (int ext in bExt)
                        {
                            // If this color is already marked as a key by red OR green list, don't add the duplicate
                            if (!allExtrema.Contains(ext))
                            {
                                allExtrema.Add(ext);
                            }
                        }
                        allExtrema.Sort();

                        /// "Merge" the extrema lists s.t. each color list contains it's value at its extrema 
                        /// AND it's color value at other colors' extremas
                        List<float> rTemp = new List<float>(allExtrema.Count);
                        List<float> gTemp = new List<float>(allExtrema.Count);
                        List<float> bTemp = new List<float>(allExtrema.Count);
                        foreach (int ext in allExtrema)
                        {
                            rTemp.Add(rList[ext]);
                            gTemp.Add(gList[ext]);
                            bTemp.Add(bList[ext]);
                        }

                        rList = rTemp;
                        gList = gTemp;
                        bList = bTemp;
                    }
                }
                
                Color[] BuildCols()
                {
                    Color[] colors = new Color[rList.Count];

                    for(int i = 0; i < colors.Length; i++)
                    {
                        colors[i] = new Color(rList[i], gList[i], bList[i]);
                    }

                    return colors;
                }

                Gradient BuildGradient(Color[] colors, List<float> alphas)
                {
                    // Apply colors and alphas
                    Gradient gradient = new Gradient();

                    gradient.SetKeys(BuildColorKeys(), BuildAlphaKeys());
                    return gradient;

                    GradientColorKey[] BuildColorKeys()
                    {
                        GradientColorKey[] colKeys = new GradientColorKey[colors.Length];
                        for (int i = 0; i < colors.Length; i++)
                        {
                            float time = (float)i / (colors.Length - 1);
                            colKeys[i] = new GradientColorKey(colors[i], time);
                        }

                        return colKeys;
                    }

                    GradientAlphaKey[] BuildAlphaKeys()
                    {
                        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[alphas.Count];

                        for (int i = 0; i < alphaKeys.Length; i++)
                        {
                            float time = (float)i / (alphas.Count - 1);
                            alphaKeys[i] = new GradientAlphaKey(alphas[i], time);
                        }

                        return alphaKeys;
                    }
                }
            }
        }
    }
}