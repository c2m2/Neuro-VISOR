using System;
using UnityEngine;
using TMPro;
namespace C2M2.Interaction.UI
{
    /// <summary>
    /// Stores and updates text labels for FPS readings
    /// </summary>
    public class FPSLabel : MonoBehaviour
    {
        public TextMeshProUGUI highFPSLabel;
        public TextMeshProUGUI avgFPSLabel;
        public TextMeshProUGUI lowFPSLabel;
        private Utils.DebugUtils.FPSCounter fpsCounter;

        void Start()
        {
            fpsCounter = GameManager.instance.fpsCounter;
            if (avgFPSLabel == null) throw new LabelNotFoundException();
            if (highFPSLabel == null) throw new LabelNotFoundException();
            if (lowFPSLabel == null) throw new LabelNotFoundException();
        }

        void Update()
        {
            avgFPSLabel.text = fpsCounter.avgStr;
            highFPSLabel.text = fpsCounter.highStr;
            lowFPSLabel.text = fpsCounter.lowStr;
        }
        public class LabelNotFoundException : Exception
        {
            public LabelNotFoundException() { }
            public LabelNotFoundException(string message) : base(message) { }
            public LabelNotFoundException(string message, Exception inner) : base(message, inner) { }
        }
    }

}
