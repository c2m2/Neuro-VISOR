using UnityEngine;

namespace C2M2.Interaction
{
    /// <summary>
    /// Adds some percentage of a transform's original scale at runtime
    /// </summary>
    [RequireComponent(typeof(OVRGrabbable))]
    public class GrabRescaler : MonoBehaviour
    {
        private OVRGrabbable grabbable = null;
        private Vector3 origScale;
        public float scaler = 0.2f;
        public float minPercentage = 0.1f;
        public float maxPercentage = 10f;

        // Returns a value between -2 and 2, where -2 implies both thumbsticks are held down, and 2 implies both are held up.
        private float ThumbstickScaler
        {
            get
            {
                // Uses average joystick y axis value
                float y1 = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
                float y2 = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
                return (y1 + y2) / 2;
            }
        }

        private bool ThumbsticksPressed
        {
            get
            {
                return OVRInput.Get(OVRInput.Button.SecondaryThumbstick) 
                    && OVRInput.Get(OVRInput.Button.PrimaryThumbstick);
            }
        }

        private void Start()
        {
            if (!GameManager.instance.vrIsActive) Destroy(this);

            grabbable = GetComponent<OVRGrabbable>();

            // Use this to determine how to scale at runtime
            origScale = transform.localScale;
        }

        void Update()
        {
            if (grabbable.isGrabbed)
            {
                // if both joysticks are pressed in, it resets the scale to the original scale
                if (ThumbsticksPressed)
                {
                    transform.localScale = origScale;
                }
                else
                { // Otherwise resolve our new scale
                    Vector3 scaleValue = scaler * ThumbstickScaler * origScale;
                    Vector3 newLocalScale = transform.localScale + scaleValue;

                    // Is the new scale too big or too small?
                    bool newScaleAcceptable = newLocalScale.magnitude > (minPercentage * origScale).magnitude
                        && newLocalScale.magnitude < (maxPercentage * origScale).magnitude;
                    if (newScaleAcceptable)
                    {
                        transform.localScale = newLocalScale;
                    }
                }
            }
        }
    }
}