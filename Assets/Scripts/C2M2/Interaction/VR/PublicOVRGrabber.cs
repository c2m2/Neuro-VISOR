using UnityEngine;

namespace C2M2.Interaction.VR
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Offers a public interface for other scripts to change OVRGrabber's grab volume.
    /// </summary>
    /// 
    /// <remarks>
    /// This allows you to change the user's grab colliders at runtime
    /// </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class PublicOVRGrabber : OVRGrabber
    {
        public Collider[] M_GrabVolumes { get { return m_grabVolumes; } set { m_grabVolumes = value; } }
        public OVRInput.Controller Controller { get { return m_controller; } }
    }
}
