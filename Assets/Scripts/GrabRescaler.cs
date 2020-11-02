using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction
{
    /// <summary>
    /// Adds some percentage of a transform's original scale at runtime
    /// </summary>
    [RequireComponent(typeof(OVRGrabbable))]
    public class GrabRescaler : MonoBehaviour
    {
        private OVRGrabbable grabbable = null;
        private Vector3 origScale;
        private float scaler = 0.1f;

        // Returns a value between -2 and 2, where -2 implies both thumbsticks are held down, and 2 implies both are held up.
        private float ThumbstickScaler
        {
            get
            {
                // Use the value of whichever joystick is held up furthest
                float y1 = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
                float y2 = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
                return (y1 + y2) / 2;
            }
        }

        private void Awake()
        {
            if (!GameManager.instance.vrIsActive) Destroy(this);

            grabbable = GetComponent<OVRGrabbable>();
            
        }

        private void Start()
        {
            // Use this to determine how to scale at runtime
            origScale = scaler * transform.localScale;
        }

        // Update is called once per frame
        void Update()
        {
            if (grabbable.isGrabbed)
            {
                transform.localScale += scaler * ThumbstickScaler * transform.localScale;
            }
        }
    }
}