using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Utils
{
    public class PositionResetControl : MonoBehaviour
    {
        public OVRInput.Button resetButton = OVRInput.Button.Start;
        public KeyCode resetKey = KeyCode.X;
        public Vector3 resetPosition = Vector3.zero;
        public Transform target = null;

        private bool ResetRequested
        {
            get
            {
                return GameManager.instance.vrIsActive ?
                    OVRInput.Get(resetButton) :
                    Input.GetKey(resetKey);
            }
        }

        private void Start()
        {
            if (target == null) target = transform;
        }

        // Update is called once per frame
        void Update()
        {
            if (ResetRequested)
            {
                Debug.Log("Resetting " + target.name + "'s position to " + resetPosition.ToString("F4"));
                target.position = resetPosition;
            }
        }
    }
}