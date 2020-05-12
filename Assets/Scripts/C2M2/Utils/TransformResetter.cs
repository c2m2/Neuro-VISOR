using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2 {
    namespace Utils {
        using static Math;
        /// <summary>
        /// This resets the position, rotation of the object it's attached to at "startFrame".
        /// </summary>
        /// <remarks>
        /// Hacky solution to scripts "nudging" object/child object positions
        /// </remarks>
        public class TransformResetter : MonoBehaviour
        {
            [Tooltip("Position to reset this transform to.")]
            /// <summary> Position to reset this transform to. </summary>
            public Vector3 position = Vector3.zero;
            [Tooltip("Rotation to reset this transform to.")]
            /// <summary> Rotation to reset this transform to. </summary>
            public Vector3 rotation = Vector3.zero;
            [Tooltip("The frame at which this script resets position (0 resets transform in Start; -1 in Awake; > 0 in Update).")]
            /// <summary>
            /// The frame at which this script resets position.
            /// </summary>
            /// <remarks>
            /// == 0 resets in Start,
            /// == -1 resets in Awake,
            /// > 0 resets in Update
            /// </remarks>
            public int targetFrame = 1;
            private int maxFrame = 1000;

            // Awake is called before Start
            private void Awake()
            {
                // -1 <= startFrame <= maxFrame
                targetFrame = Min(Max(targetFrame, -1), maxFrame);
                if(targetFrame == -1) ResetPosition();
            }

            // Start is called before the first frame update
            void Start()
            {
                if (targetFrame == 0) ResetPosition();
            }

            // Update is called once per frame
            void Update()
            {
                if (Time.frameCount == targetFrame) ResetPosition();
            }

            // Reset transform and destroys this component
            private void ResetPosition()
            {
                transform.position = position;
                transform.eulerAngles = rotation;
                Destroy(this);
            }
        }
    }
}
