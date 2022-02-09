using C2M2.Simulation;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace C2M2.Interaction
{
    [RequireComponent(typeof(List<Canvas>))]
    public class RulerMeasure : MonoBehaviour
    {
        public List<Canvas> measurementDisplays;
        public GameObject topEndcap;
        public GameObject bottomEndCap;
        private readonly List<int> potentialIntervals = new List<int> { 1, 2, 5, 10, 20, 50, 100, 200, 500 };
        private List<MarkedDisplay> markedDisplays = new List<MarkedDisplay>();
        private readonly int markerCount = 100; //Maximum number of markers
        private float initialTopEndCapLength;
        private float initialBottomEndCapLength;
        private float initialRulerLength;
        private float prevRulerLength = 0;
        private float scaledRulerLength;
        private string units;

        // Start is called before the first frame update
        void Start()
        {
            initialTopEndCapLength = topEndcap.transform.localScale.y;
            initialBottomEndCapLength = bottomEndCap.transform.localScale.y;
            initialRulerLength = transform.lossyScale.z;
            CreateMarkers();
        }

        // Update is called once per frame
        void Update()
        {
            GameObject simulationSpace = GameManager.instance.simulationSpace;
            if (simulationSpace != null)
            {
                float rulerLength = transform.lossyScale.z / simulationSpace.transform.lossyScale.z;
                if (prevRulerLength != rulerLength && Math.Abs((prevRulerLength - rulerLength)/prevRulerLength) >= .005) /// length change must be greater than 0.5% to update
                {
                    float markerSpacing = 0.03f; ///< minimum spacing between each marker and beginning and end of ruler
                    float markerSpacingPercent = markerSpacing * initialRulerLength / transform.lossyScale.z; ///< minimum spacing between each marker and beginning and end of ruler in percent of ruler's length
                    float firstMarkerLength = markerSpacingPercent * rulerLength;

                    int magnitude = GetMagnitude(firstMarkerLength*2); //multiplication by 2 ensures that markers above 500 get treated as the next unit up

                    units = " " + GetUnit(magnitude);

                    int siPrefixGroup = (int)Math.Floor(magnitude / 3.0);
                    // scaledFirstMarkerLength is a scaled version of firstMarkerLength so it is between .5 and 500
                    float scaledFirstMarkerLength = (float)(firstMarkerLength / Math.Pow(10, siPrefixGroup * 3));
                    scaledRulerLength = (float)(rulerLength / Math.Pow(10, siPrefixGroup * 3));
                    UpdateMarkers(scaledFirstMarkerLength);
                    UpdateEndCaps();
                    prevRulerLength = rulerLength;
                }
            }
        }

        /// <returns>Integer of number of zeros of base 10 magnitude of number</returns>
        private int GetMagnitude(float number)
        {
            double lengthLog10 = Math.Log10(number);
            return Convert.ToInt32(Math.Floor(lengthLog10));
        }

        /// <param name="magnitude">Integer of number of zeros of base 10 magnitude of number</param>
        /// <returns>String of the SI unit and prefix</returns>
        private string GetUnit(int magnitude)
        {
            if (magnitude < -3)
            {
                // takes the magnitude and puts it in terms of nm by adding three. Then divides by 3 to get unit group and rounds down.
                int eTerm = (magnitude + 3) / 3;
                return "e" + 3 * eTerm + " nm";
            }
            else if (magnitude < 0) return "nm";
            else if (magnitude < 3) return "μm";
            else if (magnitude < 6) return "mm";
            else if (magnitude < 9) return "m";
            else if (magnitude <= 12) return "km";
            else if (magnitude > 12)
            {
                // takes the magnitude and puts it in terms of km by subtracting twelve. Then divides by 3 to get unit group and rounds down.
                int eTerm = (magnitude - 12) / 3;
                return "e" + 3 * eTerm + " km";
            }
            else
            {
                Debug.LogError("Invalid length inputted");
                return null;
            }
        }

        private void CreateMarkers()
        {
            foreach (Canvas measurementDisplay in measurementDisplays)
            {
                List<TextMeshProUGUI> markers = new List<TextMeshProUGUI>();
                for (int i = 0; i < markerCount; i++)
                {
                    GameObject gObj = new GameObject();
                    gObj.transform.SetParent(measurementDisplay.transform);
                    TextMeshProUGUI markerText = gObj.AddComponent<TextMeshProUGUI>();
                    markerText.alignment = TextAlignmentOptions.Center;
                    markerText.rectTransform.localPosition = new Vector3(0, 0, 0);
                    markerText.fontSize = 0.03f;
                    markerText.color = Color.black;
                    gObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    gObj.transform.localScale = Vector3.one;
                    markers.Add(markerText);
                }
                MarkedDisplay markedDisplay = new MarkedDisplay(measurementDisplay, markers);
                markedDisplays.Add(markedDisplay);
            }
        }

        /// <summary>
        /// Updates the locations of the markers on the ruler
        /// </summary>
        /// <param name="minimumMarkerNumber">Minimum valid number for a marker to be placed on the ruler, needs to be between the lowest and the highest number in potentialIntervals</param>
        private void UpdateMarkers(float minimumMarkerNumber)
        {
            int interval = 0;
            int currentNumber = 0;
            while (interval == 0 && currentNumber < potentialIntervals.Count)
            {
                if (potentialIntervals[currentNumber] > minimumMarkerNumber)
                {
                    interval = potentialIntervals[currentNumber];
                }
                currentNumber++;
            }

            foreach (MarkedDisplay markedDisplay in markedDisplays)
            {
                int markerNumber = 0;
                for (int i = 0; i <= scaledRulerLength; i += interval)
                {
                    float rulerPoint = (i / scaledRulerLength) - 0.5f;

                    if (markedDisplay.markers.Count <= markerNumber)
                    {
                        //Occurs when there are more markers then the preset limit
                        break;
                    }

                    if (i == 0)
                    {
                        markedDisplay.markers[markerNumber].text = "‒ " + i + " " + units + " ‒";
                    }
                    else
                    {
                        markedDisplay.markers[markerNumber].text = "‒ " + i + " ‒";
                    }

                    markedDisplay.markers[markerNumber].rectTransform.localPosition = new Vector3(rulerPoint, 0, 0);
                    markedDisplay.markers[markerNumber].transform.localScale = new Vector3(markedDisplay.markers[markerNumber].transform.localScale.x, initialRulerLength / transform.lossyScale.z, markedDisplay.markers[markerNumber].transform.localScale.z);

                    markedDisplay.markers[markerNumber].gameObject.SetActive(true);
                    markerNumber++;
                }
                while (markerNumber < markedDisplay.markers.Count)
                {
                    markedDisplay.markers[markerNumber].gameObject.SetActive(false);
                    markerNumber++;
                }

            }
        }


        /// <summary>
        /// Ensures the EndCaps stay their initial size when the ruler is extended or shrunk
        /// </summary>
        private void UpdateEndCaps()
        {
            if (topEndcap != null)
            {
                topEndcap.transform.localScale = new Vector3(topEndcap.transform.localScale.x, initialTopEndCapLength * (initialRulerLength / transform.lossyScale.z), topEndcap.transform.localScale.z);
                topEndcap.transform.localPosition = new Vector3(0, 0, -(0.5f + topEndcap.transform.localScale.y / 2));
            }

            if (bottomEndCap != null)
            {
                bottomEndCap.transform.localScale = new Vector3(bottomEndCap.transform.localScale.x, initialBottomEndCapLength * (initialRulerLength / transform.lossyScale.z), bottomEndCap.transform.localScale.z);
                bottomEndCap.transform.localPosition = new Vector3(0, 0, 0.5f + bottomEndCap.transform.localScale.y / 2);
            }
        }

        private class MarkedDisplay
        {
            public Canvas display;
            public List<TextMeshProUGUI> markers;

            public MarkedDisplay(Canvas display, List<TextMeshProUGUI> markers)
            {
                this.display = display;
                this.markers = markers;
            }
        }
    }
}