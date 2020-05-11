using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class ProximitySoundFader : MonoBehaviour
{
    public AudioLowPassFilter lowPass;

    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Equals("Interior"))
        {
            MultiframeFade_LowpassCutoff fader = gameObject.GetComponent<MultiframeFade_LowpassCutoff>();
            if(fader != null){          //If there is already a fader effect playing
                Destroy(fader);             //Destroy it
            }
            MultiframeFade_LowpassCutoff newFade = gameObject.AddComponent<MultiframeFade_LowpassCutoff>();                   
            newFade.frequencyTarget = 300f;
            newFade.resonanceTarget = 2f;
            newFade.frameDuration = 1;
       
            //lowPass.cutoffFrequency = 300f;
            //lowPass.lowpassResonanceQ = 2f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name.Equals("Interior"))
        {
            MultiframeFade_LowpassCutoff fader = gameObject.GetComponent<MultiframeFade_LowpassCutoff>();
            if (fader != null)
            {                                   //If there is already a fader effect playing
                Destroy(fader);                     //Destroy it
            }
            MultiframeFade_LowpassCutoff newFade = gameObject.AddComponent<MultiframeFade_LowpassCutoff>();
            newFade.decrease = false;           //Increase the value
            newFade.frequencyTarget = 22000f;
            newFade.resonanceTarget = 1f;
            newFade.frameDuration = 1;

            //lowPass.cutoffFrequency = 22000f;
            //lowPass.lowpassResonanceQ = 1f;
            //Make music sound duller
        }
    }
}
