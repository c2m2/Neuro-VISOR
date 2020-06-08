using UnityEngine;
using System;

namespace C2M2.Interaction.VR
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

    public class GrabbableNotFoundException : Exception
    {
        public GrabbableNotFoundException() { }
        public GrabbableNotFoundException(string message) : base(message) { }
        public GrabbableNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
