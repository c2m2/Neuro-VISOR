using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Utils.Animation
{
    public class Rotate : MonoBehaviour
    {
        public bool rigidbodyRotate = true;

        [Header("X-axis")]
        public bool rotateX = true;
        public float XSpeed = 1f;
        [Tooltip("Produce random speed each fixed frame")]
        public bool randomX = false;

        [Header("Y-axis")]
        public bool rotateY = true;
        public float YSpeed = 1f;
        [Tooltip("Produce random speed each fixed frame")]
        public bool randomY = false;

        [Header("Z-axis")]
        public bool rotateZ = true;
        public float ZSpeed = 1f;
        [Tooltip("Produce random speed each fixed frame")]
        public bool randomZ = false;

        private Vector3 rotationVector;

        public Rigidbody rb;
        private Quaternion rotationQuat;


        // Start is called before the first frame update
        void Awake()
        {
            if (!rotateX)
            {
                XSpeed = 0f;
            }

            if (!rotateY)
            {
                YSpeed = 0f;
            }

            if (!rotateZ)
            {
                ZSpeed = 0f;
            }

            rotationVector = new Vector3(XSpeed, YSpeed, ZSpeed);
        }

        private void Start()
        {
            if (rigidbodyRotate)
            {
                rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.Log("in Rotate, could not find rigibody");
                    rigidbodyRotate = false;
                }
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (randomX || randomY || randomZ)
            {
                if (randomX)
                {
                    XSpeed = Random.Range(0f, 3f);
                }

                if (randomY)
                {
                    YSpeed = Random.Range(0f, 3f);
                }

                if (randomZ)
                {
                    ZSpeed = Random.Range(0f, 3f);
                }

                rotationVector[0] = XSpeed;
                rotationVector[1] = YSpeed;
                rotationVector[2] = ZSpeed;

            }

            if (rigidbodyRotate)
            {

                rotationQuat = Quaternion.Euler(rotationVector * Time.deltaTime);
                rb.MoveRotation(rb.rotation * rotationQuat);
            }
            else
            {
                transform.Rotate(rotationVector, Space.Self);
            }

        }
    }
}