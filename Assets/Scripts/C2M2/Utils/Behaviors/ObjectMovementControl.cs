using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace C2M2.Utils
{
    [System.Serializable]
    public class MenuButtonEvent : UnityEvent<bool> { }

    [RequireComponent(typeof(XRGrabInteractable))]
    public class ObjectMovementControl : MonoBehaviour
    {
        private XRGrabInteractable grabbable = null;

        public List<InputDevice> devicesWithMenuBtn;
        public MenuButtonEvent menuButtonPress;
        private bool lastButtonState = false;
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
                bool tempState = false;
                foreach(var device in devicesWithMenuBtn)
                {
                    bool menuButtonState = false;
                    tempState = device.TryGetFeatureValue(CommonUsages.menuButton, out menuButtonState)
                                        && menuButtonState
                                        || tempState;
                }
                bool isPress = tempState != lastButtonState;
                if(isPress)
                {
                    menuButtonPress.Invoke(tempState);
                    lastButtonState = tempState;
                }
                if (GameManager.instance.vrDeviceManager.VRActive)
                {
                    return isPress && grabbable.isSelected;
                }
                else
                {
                    return Input.GetKey(resetKey) && IsClickedObjectThisObject(out RaycastHit hitInfo);
                }
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

        private void Awake()
        {
            if (menuButtonPress == null)
            {
                menuButtonPress = new MenuButtonEvent();
            }
            devicesWithMenuBtn = new List<InputDevice>();
        }

        private void Start()
        {
            grabbable = GetComponent<XRGrabInteractable>();
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