using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Utils.Animation
{
    public class RotateAnimate : MonoBehaviour
    {

        public float rotateSpeed = 0.1f;

        private OVRGrabbable grabbable;
        private Rigidbody rb;

        // Use this for initialization
        void Start()
        {
            grabbable = GetComponent<OVRGrabbable>();
            rb = GetComponent<Rigidbody>();
            enabled = true;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            transform.Rotate(new Vector3(0, rotateSpeed, 0), Space.Self);
        }

        void GrabStateChange(bool newState)
        {
            if (newState)
            {
                enabled = false;            //If we've just been grabbed, disable animation
            }
            else
            {
                enabled = true;           //If we've just been release, enable animation again
            }
        }
    }
}