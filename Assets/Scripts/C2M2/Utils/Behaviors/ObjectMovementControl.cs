using System;
using UnityEngine;

namespace C2M2.Utils
{
    [RequireComponent(typeof(OVRGrabbable))]
    public class ObjectMovementControl : MonoBehaviour
    {
        private OVRGrabbable grabbable = null;

        public OVRInput.Button resetButton = OVRInput.Button.Start;
        public KeyCode resetKey = KeyCode.X;
        public Vector3 resetPosition = Vector3.zero;
        public Vector3 resetRotation = Vector3.zero;
        public int moveMouseButton = 1; //right button

        private bool isMouseDrag;
        private Vector3 offset;
        private Vector3 screenPosition;

        //return the object that was raycasted on
        GameObject ReturnClickedObject(out RaycastHit hit)
        {
            GameObject target = null;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction * 10, out hit))
            {
                try
                {
                    return hit.collider.transform.parent.gameObject;
                }
                catch (NullReferenceException)
                {
                    return hit.collider.gameObject;
                }
            }
            return target;
        }

        private bool ResetRequested
        {
            get
            {
                if (GameManager.instance.vrDeviceManager.VRActive)
                {
                    return OVRInput.Get(resetButton) && grabbable.isGrabbed;
                }
                else
                {
                    return Input.GetKey(resetKey) && ReturnClickedObject(out RaycastHit hitInfo) == gameObject;
                }
            }
        }

        private bool DesktopMoveRequested
        {
            get
            {
                return !GameManager.instance.vrDeviceManager.VRActive
                    && Input.GetMouseButtonDown(moveMouseButton)
                    && ReturnClickedObject(out RaycastHit hitInfo) == gameObject;
            }
        }

        private void Start()
        {
            grabbable = GetComponent<OVRGrabbable>();
            resetPosition = transform.position;
            resetRotation = transform.eulerAngles;
        }

        void Update()
        {
            if (ResetRequested)
            {
                transform.position = resetPosition;
                transform.eulerAngles = resetRotation;
            }

            if (DesktopMoveRequested)
            {
                isMouseDrag = true;
                //Convert world position to screen position
                screenPosition = Camera.main.WorldToScreenPoint(transform.position);
                offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z));
            }

            if (Input.GetMouseButtonUp(moveMouseButton))
            {
                isMouseDrag = false;
            }

            if (isMouseDrag)
            {
                //track mouse position.
                Vector3 currentScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z);

                //convert screen position to world position with offset changes.
                Vector3 currentPosition = Camera.main.ScreenToWorldPoint(currentScreenSpace) + offset;

                //It will update target gameobject's current postion
                transform.position = currentPosition;
            }
        }
    }
}