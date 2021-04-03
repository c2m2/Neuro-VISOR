using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Utils;
using TMPro;
using UnityEngine.UI;
namespace C2M2.Interaction.UI
{
    public class NDExtremaController : MonoBehaviour
    {
        /// <summary>
        /// If true, changes simulation's GlobalMax. Otherwise changes GlobalMin
        /// </summary>
        public GradientDisplay gradDisplay = null;
        public TextMeshProUGUI label = null;
        public RectTransform increaseButton = null;
        public RectTransform decreaseButton = null;
        public RectTransform resetButton = null;
        public float buttonSize = 25;
        public bool affectMax = true;
        public float shiftSensivitivty = 100f;
        public bool latchToInt = true;

        private float GlobalMax
        {
            get
            {
                return gradDisplay.sim.ColorLUT.GlobalMax;
            }
            set
            {
                if (value > GlobalMin)
                {
                    gradDisplay.sim.ColorLUT.GlobalMax = value;
                }
            }
        }
        private float GlobalMin
        {
            get
            {
                return gradDisplay.sim.ColorLUT.GlobalMin;
            }
            set
            {
                if (value < GlobalMax)
                {
                    gradDisplay.sim.ColorLUT.GlobalMin = value;
                }
            }
        }

        private float fi = -1;
        private float startTime = 0;
        private float maxHoldTime = 5.0f;
        private float ff = -1;

        private void Awake()
        {
            NullChecks();

            void NullChecks()
            {
                bool fatal = false;
                if (gradDisplay == null)
                {
                    gradDisplay = GetComponentInParent<GradientDisplay>();
                    if (gradDisplay == null)
                    {
                        Debug.LogError("No gradient display found");
                        fatal = true;
                    }
                }
                if(label == null)
                {
                    label = GetComponentInChildren<TextMeshProUGUI>();
                    if(label == null)
                    {
                        Debug.LogError("No label found.");
                        fatal = true;
                    }
                }
                if(increaseButton == null)
                {
                    Debug.LogError("No increase button given.");
                    fatal = true;
                }
                if(decreaseButton == null)
                {
                    Debug.LogError("No decrease button given.");
                    fatal = true;
                }
                if(resetButton == null)
                {
                    Debug.LogError("No reset button given.");
                    fatal = true;
                }

                if (fatal) Destroy(this);
            }
        }
        private void Start()
        {
            Image[] buttons = GetComponentsInChildren<Image>();
            foreach(Image b in buttons)
            {
                Vector3 size = b.rectTransform.sizeDelta;
                b.transform.localScale = new Vector3(buttonSize / size.x, buttonSize / size.y, 1f);
            }
            BoxCollider[] cols = GetComponentsInChildren<BoxCollider>();
            foreach(BoxCollider b in cols)
            {
                b.size = new Vector3(buttonSize, buttonSize, 1f);
            }

            PositionButtons();

            ResetScaler();
        }
        private void Update()
        {
            PositionButtons();
        }

        private void PositionButtons()
        {
            // Reposition buttons to the left of text
            float y = label.bounds.max.x + label.transform.localPosition.y + buttonSize;
            transform.localPosition = new Vector3(label.transform.localPosition.x, label.transform.localPosition.y, label.transform.localPosition.z);

            float labelHeight = label.bounds.extents.y;
            float labelWidth = label.bounds.extents.x;
            increaseButton.localPosition = new Vector3(label.bounds.max.y + buttonSize, 0f, 0f);
            decreaseButton.localPosition = new Vector3(label.bounds.min.y - buttonSize, 0f, 0f);
            resetButton.localPosition = new Vector3(0f, label.bounds.extents.x + buttonSize, 0f);
        }

        private void ScaleExtremaPress(float sign)
        {
            float pressAmt = 2 * (GlobalMax - GlobalMin) / shiftSensivitivty;
            SetExtrema(sign * pressAmt);

            PositionButtons();

            startTime = Time.unscaledTime;
        }
        public void IncreaseExtremaPress() => ScaleExtremaPress(1);
        public void DecreaseExtremaPress() => ScaleExtremaPress(-1);

        private void ScaleExtremaHold(float sign)
        {
            float holdTime = Time.unscaledTime - startTime;

            SetExtrema(sign * Time.fixedDeltaTime * GetScaler(Math.Min(holdTime, maxHoldTime)));

            PositionButtons();
        }
        public void IncreaseExtremaHold() => ScaleExtremaHold(1);
        public void DecreaseExtremaHold() => ScaleExtremaHold(-1);
      
        private void SetExtrema(float val)
        {
            if (affectMax) GlobalMax += val;
            else GlobalMin += val;
        }
        public void ResetExtrema()
        {
            if (affectMax) GlobalMax = gradDisplay.originalMax;
            else GlobalMin = gradDisplay.originalMin;
        }

        public void ResetScaler()
        {
            if (latchToInt)
            {
                // If we operate from 0.000 to 0.055, we cannot round to an int.
                // We need to convert to the display value (0 to 55), round, and then convert back
                float val = affectMax ? GlobalMax : GlobalMin;
                val = Mathf.Round(val * gradDisplay.UnitScaler) / gradDisplay.UnitScaler;

                if (affectMax)
                {
                    // Don't let max round to min value
                    if(val == GlobalMin)
                    {
                        val = (val * gradDisplay.UnitScaler);
                        val++;
                        val = val / gradDisplay.UnitScaler;
                    }
                    GlobalMax = val;
                }
                else
                {
                    // Don't let min round to max value
                    if (val == GlobalMax)
                    {
                        val = (val * gradDisplay.UnitScaler);
                        val--;
                        val = val / gradDisplay.UnitScaler;
                    }
                    GlobalMin = val;
                }
            }

            startTime = float.NegativeInfinity;
            //fi = 2 * (gradDisplay.originalMax - gradDisplay.originalMin) / shiftSensivitivty;
            fi = 0;
            ff = GlobalMax - GlobalMin;
        }

        // fi = 2 * (Max - Min) / sensistivity
        // ff = (Max - Min)
        // f(x) = ((ff - fi) / maxHoldTime) * x + fi
        private float GetScaler(float holdTime) => ((ff - fi) / maxHoldTime) * holdTime + fi;
    }
}