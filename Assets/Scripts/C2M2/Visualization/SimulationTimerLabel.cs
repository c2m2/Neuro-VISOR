using C2M2.Simulation;
using System;
using TMPro;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SimulationTimerLabel : MonoBehaviour
    {
        public TextMeshProUGUI timerText;

        /// <summary>
        /// Current time in simulation
        /// </summary>
        private float time;

        private void Awake()
        {
            if(timerText == null)
            {
                Debug.LogError("No label found");
                Destroy(this);
            }
        }

        private void Update()
        {
            if (GameManager.instance.activeSims.Count != 0)
            {
                time = GameManager.instance.activeSims[0].GetSimulationTime();
                timerText.text = ToString();
            }
            
        }

        static string sFormat = "{0:f0} s {1:f0} ms";
        static string msFormat = "{0:f0} ms";
        public override string ToString()
        {
            if (time > 1) return String.Format(sFormat, (int)time, (int)((time - (int)time) * 1000));
            else return String.Format(msFormat, (int)(time * 1000));
        }
    }
}