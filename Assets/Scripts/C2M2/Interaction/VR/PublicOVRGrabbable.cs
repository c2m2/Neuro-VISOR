using UnityEngine;

namespace C2M2
{
    namespace Interaction
    {
        namespace VR
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// Offers a public interface for other scripts to change OVRGrabber's grab points, such as when using compound colliders
            /// </summary>
            /// 
            /// <remarks>
            /// This is used so that we can produce a NonConvexMeshCollider and set its colliders as grab points at runtime
            /// </remarks>
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            public class PublicOVRGrabbable : OVRGrabbable
            {
                public Collider[] M_GrabPoints { get { return m_grabPoints; } set { m_grabPoints = value; } }
            }
        }
    }
}
