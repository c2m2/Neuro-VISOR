using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace C2M2
{
    public class FPSLabel : MonoBehaviour
    {
        public TextMeshProUGUI highFPSLabel;
        public TextMeshProUGUI avgFPSLabel;
        public TextMeshProUGUI lowFPSLabel;
        private FPSCounter fpsCounter;

        // Start is called before the first frame update
        void Start()
        {
            fpsCounter = GameManager.instance.fpsCounter;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateLabels();
        }
        private void UpdateLabels()
        {
            if (avgFPSLabel != null) avgFPSLabel.text = fpsCounter.averageFPSString;
            if (highFPSLabel != null) highFPSLabel.text = fpsCounter.highestFPSString;
            if (lowFPSLabel != null) lowFPSLabel.text = fpsCounter.lowestFPSString;
        }
    }
}
