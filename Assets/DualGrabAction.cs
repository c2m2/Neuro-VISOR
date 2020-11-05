using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction
{
    public abstract class DualGrabAction : MonoBehaviour
    {
        protected Transform handA = null;
        protected Transform handB = null;
        private Coroutine routine = null;
        private bool grabBegun = false;


        protected abstract IEnumerator GrabAction(Transform handA, Transform handB);

        public void GrabBegin(Transform handA, Transform handB)
        {
            if (routine == null)
            {
                Debug.Log("Dual grab begin");
                this.handA = handA;
                this.handB = handB;
                routine = StartCoroutine(GrabAction(handA, handB));
                grabBegun = true;
            }
        }

        private bool LeftDown { get { return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch); } }
        private bool RightDown { get { return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch); } }
        private bool BothTriggersPressed { get { return LeftDown && RightDown; } }

        private void Update()
        {
            if (grabBegun)
            {
                if(!BothTriggersPressed)
                {
                    GrabEnd();
                    grabBegun = false;
                }
            }
        }

        public void GrabEnd()
        {
            if (routine != null)
            {
                Debug.Log("Dual grab end.");
                StopCoroutine(routine);
                handA = null;
                handB = null;
            }
        }
    }
}