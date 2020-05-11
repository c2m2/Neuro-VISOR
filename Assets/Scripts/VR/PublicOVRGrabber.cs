using UnityEngine;

namespace C2M2
{
    namespace Utilities
    {
        namespace VR
        {
            /// <summary>
            /// Offers a public interface for other scripts to change OVRGrabber's grab volume.
            /// </summary>
            public class PublicOVRGrabber : OVRGrabber
            {
                public Collider[] M_GrabVolumes { get { return m_grabVolumes; } set { m_grabVolumes = value; } }
            }
        }
    }
}
