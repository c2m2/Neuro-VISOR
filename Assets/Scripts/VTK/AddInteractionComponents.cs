using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds components needed for VR interaction. Use this to build VR-interactable objects at runtime.
/// </summary>
[RequireComponent(typeof(RaycastMeshCollider))]
public class AddInteractionComponents
{
    #region PUBLIC_MEMBERS
    [Header("Rigidbody Options")]
        /// <summary>
        /// If false, rigidbody drag and angular drag = Infinity
        /// </summary>
        [Tooltip("If false, rigidbody drag and angular drag = Infinity")]
        public bool freeMoving = false;
        /// <summary>
        /// If false, rigidbody.useGravity = false
        /// </summary>
        [Tooltip("If false, rigidbody.useGravity = false")]
        public bool useGravity = false;
        /// <summary>
        /// If false, rigidbody.isKinematic = false
        /// </summary>
        [Tooltip("If false, rigidbody.isKinematic = false")]
        public bool kinematic = false;
    [Header("Non-convex Compound Collider Options")]
        ///<summary>
        /// Should a compound collider be calculated and added?
        /// </summary>
        [Tooltip("Should a compound collider be calculated and added?")]
        public bool addNonConvexMeshCollider = true;
        /// <summary>
        /// If true, NonConvexMeshCollider component will be added and calculated and compoundColliderResolution will be passed to it
        /// </summary>
        [Tooltip("If true, NonConvexMeshCollider component will be added and calculated and compoundColliderResolution will be passed to it")]
        public bool buildCompoundCollider = true;
        /// <summary>
        /// Compound collider "resolution". If compoundColliderResolution = 20, the mesh's bounding box will be filled with 20x20x20 box colliders the will be whittled down to fit the mesh.
        /// </summary>
        [Tooltip("Compound collider \"resolution\". If compoundColliderResolution = 20, the mesh's bounding box will be filled with 20x20x20 box colliders the will be whittled down to fit the mesh.")]
        public int compoundColliderResolution = 75;
    #endregion
    #region PRIVATE_MEMBERS
        /// <summary>
        /// OVR grabbable 
        /// </summary>
        private OVRGrabbable grab;
        private RaycastMeshCollider buildRaycastee;
    #endregion
    /// <summary>
    /// Build virtual reality functionality.
    /// </summary>
    public void Build(GameObject gameObject)
    {
        // Add a rigidbody
        Rigidbody rB = gameObject.AddComponent<Rigidbody>();    
        if (!freeMoving)
        { // If not free moving, set drags to infinity. Otherwise leave defaults
            rB.drag = Mathf.Infinity;
            rB.angularDrag = Mathf.Infinity;
        }    
        if (!useGravity)
        { // If non-gravty, set gravity to false. Otherwise leave default
            rB.useGravity = false;
        } 
        if (kinematic)
        { // If kinematic, set kinematic true. Otherwise leave default
            rB.isKinematic = true;
        }      
        /// Make object grabbable in VR
        OVRGrabbable grab = gameObject.AddComponent<OVRGrabbable>();
        grab.enabled = true;

    }
}
