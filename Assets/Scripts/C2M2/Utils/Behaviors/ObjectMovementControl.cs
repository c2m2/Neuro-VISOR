using System;
using UnityEngine;
using C2M2.Interaction.VR;

namespace C2M2.Utils
{
    [RequireComponent(typeof(OVRGrabbable))]
    public class ObjectMovementControl : MonoBehaviour
    {
        private OVRGrabbable grabbable = null;

        public KeyCode resetKey = KeyCode.X;
        public Vector3 resetPosition = Vector3.zero;
        public Vector3 resetRotation = Vector3.zero;
        public int moveMouseButton = 1; //right button

        private bool isMouseDrag;
        private Vector3 offset;
        private Vector3 screenPosition;

        //return the object that was raycasted on
        private bool IsClickedObjectThisObject(out RaycastHit hit)
        {
            GameObject target = null;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction * 10, out hit))
            {
                try
                {
                    target = hit.collider.transform.parent.gameObject;
                }
                catch (NullReferenceException)
                {
                    target =  hit.collider.gameObject;
                }
            }
            return target == gameObject;
        }

        private bool ResetRequested
        {
            get
            {
                if (GameManager.instance.vrDeviceManager.VRActive)
                {
                    XRInputBridge xri = XRInputBridge.Instance;
                    return xri != null && xri.GetMenuButton() && grabbable.isGrabbed;
                }
                return Input.GetKey(resetKey) && IsClickedObjectThisObject(out _);
            }
        }

        private bool DesktopMoveRequested
        {
            get
            {
                return !GameManager.instance.vrDeviceManager.VRActive
                    && Input.GetMouseButtonDown(moveMouseButton)
                    && IsClickedObjectThisObject(out RaycastHit hitInfo);
            }
        }

        private bool DesktopMoveEnd
        {
            get
            {
                return Input.GetMouseButtonUp(moveMouseButton);
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

            if (DesktopMoveEnd)
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