using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.VR
{
    public class TwoHandedGrabbable : MonoBehaviour
    {
        public PublicOVRGrabbable handA = null;
        public PublicOVRGrabbable handB = null;

        private void Awake()
        {
            //if(handA == null || handB == null)
        }
    }
}