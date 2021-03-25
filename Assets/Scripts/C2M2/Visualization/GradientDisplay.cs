using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
using TMPro;
namespace C2M2.Visualization
{
    public class GradientDisplay : MonoBehaviour
    {
        public LineRenderer gradientLine = null;
        public Gradient gradient;
        public float displayLength = 75;
        public float displayHeight = 10;
        public int numTextMarkers = 5;
        public MeshSimulation sim = null;
        public GameObject textMarkerPrefab = null;
        public GameObject textMarkerHolder = null;
        public TextMeshProUGUI unitText = null;
        public LineRenderer outline = null;
        public float Scaler
        {
            get
            {
                return sim.unitScaler;
            }
        }
        public string Unit
        {
            get
            {
                return sim.unit;
            }
        }
        public string precision = "F4";

        private float LineWidth
        {
            get
            {
                return displayHeight / 20;
            }
        }

        private void Awake()
        {
            // Init gradient display
            if(gradientLine == null)
            {
                gradientLine = GetComponent<LineRenderer>();
                if(gradientLine == null)
                {
                    Debug.LogError("No line renderer found on " + name);
                    Destroy(this);
                }
            }

            StartCoroutine(UpdateDisplayRoutine());
        }

        private IEnumerator UpdateDisplayRoutine()
        {
            // Wait for first frame to render, then run every 0.5 seconds
            yield return new WaitForEndOfFrame();
            while (true)
            {
                try
                {
                    UpdateDisplay();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    // We set hasChanged to be true so that it tries to update the display again
                    sim.ColorLUT.hasChanged = true;
                }
                yield return new WaitUntil(() => sim.ColorLUT.hasChanged == true);
            }
        }

        private void DrawOutline()
        {
            if (outline == null)
            {
                Debug.LogError("No outline found!");
                return;
            }

            outline.positionCount = 4;
            outline.SetPositions(new Vector3[] {
                new Vector3(0f, -displayHeight/2, 0f),
                new Vector3(displayLength, -displayHeight/2, 0f),
                new Vector3(displayLength, displayHeight/2, 0f),
            new Vector3(0f, displayHeight/2, 0f)});
            outline.loop = true;
            outline.startWidth = LineWidth;
            outline.endWidth = LineWidth;
        }

        private void UpdateDisplay()
        {
            // Fetch graddient from simulation's colorLUT
            if (sim.ColorLUT.hasChanged)
            {
                Debug.Log("Updating GradientDisplay...");

                gradient = sim.ColorLUT.Gradient;

                GradientColorKey[] colorKeys = gradient.colorKeys;

                gradientLine.positionCount = colorKeys.Length;
                Vector3[] positions = new Vector3[colorKeys.Length];
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    positions[i] = new Vector3(colorKeys[i].time * displayLength, 0f, 0f);
                }

                gradientLine.SetPositions(positions);

                gradientLine.colorGradient = gradient;

                gradientLine.startWidth = displayHeight;
                gradientLine.endWidth = displayHeight;

                DrawOutline();

                UpdateText();

                sim.ColorLUT.hasChanged = false;
            }
        }
        private void UpdateText()
        {
            if (textMarkerHolder == null) { Debug.LogError("No text marker holder object found."); return; }
            if (textMarkerPrefab == null) { Debug.LogError("No text marker prefab found."); return; }

            // Clear old text markers
            foreach (TextMeshProUGUI marker in textMarkerHolder.GetComponentsInChildren<TextMeshProUGUI>())
            {
                Destroy(marker.gameObject);
            }

            BuildNewMarkers();

            if (unitText != null) unitText.text = Unit;

            void BuildNewMarkers()
            {
                float max = Scaler * sim.ColorLUT.GlobalMax;
                float min = Scaler * sim.ColorLUT.GlobalMin;
                float valueStep = (max - min) / (numTextMarkers - 1);
                float placementStep = displayLength / (numTextMarkers - 1);
                for (int i = 0; i < numTextMarkers; i++)
                {
                    GameObject newMarker = Instantiate(textMarkerPrefab, textMarkerHolder.transform);
                    newMarker.transform.localPosition = new Vector3(i * placementStep, -displayHeight, 0f);

                    // Set label text
                    TextMeshProUGUI labelText = newMarker.GetComponentInChildren<TextMeshProUGUI>();
                    if(labelText == null) Debug.LogError("No text found on marker!");

                    labelText.text = (min + (i * valueStep)).ToString(precision);             

                    // Draw line perpendicular to gradient
                    LineRenderer lr = newMarker.GetComponentInChildren<LineRenderer>();

                    if(lr == null)
                    {
                        Debug.LogError("No line renderer found on text marker prefab!");
                        return;
                    }

                    lr.transform.localPosition = new Vector3(0f, displayHeight / newMarker.transform.localScale.y / 2, 0f);
                    lr.positionCount = 2;
                    lr.SetPositions(new Vector3[] {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, displayHeight, 0f) });

                    // Draw marker line in front of the gradient
                    lr.sortingOrder = gradientLine.sortingOrder + 1;

                    lr.startWidth = LineWidth;
                    lr.endWidth = LineWidth;
                }
            }
        }
    }
}