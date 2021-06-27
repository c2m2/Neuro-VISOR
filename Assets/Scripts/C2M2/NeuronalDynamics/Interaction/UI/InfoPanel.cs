using TMPro;
using UnityEngine;

namespace C2M2.Interaction.UI
{
    public class InfoPanel : MonoBehaviour
    {
        public string valueLabel = "";
        public string unit = "mV";
        public TextMeshProUGUI vertexText;
        public TextMeshProUGUI powerText;
        public int Vertex
        {
            get
            {
                int.TryParse(vertexText.text, out int vert);
                return vert;
            }
            set
            {
                vertexText.text = "Vertex " + value.ToString();
            }
        }
        private int powerPrecisionInt = 2;
        public int PowerPrecision
        {
            get
            {
                return powerPrecisionInt;
            }
            set
            {
                powerPrecisionInt = Mathf.Clamp(value, 2, 6); ;
                powerPrecision = "F" + powerPrecisionInt;
            }
        }
        private string powerPrecision = "F2";
        public double Power
        {
            get
            {
                double.TryParse(powerText.text, out double power);
                return power;
            }
            set
            {
                if (value < 1 && value > -1) PowerPrecision++;
                else PowerPrecision--;
                powerText.text = string.Format(powerFormat, valueLabel, value.ToString(powerPrecision), unit);
            }
        }
        private readonly string powerFormat = "{0} {1} {2}";

        /// <summary>
        /// Position to focus panel on.
        /// </summary>
        /// <remarks>
        /// Panel will automatically place itself at this position, and shift itself to the side if requested.
        /// </remarks>
        public Vector3 FocusLocalPosition
        {
            get { return transform.localPosition; }
            set
            {
                // Position the panel at desired location, and then shift it over to match the shift anchor
                transform.localPosition = new Vector3(value.x, value.y, value.z);
                transform.position = shiftAnchor.position;
            }
        }

        public Vector3 GlobalSize = new Vector3(.0005f, .0005f, .0005f);

        private RectTransform rt;
        // We want the panel to hover to the side of the cursor, instead of directly over it.
        // Shift anchor provides the position to shift the panel over to.
        private Transform shiftAnchor;
        private void Awake()
        {
            rt = (RectTransform)transform;
            shiftAnchor = transform.Find("ShiftAnchor");
            if (shiftAnchor == null) Debug.LogError("No shift anchor found for panel.");
        }
        void Update()
        {
            transform.LookAt(transform.position);
            transform.rotation = Camera.main.transform.rotation;

            if (transform.lossyScale != GlobalSize)
            {
                Vector3 newLocalScale = new Vector3(
                    transform.localScale.x * GlobalSize.x / transform.lossyScale.x,
                    transform.localScale.y * GlobalSize.y / transform.lossyScale.y,
                    transform.localScale.z * GlobalSize.z / transform.lossyScale.z);
                transform.localScale = newLocalScale;
            }

        }
    }
}