#if UNITY_EDITOR
#pragma warning disable 0414 // warning CS0414: The field 'ParticleFieldScaleButtonController.valueChanged' is assigned but its value is never used
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParticleFieldScaleButtonController : MonoBehaviour {

    public bool increasingButton = true;
    public Text valueText;
    public float buttonChangeValue = 0.5f;
    public ParticleSystemController partSysCon;

    public float buttonWaitTime = 0.4f;
    public AudioSource menuAudioSource;
    public AudioClip buttonPressSound;
    public AudioClip hapticAudioClip;
    private OVRHapticsClip hapticsClip;

    private bool valueChanged = false;
    private float axisValue;

    private bool xMin = false;
    private bool xMax = false;
    private bool yMin = false;
    private bool yMax = false;
    private bool zMin = false;
    private bool zMax = false;

    private GameObject localAvatar;

    public void onClick()
    {
        if (xMin)
        {
            if (increasingButton)
            {
                //If adding to xMin keeps it below xMax, add to it
                if ((partSysCon.minValues.x + buttonChangeValue) < partSysCon.maxValues.x)
                {
                    partSysCon.minValues.x += buttonChangeValue;
                }
            }
            else
            {
                partSysCon.minValues.x -= buttonChangeValue;
            }

            //Update value text
            valueText.text = partSysCon.minValues.x.ToString();
        }
        else if (xMax)
        {
            if (increasingButton)
            {
                partSysCon.maxValues.x += buttonChangeValue;
            }
            else
            {
                //If subtracting from max keeps it above min, subtract it
                if ((partSysCon.maxValues.x - buttonChangeValue) > partSysCon.minValues.x)
                {
                    partSysCon.maxValues.x -= buttonChangeValue;
                }
            }
            valueText.text = partSysCon.maxValues.x.ToString();
        }
        else if (yMin)
        {
            if (increasingButton)
            {
                if ((partSysCon.minValues.y + buttonChangeValue) < partSysCon.maxValues.y)
                {
                    partSysCon.minValues.y += buttonChangeValue;
                }
            }
            else
            {
                partSysCon.minValues.y -= buttonChangeValue;
            }
            valueText.text = partSysCon.minValues.y.ToString();
        }
        else if (yMax)
        {
            if (increasingButton)
            {
                partSysCon.maxValues.y += buttonChangeValue;
            }
            else
            {
                if ((partSysCon.maxValues.y - buttonChangeValue) > partSysCon.minValues.y)
                {
                    partSysCon.maxValues.y -= buttonChangeValue;
                }
            }
            valueText.text = partSysCon.maxValues.y.ToString();
        }
        else if (zMin)
        {
            if (increasingButton)
            {
                if ((partSysCon.minValues.z + buttonChangeValue) < partSysCon.maxValues.z)
                {
                    partSysCon.minValues.z += buttonChangeValue;
                }
            }
            else
            {
                partSysCon.minValues.z -= buttonChangeValue;
            }
            valueText.text = partSysCon.minValues.z.ToString();
        }
        else if (zMax)
        {
            if (increasingButton)
            {
                partSysCon.maxValues.z += buttonChangeValue;
            }
            else
            {
                if ((partSysCon.maxValues.z - buttonChangeValue) > partSysCon.minValues.z)
                {
                    partSysCon.maxValues.z -= buttonChangeValue;
                }
            }
            valueText.text = partSysCon.maxValues.z.ToString();
        }
        else
        {
            Debug.Log("Error: " + gameObject.name + " in " + gameObject.transform.parent.name + " is not controlling an axis value");
        }

        //Play button sound
        menuAudioSource.PlayOneShot(buttonPressSound);
    }


    // Use this for initialization
    void Start () {
        //Find local avatar
        localAvatar = GameObject.Find("LocalAvatar");
        hapticsClip = new OVRHapticsClip(hapticAudioClip);

    }

    private void Update()
    {
        if(Time.frameCount == 1)
        {
            //Find button case and prime it for that case
            if (gameObject.transform.parent.name.Equals("Xmin"))
            {
                xMin = true;
                valueText.text = partSysCon.minValues.x.ToString();
            }
            else if (gameObject.transform.parent.name.Equals("Xmax"))
            {
                xMax = true;
                valueText.text = partSysCon.maxValues.x.ToString();
            }
            else if (gameObject.transform.parent.name.Equals("Ymin"))
            {
                yMin = true;
                valueText.text = partSysCon.minValues.y.ToString();
            }
            else if (gameObject.transform.parent.name.Equals("Ymax"))
            {
                yMax = true;
                valueText.text = partSysCon.maxValues.y.ToString();
            }
            else if (gameObject.transform.parent.name.Equals("Zmin"))
            {
                zMin = true;
                valueText.text = partSysCon.minValues.z.ToString();
            }
            else if (gameObject.transform.parent.name.Equals("Zmax"))
            {
                zMax = true;
                valueText.text = partSysCon.maxValues.z.ToString();
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
