using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Utils;

namespace C2M2.Interaction.VR
{
    public class VRGrabbableMesh : MonoBehaviour
    {
        private void Awake()
        {
            // Initialize Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) gameObject.AddComponent<Rigidbody>();

            // Initialize new collider array
            Collider[] grabColliders = new Collider[1];
            grabColliders = NonConvexMeshCollider.Calculate(gameObject);

            // If there is no OVRGrabbable, we can't make these colliders meaningful
            PublicOVRGrabbable ovr = GetComponent<PublicOVRGrabbable>();
            if (ovr == null) ovr = gameObject.AddComponent<PublicOVRGrabbable>();
            ovr.M_GrabPoints = grabColliders;
        }

        private void Start()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.SetDefaultState();

            Destroy(this);
        }
    }
}