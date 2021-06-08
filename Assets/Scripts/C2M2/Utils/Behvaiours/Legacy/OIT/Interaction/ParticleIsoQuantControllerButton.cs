using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace C2M2.OIT.Interaction
{
    public class ParticleIsoQuantControllerButton : MonoBehaviour
    {
        public bool highController = true;
        public bool increasingButton = true;
        public Text valueText;
        public float buttonChangeValue = 0.1f;
        public ParticleSystemController partSysCon;

        public float buttonWaitTime = 0.4f;
        public AudioSource menuAudioSource;
        public AudioClip buttonPressSound;
        public AudioClip hapticAudioClip;
        private OVRHapticsClip hapticsClip;

        private GameObject localAvatar;

        private float originalHigh;
        private float originalLow;

        public void onClick()
        {
            if (highController)
            {
                if (increasingButton)
                {
                    //Don't let it pass its original value
                    if ((partSysCon.isoquantHigh + buttonChangeValue) <= originalHigh)
                    {
                        partSysCon.isoquantHigh += buttonChangeValue;
                    }
                }
                else
                {
                    //Don't let the high value pass the low value
                    if ((partSysCon.isoquantHigh - buttonChangeValue) > partSysCon.isoquantLow)
                    {
                        partSysCon.isoquantHigh -= buttonChangeValue;
                    }
                }
                //Update value text
                valueText.text = partSysCon.isoquantHigh.ToString();

            }
            else
            {
                if (increasingButton)
                {
                    //Don't let the low pass the high value
                    if ((partSysCon.isoquantLow + buttonChangeValue) < partSysCon.isoquantHigh)
                    {
                        partSysCon.isoquantLow += buttonChangeValue;
                    }
                }
                else
                {
                    //Don't let the low pass its original value
                    if ((partSysCon.isoquantLow - buttonChangeValue) >= originalLow)
                    {
                        partSysCon.isoquantLow -= buttonChangeValue;
                    }
                }

                //Update value text
                valueText.text = partSysCon.isoquantLow.ToString();

            }

            //Play button sound
            menuAudioSource.PlayOneShot(buttonPressSound);
        }


        // Use this for initialization
        void Start()
        {
            //Find local avatar
            localAvatar = GameObject.Find("LocalAvatar");
            hapticsClip = new OVRHapticsClip(hapticAudioClip);



        }

        private void Update()
        {
            if (Time.frameCount == 1)
            {
                originalHigh = partSysCon.isoquantHigh;
                originalLow = partSysCon.isoquantLow;

                if (highController)
                {
                    valueText.text = partSysCon.isoquantHigh.ToString();
                }
                else
                {
                    valueText.text = partSysCon.isoquantLow.ToString();
                }
            }
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