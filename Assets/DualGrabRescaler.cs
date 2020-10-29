using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction
{
    /// <summary>
    /// Scales an object grabbed by two OVRGrabbers
    /// </summary>
    public class DualGrabRescaler : DualGrabAction
    {
        protected override IEnumerator GrabAction(Transform handA, Transform handB)
        {
            Vector3 origScale = transform.localScale;
            float origDist = Vector3.Distance(handA.position, handB.position);

            while (true)
            {
                float dist = Vector3.Distance(handA.position, handB.position);
                float relDist = dist / origDist;

                Vector3 newScale = relDist * origScale;
                transform.localScale = newScale;

                yield return null;
            }
        }
    }
}