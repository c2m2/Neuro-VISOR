using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using C2M2.Interaction.UI;

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
        public KeyCode rotationKey = KeyCode.LeftControl;
        public bool limitPos = false;
        public Vector3 maxPos = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public Vector3 minPos = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        private bool ForwardPress { get { return Input.GetKey(forwardKey); } }
        private bool BackwardPress { get { return Input.GetKey(backwardKey); } }
        private bool LeftPress { get { return Input.GetKey(leftKey); } }
        private bool RightPress { get { return Input.GetKey(rightKey); } }
        private bool RotationPress { get { return Input.GetKey(rotationKey); } }

        private Coroutine moveRoutine = null;
        public bool isMoving { get; private set; } = false;
        public float speed = 0.01f;
        public Transform relativeTo = null;

        public float rotateSpeed = 2.0f;

        private float x = 0.0f;
        private float y = 0.0f;

        private GameObject controlUI;

        void Update()
        {
            if (RotationPress)
            {
                Cursor.lockState = CursorLockMode.Locked;

                x += rotateSpeed * Input.GetAxis("Mouse X");
                y -= rotateSpeed * Input.GetAxis("Mouse Y");

                transform.eulerAngles = new Vector3(y, x, 0.0f);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }

        }

        private void OnEnable ()
        {
            EnableMovement();
            InitUI(true);
        }

        private void OnDisable()
        {
            DisableMovement();
            InitUI(false);
        }

        public void EnableMovement()
        {
            moveRoutine = StartCoroutine(Movement());
            isMoving = true;
        }
        public void DisableMovement()
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
            isMoving = false;
        }

        private IEnumerator Movement()
        {
            while (true)
            {
                float x = 0f, y = 0f, z = 0f;
                if (ForwardPress) z += speed;             // Move forward
                if (BackwardPress) z -= speed;            // Move backward
                if (LeftPress) x -= speed;                // Move left
                if (RightPress) x += speed;               // Move right

                if (relativeTo == null) transform.Translate(new Vector3(x, y, z), Space.World);
                else transform.Translate(new Vector3(x, y, z), relativeTo);

                if (limitPos) transform.position = transform.position.Clamp(minPos, maxPos);

                yield return new WaitForFixedUpdate();
            }
        }

        private void InitUI(bool enable)
        {
            if (enable)
            {
                controlUI = Instantiate(Resources.Load("Prefabs/ControlOverlay") as GameObject);
                List<KeyCode> keys = new List<KeyCode>(4)
                {
                    forwardKey,
                    backwardKey,
                    leftKey,
                    rightKey,
                    forwardKey
                };
                controlUI.GetComponent<ControlOverlay>().SetActivationKeys(keys.ToArray());
            }
            else
            {
                if (controlUI != null) Destroy(controlUI);
            }
        }
    }
}