using UnityEngine;

using C2M2.Interaction.VR;
namespace C2M2.Interaction.Signaling
{
    public class MouseEventSignaler : RaycastEventSignaler
    {
        public KeyCode[] grabKeys = new KeyCode[] { KeyCode.G };

        Transform grabTransform;
        PublicOVRGrabber grabber;
        SphereCollider grabVolume;

        protected override void OnAwake()
        {
            grabTransform = new GameObject().transform;
            // Name grabber object
            grabTransform.name = "EmulatorGrab";

            // Create grab collider
            grabVolume = grabTransform.gameObject.AddComponent<SphereCollider>();
            grabVolume.radius = 0.1f;
            grabVolume.isTrigger = true;

            // Greate OVRGrabber
            grabber = grabTransform.gameObject.AddComponent<PublicOVRGrabber>();
            grabber.M_GrabVolumes = new Collider[] { grabVolume };

            Rigidbody rb = grabTransform.GetComponent<Rigidbody>() ?? grabTransform.gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        protected override void OnStart() { }
        /// <summary>
        /// This is returns true if the left mouse button is pressed down
        /// </summary>
        protected override bool BeginRaycastingCondition() => Input.GetMouseButton(0);
        /// <summary>
        /// This builds a ray from the mouse's position, and attempts a raycast using that ray
        /// </summary>
        protected override bool RaycastingMethod(out RaycastHit hit, float maxDistance, LayerMask layerMask)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, maxDistance, layerMask);
           
            /*
            bool grabPressed = false;
            foreach(KeyCode key in grabKeys)
            {
                if (Input.GetKey(key)) grabPressed = true;
            }

            if (grabPressed)
            {
                grabVolume.enabled = true;

                if (raycastHit) grabTransform.position = hit.point;

                // Don't allow raycast event signalling if a grab is happening
                return false;
            }
            else grabVolume.enabled = false;
            */

            return raycastHit;
        }
        /// <summary>
        /// With mouse raycasting, we only want to press the mouse button to trigger events
        /// </summary>
        protected override bool ChildsPressCondition() => true;

        /// We don't need any extra functionality for our mouse activator,
        /// so these just return immediatley
        protected override void OnPressSub() { }
        protected override void OnHoldPressSub() { }
        protected override void OnEndPressSub() { }
    }
}
