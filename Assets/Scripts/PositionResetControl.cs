using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction
{
    public class PositionResetControl : MonoBehaviour
    {
        public OVRInput.Button resetButton = OVRInput.Button.Start;
        public Vector3 resetPosition = Vector3.zero;
        public Transform target = null;

        private void Start()
        {
            if (target == null) target = transform;
        }

        // Update is called once per frame
        void Update()
        {
            if (OVRInput.Get(resetButton))
            {
                Debug.Log("Resetting " + target.name + "'s position to " + resetPosition.ToString("F4"));
                target.position = resetPosition;
            }
        }
    }
}