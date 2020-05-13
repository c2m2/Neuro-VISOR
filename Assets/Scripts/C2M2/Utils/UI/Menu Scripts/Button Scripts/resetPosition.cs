using UnityEngine;

namespace C2M2
{
    namespace Utils
    {
        public class ResetPositionMono : MonoBehaviour
        {
            [Tooltip("If true, object will be centered by its bounding box.")]
            public bool useBoundingBox = true;
            public Vector3 overloadPosition;
            public Vector3 overloadRotation;
            private Vector3 initialPosition;
            private Vector3 initialRotation;
            private void Awake()
            {
                if (useBoundingBox)
                {
                    MeshFilter mf = GetComponent<MeshFilter>();
                    //mf.mesh.bounds.center;
                }
            }
            // Use this for initialization
            void Start()
            {
                initialPosition = gameObject.transform.position;
                initialRotation = gameObject.transform.eulerAngles;
            }

            public void ResetPosition()
            {
                gameObject.transform.position = (overloadPosition == null) ? initialPosition : overloadPosition;
                gameObject.transform.eulerAngles = (overloadRotation == null) ? initialRotation : overloadRotation;
            }
        }
    }
}
