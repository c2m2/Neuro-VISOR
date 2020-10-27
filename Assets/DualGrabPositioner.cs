using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction
{
    public class DualGrabPositioner : DualGrabAction
    {
        protected override IEnumerator GrabAction(Transform handA, Transform handB)
        {
            Vector3 relPos = transform.position - Vector3.Lerp(handA.position, handB.position, 0.5f);

            while (true)
            {
                transform.position = relPos + Vector3.Lerp(handA.position, handB.position, 0.5f);
                yield return null;
            }
        }
    }
}