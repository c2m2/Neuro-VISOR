using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class TextMarker : MonoBehaviour
    {
        public TextMeshProUGUI label = null;
        public NDExtremaController extremaController = null;
        public LineRenderer line = null;
        public float padding = 25;
        public GradientDisplay gradDisplay = null;
        private RectTransform rt;


        private void Awake()
        {
            NullChecks();

            rt = (RectTransform)transform;

            ShiftLabel();

            void NullChecks()
            {
                if (label == null)
                {
                    label = GetComponentInChildren<TextMeshProUGUI>();
                    if (label == null)
                    {
                        Debug.LogError("No label found.");
                        Destroy(this);
                    }
                }
                if (extremaController == null)
                {
                    extremaController = GetComponentInChildren<NDExtremaController>();
                    if (extremaController == null)
                    {
                        Debug.LogError("No extrema controller found.");
                        Destroy(this);
                    }
                }
                if(line == null)
                {
                    line = GetComponentInChildren<LineRenderer>();
                    if(line == null)
                    {
                        Debug.LogError("No line renderer found");
                        Destroy(this);
                    }
                }
            }
        }

        private void Start()
        {
            if(gradDisplay == null)
            {
                gradDisplay = GetComponentInParent<GradientDisplay>();
                if(gradDisplay == null)
                {
                    Debug.LogError("No gradient display found.");
                    Destroy(this);
                }
            }
        }

        // This could be improved by only calling ShiftLabel() when label text changes or when gradient display changes 
        void Update()
        {
            ShiftLabel();
        }
        private void ShiftLabel()
        {
            label.transform.localPosition = new Vector3(
                label.transform.localPosition.x,
                rt.sizeDelta.y + label.bounds.extents.x + padding,
                label.transform.localPosition.z);
        }
    }
}