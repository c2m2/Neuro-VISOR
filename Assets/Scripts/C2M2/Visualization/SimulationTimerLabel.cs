using System;
using TMPro;
using UnityEngine;
using System.Runtime.Serialization;
using C2M2.Simulation;

namespace C2M2.Visualization
{
    [RequireComponent(typeof(Simulation<,,,>))]
    public class SimulationTimerLabel : MonoBehaviour
    {
        public Interactable sim = null;
        public TextMeshProUGUI timerText;

        /// <summary>
        /// Current time in simulation
        /// </summary>
        public double time;

        private void Start()
        {
            if (timerText == null) throw new LabelNotFoundException();
            timerText.text = time.ToString();
        }

        private void FixedUpdate()
        {
            if (sim != null)
            {
                time = sim.GetSimulationTime();
                timerText.text = ToString();
            }
            else
            {
                timerText.text = "";
            }
        }

        static string sFormat = "{0:f0} s {1:f0} ms";
        static string msFormat = "{0:f0} ms";
        public override string ToString()
        {
            if (time > 1) return String.Format(sFormat, (int)time, (int)((time - (int)time) * 1000));
            else return String.Format(msFormat, (int)(time * 1000));
        }

        public class LabelNotFoundException : Exception
        {
            public LabelNotFoundException() { }
            public LabelNotFoundException(string message) : base(message) { }
            public LabelNotFoundException(string message, Exception inner) : base(message, inner) { }
            protected LabelNotFoundException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt) { }
        }
    }
}