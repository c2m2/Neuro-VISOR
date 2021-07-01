using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2
{
    namespace Utils
    {
        public static class RigidbodyUtilities
        {
            public static void SetDefaultState(this Rigidbody rb)
            {
                rb.drag = Mathf.Infinity;
                rb.angularDrag = Mathf.Infinity;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }
}
