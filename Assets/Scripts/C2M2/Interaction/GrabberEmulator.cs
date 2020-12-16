using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction {
    public class GrabberEmulator : OVRGrabber
    {
        public float moveSpeed = 0.5f;

        protected override void Awake()
        {
            base.Awake();
            gameObject.AddComponent<MovementController>();

        }


    }
}