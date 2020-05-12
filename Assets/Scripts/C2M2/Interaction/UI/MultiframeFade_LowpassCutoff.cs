using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.UI
{
    [RequireComponent(typeof(AudioLowPassFilter))]
    public class MultiframeFade_LowpassCutoff : MonoBehaviour
    {
        public AudioLowPassFilter lowPass;
        [Tooltip("Number of frames to fade over")]
        public int frameDuration = 25;
        [Tooltip("Should the lowpass attributes be reduced or increased back to default?")]
        public bool decrease = true;
        public float frequencyTarget = 300f;
        public float resonanceTarget = 2f;

        private float frequencyIncrement;
        private float resonanceIncrement;
        private int frameCounter = 0;

        // Start is called before the first frame update
        void Start()
        {
            lowPass = gameObject.GetComponent<AudioLowPassFilter>();
            frequencyIncrement = Mathf.Abs(frequencyTarget - lowPass.cutoffFrequency) / frameDuration;      // |Final - Initial| / (Number of steps to take)     
            resonanceIncrement = Mathf.Abs(resonanceTarget - lowPass.lowpassResonanceQ) / frameDuration;
            Debug.Log(lowPass.cutoffFrequency);
        }

        // Update is called once per frame
        void Update()
        {
            if (frameCounter > frameDuration)                    //Override incase something goes wrong
            {
                lowPass.cutoffFrequency = frequencyTarget;
                lowPass.lowpassResonanceQ = resonanceTarget;
                Destroy(this);
            }

            if (decrease)
            {
                if (lowPass.cutoffFrequency > frequencyTarget)
                {
                    lowPass.cutoffFrequency -= frequencyIncrement;
                    lowPass.lowpassResonanceQ -= resonanceIncrement;
                    Debug.Log(lowPass.cutoffFrequency);
                }
                else
                {
                    Destroy(this);
                }
            }
            else
            {
                if (lowPass.cutoffFrequency < frequencyTarget)
                {
                    lowPass.cutoffFrequency += frequencyIncrement;
                    lowPass.lowpassResonanceQ += resonanceIncrement;
                }
                else
                {
                    Destroy(this);
                }
            }
            frameCounter++;
        }
    }
}