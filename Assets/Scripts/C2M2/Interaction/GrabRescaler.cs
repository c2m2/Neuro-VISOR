using UnityEngine;
using C2M2.Interaction.VR;

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
        public float minPercentage = 0.25f;
        public float maxPercentage = 4f;
        public bool xScale = true;
        public bool yScale = true;
        public bool zScale = true;
        public OVRInput.Button vrThumbstick = OVRInput.Button.PrimaryThumbstick;
        public KeyCode incKey = KeyCode.UpArrow;
        public KeyCode decKey = KeyCode.DownArrow;
        public KeyCode resetKey = KeyCode.R;
        private PublicOVRGrabber grabber;

        ///<returns>A float between -1 and 1, where -1 means the thumbstick y axis is completely down and 1 implies it is all the way up</returns>
        private float ThumbstickScaler
        {
            get
            {
                float scaler = -1;
                if (GameManager.instance.vrDeviceManager.VRActive) return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, grabber.Controller).y;
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
                if (GameManager.instance.vrDeviceManager.VRActive) return OVRInput.Get(vrThumbstick, grabber.Controller);
                else return Input.GetKey(resetKey);
            }
        }

        private void Start()
        {
            grabbable = GetComponent<OVRGrabbable>();

            // Use this to determine how to scale at runtime
            origScale = transform.localScale;
            minScale = minPercentage * origScale;
            maxScale = maxPercentage * origScale;
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
            else
            { // Otherwise resolve our new scale
                Vector3 scaleValue = scaler * ThumbstickScaler * origScale;
                Vector3 newLocalScale = transform.localScale + scaleValue;

                // Makes sure the new scale is within the determined range
                if (newLocalScale.x < minScale.x || newLocalScale.y < minScale.y || newLocalScale.z < minScale.z) newLocalScale = minScale;
                if (newLocalScale.x > maxScale.x || newLocalScale.y > maxScale.y || newLocalScale.z > maxScale.z) newLocalScale = maxScale;

                // Only scales the proper dimensions
                if (!xScale) newLocalScale.x = transform.localScale.x;
                if (!yScale) newLocalScale.y = transform.localScale.y;
                if (!zScale) newLocalScale.z = transform.localScale.z;

                transform.localScale = newLocalScale;
            }
        }
    }
}