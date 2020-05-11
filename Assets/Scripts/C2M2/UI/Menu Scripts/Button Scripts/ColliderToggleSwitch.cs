using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColliderToggleSwitch : MonoBehaviour {
    Toggle objectToggle = null;

    private OVRHapticsClip hapticsClip;
    public AudioClip hapticAudioClip;

    private void Start()
    {
        objectToggle = gameObject.GetComponent<Toggle>();
        hapticsClip = new OVRHapticsClip(hapticAudioClip);
    }

    private void OnTriggerEnter(Collider other)
    {
        //Only allow interactions by fingers, otherwise hand colliders get in the way       
        if (other.transform.parent.name.Contains("hands:"))
        {         
            if (objectToggle.isOn == false)
            {
                objectToggle.isOn = true;
            }
            else
            {
                objectToggle.isOn = false;
            }

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
