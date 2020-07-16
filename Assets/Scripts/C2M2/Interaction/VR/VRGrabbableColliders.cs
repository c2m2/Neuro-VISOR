using UnityEngine;
using C2M2.Utils;
using System.Collections.Generic;

namespace C2M2.Interaction.VR
{
    public class VRGrabbableColliders : MonoBehaviour
    {
        public Collider[] source = null;
        private void Awake()
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.SetDefaultState();
            gameObject.layer = LayerMask.NameToLayer("Grabbable");
        }

        // Start is called before the first frame update
        void Start()
        {
            // Initialize new collider array
            Collider[] allColliders = new Collider[1];
            if(source == null)
            {
                allColliders = gameObject.GetComponentsInChildren<Collider>();
            }
            else
            {
                allColliders = source;
            }
                 
            List<Collider> grabColliders = new List<Collider>(allColliders.Length / 2);
            for(int i = 0; i < allColliders.Length; i++)
            {
                if (!allColliders[i].name.Contains("RaycastTarget"))
                {
                    allColliders[i].gameObject.layer = LayerMask.NameToLayer("Grabbable");
                    allColliders[i].isTrigger = true;
                    grabColliders.Add(allColliders[i]);               
                }
            }

            allColliders = grabColliders.ToArray();

            // If there is no OVRGrabbable, we can't make these colliders meaningful
            PublicOVRGrabbable ovr = GetComponent<PublicOVRGrabbable>();
            if (ovr == null) ovr = gameObject.AddComponent<PublicOVRGrabbable>();
            ovr.M_GrabPoints = grabColliders.ToArray();

            //Destroy(this);

        }
    }
}
