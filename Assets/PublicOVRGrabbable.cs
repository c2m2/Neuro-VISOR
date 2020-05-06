using UnityEngine;

namespace C2M2
{
    namespace Utilities
    {
        namespace VR
        {
            /// <summary>
            /// Offers a public interface for other scripts to change OVRGrabber's grab points, such as when using compound colliders
            /// </summary>
            public class PublicOVRGrabbable : OVRGrabbable
            {
                public Collider[] M_GrabPoints { get { return m_grabPoints; } set { m_grabPoints = value; } }
            }
        }
    }
}
