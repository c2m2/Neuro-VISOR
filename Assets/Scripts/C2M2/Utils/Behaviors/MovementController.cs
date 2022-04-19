using System.Collections;
using UnityEngine;

namespace C2M2.Utils
{
    /// <summary>
    /// Allow an object to be moved through keyboard controls
    /// </summary>
    public class MovementController : MonoBehaviour
    {
        public KeyCode forwardKey = KeyCode.W;
        public KeyCode backwardKey = KeyCode.S;
        public KeyCode leftKey = KeyCode.A;
        public KeyCode rightKey = KeyCode.D;
        public KeyCode controlKey = KeyCode.LeftControl;
        public bool limitPos = false;
        public Vector3 maxPos = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public Vector3 minPos = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        private bool ForwardPress { get { return Input.GetKey(forwardKey); } }
        private bool BackwardPress { get { return Input.GetKey(backwardKey); } }
        private bool LeftPress { get { return Input.GetKey(leftKey); } }
        private bool RightPress { get { return Input.GetKey(rightKey); } }
        private bool ControlPress { get { return Input.GetKey(controlKey); } }
        private float ScrollWheel { get { return Input.GetAxis("Mouse ScrollWheel"); } }

        private Coroutine moveRoutine = null;
        public bool Moving { get; private set; } = false;
        private static float speedModifier = 2.5f;
        public Transform relativeTo = null;

        public float rotateSpeed = 5.0f;

        private float x, y, z = 0.0f;

        public void EnableMovement()
        {
            moveRoutine = StartCoroutine(Movement());
            Moving = true;
        }
        public void DisableMovement()
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
            Moving = false;
        }

        private IEnumerator Movement()
        {
            while (true)
            {
                float speed = speedModifier * Time.deltaTime;
                float pos_x = 0f, pos_y = 0f, pos_z = 0f;
                if (ForwardPress) pos_z += speed;             // Move forward
                if (BackwardPress) pos_z -= speed;            // Move backward
                if (LeftPress) pos_x -= speed;                // Move left
                if (RightPress) pos_x += speed;               // Move right

                if (ControlPress)
                {
                    Cursor.lockState = CursorLockMode.Locked;

                    x += rotateSpeed * Input.GetAxis("Mouse X");  // Turn left and right
                    y -= rotateSpeed * Input.GetAxis("Mouse Y");  // Turn up and down

                    transform.eulerAngles = new Vector3(y, x, z);

                    pos_y = ScrollWheel; // Move up and down
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                }

                if (relativeTo == null) transform.Translate(new Vector3(pos_x, pos_y, pos_z), Space.World);
                else transform.Translate(new Vector3(pos_x, pos_y, pos_z), relativeTo);

                if (limitPos) transform.position = transform.position.Clamp(minPos, maxPos);

                yield return null;
            }
        }

        private void OnEnable()
        {
            EnableMovement();
            x = transform.eulerAngles.y;
            y = transform.eulerAngles.x;
            z = transform.eulerAngles.z;
        }

        private void OnDisable()
        {
            DisableMovement();
        }

        // set/get x, y, z for saving/loading
        public void setXYZ(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3 getXYZ()
        {
            Vector3 ret;
            ret.x = x;
            ret.y = y;
            ret.z = z;
            return ret;
        }
    }
}