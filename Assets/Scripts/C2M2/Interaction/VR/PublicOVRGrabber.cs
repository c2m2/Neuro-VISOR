using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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
    public class PublicOVRGrabber : XRGrabInteractable
    {
        public Collider[] M_GrabVolumes { get => colliders.ToArray(); set { } }
        public XRController Controller { get; }
    }
}
