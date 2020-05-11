using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
[ExecuteInEditMode]
public class VirtualRealityEnabled : MonoBehaviour
{
    public bool scriptEnabled = false;
    public OVRPlayerController ovrPlayerController = null;
    public GameObject nonVRCamera = null;
    private void OnEnable()
    {
        if (scriptEnabled)
        {
            if (!Application.isPlaying)
            { // We can't change these settings if the application is running
              /// NOTE: OVRPlayerController object should enable VR settings when it becomes enabled
                XRSettings.enabled = true;
                if (!ovrPlayerController.gameObject.activeSelf)
                { // If OVRPlayerController is disabled, enable it
                    ovrPlayerController.gameObject.SetActive(true);
                }
                if (nonVRCamera.activeSelf)
                { // If our non-VR camera is still enabled, disable it
                    nonVRCamera.SetActive(false);
                }
            }
        }
    }
    private void OnDisable()
    {
        if (scriptEnabled)
        {
            if (!Application.isPlaying)
            { // We can't change these settings if the application is running
                if (!nonVRCamera.activeSelf)
                { // If our non-VR camera is disabled, enable it
                    nonVRCamera.SetActive(true);
                }
                /// NOTE:   OVRPlayerController object must be disabled before disabling VR. 
                ///         OVRPlayerController will automatically enable VR when it is enabled
                if (ovrPlayerController.gameObject.activeSelf)
                { // If OVRPlayerController is enabled, disable it
                    ovrPlayerController.gameObject.SetActive(false);
                }
                XRSettings.LoadDeviceByName("none");
                XRSettings.enabled = false;
            }
        }
    }
}
