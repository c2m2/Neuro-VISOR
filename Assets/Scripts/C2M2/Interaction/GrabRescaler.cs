using UnityEngine;
using C2M2.Interaction.VR;
using C2M2.Utils;

namespace C2M2.Interaction
{
    /// <summary>
    /// Controls the scaling of a transform
    /// </summary>
    [RequireComponent(typeof(OVRGrabbable))]
    public class GrabRescaler : MonoBehaviour
    {
        private OVRGrabbable grabbable = null;
        private Vector3 origScale;
        private Vector3 minScale;
        private Vector3 maxScale;
        public float scaler = 0.2f;
        public float minPercentage = 0.5f;
        public float maxPercentage = 3f;
        public bool xScale = true;
        public bool yScale = true;
        public bool zScale = true;
        public OVRInput.Button vrThumbstick = OVRInput.Button.PrimaryThumbstick;
        public OVRInput.Button vrThumbstickS = OVRInput.Button.SecondaryThumbstick;
        public KeyCode incKey = KeyCode.UpArrow;
        public KeyCode decKey = KeyCode.DownArrow;
        public KeyCode resetKey = KeyCode.R;
        private PublicOVRGrabber grabber;

        ///<returns>A float between -1 and 1, where -1 means the thumbstick y axis is completely down and 1 implies it is all the way up</returns>
        private float ThumbstickScaler
        {
            get
            {
                if (GameManager.instance.vrDeviceManager.VRActive) return (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y + OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y);
                else if (Input.GetKey(incKey)) return 1;
                else if (Input.GetKey(decKey)) return -1;
                return 0;
            }
        }


        ///<returns>A boolean of whether the joystick is pressed</returns>
        private bool ThumbstickPressed
        {
            get
            {
                if (GameManager.instance.vrDeviceManager.VRActive) return (OVRInput.Get(vrThumbstick) || OVRInput.Get(vrThumbstickS));
                else return Input.GetKey(resetKey);
            }
        }

        private void Start()
        {
            grabbable = GetComponent<OVRGrabbable>();

            // Use this to determine how to scale at runtime
            origScale = transform.localScale;
            minScale = Vector3.one * 0.25f;
            maxScale = Vector3.one * 5f;
        }

        void Update()
        {
            if (!GameManager.instance.vrDeviceManager.VRActive) return;

            grabber = (PublicOVRGrabber)grabbable.grabbedBy;
            if (grabbable.isGrabbed)
            {
                Rescale();
            }
        }

        public void Rescale()
        {
            // if joystick is pressed in, it resets the scale to the original scale
            if (ThumbstickPressed)
            {
                transform.localScale = origScale;
            }
            else if(ThumbstickScaler != 0)
            { // Otherwise resolve our new scale
                Vector3 scaleValue = scaler * ThumbstickScaler * origScale;
                Vector3 newLocalScale = transform.parent.localScale + scaleValue;

                // Makes sure the new scale is within the determined range
                newLocalScale = Math.Clamp(newLocalScale, minScale, maxScale);

                // Only scales the proper dimensions
                if (!xScale) newLocalScale.x = transform.parent.localScale.x;
                if (!yScale) newLocalScale.y = transform.parent.localScale.y;
                if (!zScale) newLocalScale.z = transform.parent.localScale.z;

                transform.parent.localScale = newLocalScale;
            }
        }
    }
}