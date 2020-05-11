using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitOnTriggerEnter : MonoBehaviour {

    public AudioSource menuAudioSource;
    public AudioClip buttonPressSound;

    public AudioClip hapticAudioClip;
    private OVRHapticsClip hapticsClip;

    private void Start()
    {
         hapticsClip = new OVRHapticsClip(hapticAudioClip);
    }

    private void OnTriggerEnter(Collider other)
    {
        ButtonSound();
        HapticResponse(other);

        Invoke("ExitApplication", 0.1f);
    }

    //Haptic response
    private void HapticResponse(Collider other)
    {
        if (other.transform.parent.name.Contains("l"))
        {
            OVRHaptics.LeftChannel.Mix(hapticsClip);
        }
        else if (other.transform.parent.name.Contains("r"))
        {
            OVRHaptics.RightChannel.Mix(hapticsClip);
        }
    }

    private void ButtonSound()
    {
        menuAudioSource.PlayOneShot(buttonPressSound);
    }

    private void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

}
