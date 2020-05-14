using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Utils.DebugUtils.Actions
{
    /// <summary> Correct an object's position and rotation to given values </summary>
    public static class TransformUpdate
    {
        /// <summary> [Default]: update transform to zero vector and quaternion identity </summary>
        /// <param name="transform"> Transform to adjust </param>
        public static void UpdateTransform(Transform transform)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
        /// <summary> [Position Only]: update transform to newPosition </summary>
        /// <param name="newPosition"> New Vector3 for transform.position </param>
        public static void UpdateTransform(Transform transform, Vector3 newPosition)
        {
            transform.position = newPosition;
        }
        /// <summary> [Rotation Only]: update transform to newRotation </summary>
        /// <param name="newRotation"> New Quaternion value for transform.rotation </param>
        public static void UpdateTransform(Transform transform, Quaternion newRotation)
        {
            transform.rotation = newRotation;
        }
        /// <summary> [Position + Vector3 Rotation]: update transform to newPosition and newRotation's x, y, z (With the object's current 'w' value) </summary>
        /// <param name="newRotation"> New Vector3 values for transform.rotation(x, y, z), current 'w' is maintained </param>
        public static void UpdateTransform(Transform transform, Vector3 newPosition, Vector3 newRotation)
        {
            Quaternion newRotation4 = new Quaternion(newRotation.x, newRotation.y, newRotation.z, transform.rotation.w);
            transform.position = newPosition;
            transform.rotation = newRotation4;
        }
        /// <summary> [Position + Quaternion Rotation]: Update transform to newPosition and newRotation </summary>
        /// <param name="newRotation"> New Quaternion value for transform.rotation </param>
        public static void UpdateTransform(Transform transform, Vector3 newPosition, Quaternion newRotation)
        {
            transform.position = newPosition;
            transform.rotation = newRotation;
        }
    }
}