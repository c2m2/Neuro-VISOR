using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Simulation;
using TMPro;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Visualization;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class GradientDisplay : MonoBehaviour
    {
        public NDBoardController boardController = null;
        public NDSimulationManager SimManager { get { return GameManager.instance.simulationManager; } }
        public ColorLUT ColorLUT { get { return SimManager.colorLUT; } }

        public LineRenderer gradientLine = null;
        public float displayLength = 75;
        public float displayHeight = 10;
        public int numTextMarkers = 5;
        public GameObject textMarkerPrefab = null;
        public GameObject textMarkerHolder = null;
        public TextMeshProUGUI unitText = null;
        public LineRenderer outline = null;

        public float originalMin { get; private set; } = float.PositiveInfinity;
        public float originalMax { get; private set; } = float.NegativeInfinity;

        public TextMarker[] textMarkers = new TextMarker[0];

        public float UnitScaler { get { return SimManager.unitScaler; } }
        public string Unit { get { return SimManager.unit; } }
        public string Precision { get { return "F" + SimManager.displayPrecision; } }

        private float LineWidth
        {
            get
            {
                return displayHeight / 20;
            }
        }

        private void Awake()
        {
            if(boardController == null)
            {
                boardController = GetComponentInParent<NDBoardController>();
                if(boardController == null)
                {
                    Debug.LogError("No simulation controller found.");
                    Destroy(this);
                }
            }
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

        }

        private void Start()
        {
            originalMin = ColorLUT.GlobalMin;
            originalMax = ColorLUT.GlobalMax;

           // StartCoroutine(UpdateDisplayRoutine());
        }

        private void Update()
        {
            if (ColorLUT.HasChanged)
            {
                UpdateDisplay();
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
            if (ColorLUT.HasChanged)
            {
                GradientColorKey[] colorKeys = ColorLUT.Gradient.colorKeys;

                gradientLine.positionCount = colorKeys.Length;
                Vector3[] positions = new Vector3[colorKeys.Length];
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    positions[i] = new Vector3(colorKeys[i].time * displayLength, 0f, 0f);
                }

                gradientLine.SetPositions(positions);

                gradientLine.colorGradient = ColorLUT.Gradient;

                gradientLine.startWidth = displayHeight;
                gradientLine.endWidth = displayHeight;

                DrawOutline();

                UpdateTextMarkers();

                ColorLUT.HasChanged = false;
            }
        }
        public void UpdateTextMarkers()
        {
            if (textMarkerHolder == null) { Debug.LogError("No text marker holder object found."); return; }
            if (textMarkerPrefab == null) { Debug.LogError("No text marker prefab found."); return; }

            float max = UnitScaler * ColorLUT.GlobalMax;
            float min = UnitScaler * ColorLUT.GlobalMin;
            float valueStep = (max - min) / (numTextMarkers - 1);
            float placementStep = displayLength / (numTextMarkers - 1);

            if (textMarkers.Length != numTextMarkers)
            {
                // Destroy old markers if there are any
                if(textMarkers.Length > 0)
                {
                    foreach(TextMarker tm in textMarkers)
                    {
                        Destroy(tm.gameObject);
                    }
                }

                BuildNewMarkers();
            }
            else
            {
                UpdateLabels();
            }

            if (unitText != null) unitText.text = Unit;
            
            void BuildNewMarkers()
            {

                textMarkers = new TextMarker[numTextMarkers];

                for (int i = 0; i < numTextMarkers; i++)
                {
                    GameObject newMarker = Instantiate(textMarkerPrefab, textMarkerHolder.transform);
                    newMarker.transform.localPosition = new Vector3(i * placementStep, 0, 0f);

                    textMarkers[i] = newMarker.GetComponent<TextMarker>();
                    if (textMarkers[i] == null) Debug.LogError("No TextMarker found on Prefab");

                    textMarkers[i].gradDisplay = this;

                    DrawLR(textMarkers[i]);

                    InitExtremaController(textMarkers[i], (i == numTextMarkers-1), (i == 0));
                }

                UpdateLabels();

                void DrawLR(TextMarker tm)
                {

                    tm.line.transform.localPosition = new Vector3(0f, displayHeight / tm.transform.localScale.y / 2, 0f);
                    tm.line.positionCount = 2;
                    tm.line.SetPositions(new Vector3[] {
                        new Vector3(0f, 0f, 0f),
                        new Vector3(0f, -displayHeight, 0f) });

                    // Draw marker line in front of the gradient
                    tm.line.sortingOrder = gradientLine.sortingOrder + 1;

                    tm.line.startWidth = LineWidth;
                    tm.line.endWidth = LineWidth;
                }
                void InitExtremaController(TextMarker tm, bool isMax = false, bool isMin = false)
                {
                    var extrema = tm.extremaController;
                    if (extrema != null)
                    {
                        if (isMin)
                        {
                            extrema.gameObject.SetActive(true);
                            extrema.affectMax = false;
                            tm.name = "MinMarker (" + tm.label.text + ")";
                        }
                        else if (isMax)
                        {
                            extrema.gameObject.SetActive(true);
                            extrema.affectMax = true;
                            tm.name = "MaxMarker (" + tm.label.text + ")";
                        }
                        else
                        {
                            Destroy(extrema.gameObject);
                          //  extrema.gameObject.SetActive(false);
                        }
                    }
                }
            }
            void UpdateLabels()
            {
                for (int i = 0; i < numTextMarkers; i++)
                {
                    SetLabel(textMarkers[i], min + (i * valueStep));
                }
            }

            void SetLabel(TextMarker tm, float val)
            {
                tm.label.text = (val).ToString(Precision);
                tm.name = "Marker (" + tm.label.text + ")";
            }
        }
    }
}