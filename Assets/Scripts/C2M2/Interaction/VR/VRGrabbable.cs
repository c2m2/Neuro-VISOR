using UnityEngine;

namespace C2M2
{
    namespace Interaction
    {
        namespace VR
        {
            using Utils;
            using VR;
            using Interaction;
            /// <summary>
            /// Add Rigidbody, Collider, and OVRGrabbable to object
            /// </summary>
            public class VRGrabbable : MonoBehaviour
            {
                public Collider[] grabColliders;
                public enum TCollider { Sphere, Box, Capsule, NonConvexMeshCollider }
                private TCollider colliderType = TCollider.NonConvexMeshCollider;
                public TCollider ColliderType
                {
                    get { return colliderType; }
                    set
                    {
                        colliderType = value;
                        RefreshColliders(ColliderType);
                    }
                }
                private void Awake()
                {
                    // Initialize Rigidbody
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = gameObject.AddComponent<Rigidbody>();
                    }
                    // Initialize Colliders
                    RefreshColliders(ColliderType);
                }
                private void Start()
                {
                    Rigidbody rb = GetComponent<Rigidbody>();
                    rb.SetDefaultState();
                }
                private void RefreshColliders(TCollider colliderType)
                {
                    if (grabColliders != null)
                    {
                        // Destroy old colliders
                        for (int i = 0; i < grabColliders.Length; i++)
                        {
                            Destroy(grabColliders[i]);
                        }
                    }
                    // Initialize new collider array
                    grabColliders = new Collider[1];
                    switch (colliderType)
                    {
                        case (TCollider.NonConvexMeshCollider):
                            grabColliders = NonConvexMeshCollider.Calculate(gameObject);
                            break;
                        case (TCollider.Sphere):
                            grabColliders[0] = gameObject.AddComponent<SphereCollider>();
                            break;
                        case (TCollider.Box):
                            grabColliders[0] = gameObject.AddComponent<BoxCollider>();
                            break;
                        case (TCollider.Capsule):
                            grabColliders[0] = gameObject.AddComponent<CapsuleCollider>();
                            break;
                    }
                    // If there is no OVRGrabbable, we can't make these colliders meaningful
                    PublicOVRGrabbable ovr = GetComponent<PublicOVRGrabbable>();
                    if (ovr == null)
                    {
                        ovr = gameObject.AddComponent<PublicOVRGrabbable>();
                    }
                    ovr.M_GrabPoints = grabColliders;
                }
            }
        }
    }
}
