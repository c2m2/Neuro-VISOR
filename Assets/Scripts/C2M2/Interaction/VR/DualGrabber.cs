using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.VR
{
    public class DualGrabber : OVRGrabber
    {
        private Transform leftGrabber { get { return GameManager.instance.ovrLeftHandAnchor.transform; } }
        private Transform rightGrabber { get { return GameManager.instance.ovrRightHandAnchor.transform; } }

        protected override void OffhandGrabbed(OVRGrabbable grabbable)
        {
            // If the object we are trying to grab is already grabbed, it's a dual grab
            if(m_grabbedObj == grabbable)
            {
                // Invoke dual grab actions
                DualGrabAction[] grabActions = m_grabbedObj.GetComponents<DualGrabAction>();
                foreach(DualGrabAction action in grabActions)
                {
                    action.GrabBegin(leftGrabber, rightGrabber);
                }
            }

            base.OffhandGrabbed(grabbable);
        }
    }
}