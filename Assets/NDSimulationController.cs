using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using TMPro;
using UnityEngine.UI;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDSimulationController : MonoBehaviour
    {
        public NDSimulation sim = null;
        public Color defaultCol = new Color(1f, 0.75f, 0f);
        public Color highlightCol = new Color(1f, 0.85f, 0.4f);
        public Color pressedCol = new Color(1f, 0.9f, 0.6f);
        public Image[] defColTargets = new Image[0];
        public Image[] hiColTargets = new Image[0];
        public Image[] pressColTargets = new Image[0];

        private void Start()
        {
            if(sim == null)
            {
                Debug.LogError("No simulation given.");
                Destroy(gameObject);
            }
            StartCoroutine(UpdateColRoutine(0.5f));
        }

        private void UpdateCols()
        {
            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = defaultCol;
            }
            foreach(Image i in defColTargets)
            {
                i.color = defaultCol;
            }
            foreach (Image i in hiColTargets)
            {
                i.color = highlightCol;
            }
            foreach (Image i in pressColTargets)
            {
                i.color = pressedCol;
            }
        }

        IEnumerator UpdateColRoutine(float waitTime)
        {
            while (true)
            {
                UpdateCols();
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
}