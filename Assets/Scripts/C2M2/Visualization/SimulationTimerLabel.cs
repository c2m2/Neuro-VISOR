using System;
using TMPro;
using UnityEngine;
using C2M2.Simulation;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    [RequireComponent(typeof(Simulation<,,,>))]
    public class SimulationTimerLabel : MonoBehaviour
    {
        public NDSimulationController simController = null;
        public Interactable Sim
        {
            get
            {
                return simController.sim;
            }
        }

        public TextMeshProUGUI timerText;

        /// <summary>
        /// Current time in simulation
        /// </summary>
        private float time;

        private void Awake()
        {
            bool fatal = false;
            if(timerText == null)
            {
                Debug.LogError("No label found.");
                fatal = true;
            }
            if(simController == null)
            {
                simController = GetComponentInParent<NDSimulationController>();
                if(simController == null)
                {
                    Debug.LogError("No simulation controller found.");
                    fatal = true;
                }
            }
            if (fatal) Destroy(this);
        }

        private void Update()
        {
            time = Sim.GetSimulationTime();
            timerText.text = ToString();
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