using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class VRGazeScrollDownButton : MonoBehaviour
{
    public ScrollRect panelScrollRect;

    private Image childImage;
    private Color childImageNewColor;

    public float scrollSpeed = 0.005f;
    
    public AudioClip hapticAudioClip;
    private OVRHapticsClip hapticsClip;

    private bool switchOn = false;
    private bool left = false;
    private bool right = false;

    public void Start()
    {
        panelScrollRect.verticalNormalizedPosition = 1f;
        if (gameObject.transform.childCount != 0 && gameObject.transform.GetChild(0).gameObject.GetComponent<Image>() != null)
        {
            childImage = gameObject.transform.GetChild(0).gameObject.GetComponent<Image>();
            childImageNewColor = childImage.color;
        }

        hapticsClip = new OVRHapticsClip(hapticAudioClip);
    }

    public void Update()
    {
        if(switchOn)
        {
            panelScrollRect.verticalNormalizedPosition -= scrollSpeed;
        }

        //If you're at the bottom of the list and the arrow image is visible, make the arrow image dissapear
        if(panelScrollRect.verticalNormalizedPosition <= 0.02 && childImage.color.a > 0f)
        {
                childImageNewColor.a = 0f;
                childImage.color = childImageNewColor;
        }

        //If you aren't at the bottom of the list and the arrow image is invisible, make it visible again
        if(panelScrollRect.verticalNormalizedPosition > 0.02 && childImage.color.a <= 0f)
        {
            childImageNewColor.a = 255f;
            childImage.color = childImageNewColor;
        }


       // panelScrollRect.verticalNormalizedPosition += 0.05f;
    }

    //Touch capability
    private void OnTriggerEnter(Collider other)
    {
        switchOn = true;

        if (other.transform.parent.name.Contains("l"))
        {
            left = true;
        }
        if (other.transform.parent.name.Contains("r"))
        {
            right = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        //If it's at the bottom stop playing clips, otherwise queue clips
        if (panelScrollRect.verticalNormalizedPosition <= 0)
        {
            if (left)
            {
                OVRHaptics.LeftChannel.Clear();
            }
            else if (right)
            {
                OVRHaptics.RightChannel.Clear();
            }
        }
        else
        {
            if (left)
            {
                OVRHaptics.LeftChannel.Queue(hapticsClip);
            }
            else if (right)
            {
                OVRHaptics.RightChannel.Queue(hapticsClip);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        switchOn = false;

        OVRHaptics.LeftChannel.Clear();
        OVRHaptics.RightChannel.Clear();

        left = false;
        right = false;
    }

    public void OnGazeEnter()
    {
        //Switcch scroll on
        switchOn = true;
    }

    public void OnGazeExit()
    {
        switchOn = false;
    }

}
