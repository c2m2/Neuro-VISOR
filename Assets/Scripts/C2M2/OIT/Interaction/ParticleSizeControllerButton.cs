using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace C2M2.OIT.Interaction
{
    public class ParticleSizeControllerButton : MonoBehaviour
    {

        public bool increasingButton = true;
        public Text valueText;
        public float buttonChangeValue = 0.1f;
        public ParticleSystemController partSysCon;

        public float buttonWaitTime = 0.4f;
        public AudioSource menuAudioSource;
        public AudioClip buttonPressSound;
        public AudioClip hapticAudioClip;
        private OVRHapticsClip hapticsClip;

        public void onClick()
        {

            if (increasingButton)
            {
                //If adding to xMin keeps it below xMax, add to it
                if ((partSysCon.particleSize + buttonChangeValue) < 10)
                {
                    partSysCon.particleSize += buttonChangeValue;
                }
            }
            else
            {
                //If adding to xMin keeps it below xMax, add to it
                if ((partSysCon.particleSize - buttonChangeValue) > 0)
                {
                    partSysCon.particleSize -= buttonChangeValue;
                }
            }

            //Update value text
            valueText.text = partSysCon.particleSize.ToString();

            //Play button sound
            menuAudioSource.PlayOneShot(buttonPressSound);
        }


        // Use this for initialization
        void Start()
        {
            hapticsClip = new OVRHapticsClip(hapticAudioClip);

            //Initialize value text
            valueText.text = partSysCon.particleSize.ToString();

        }

        //Touch capability
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.parent.name.Contains("hands:"))
            {

                onClick();

                //if hand name contains r or l, run to r or l channel
                if (other.transform.parent.name.Contains("l"))
                {
                    OVRHaptics.LeftChannel.Mix(hapticsClip);
                }
                else if (other.transform.parent.name.Contains("r"))
                {
                    OVRHaptics.RightChannel.Mix(hapticsClip);
                }
            }
        }
    }
}