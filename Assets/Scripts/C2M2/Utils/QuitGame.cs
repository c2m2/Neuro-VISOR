using UnityEngine;

namespace C2M2.Utils
{
    public class QuitGame : MonoBehaviour
    {
        public KeyCode quitKey = KeyCode.Escape;
        public OVRInput.Button quitButton = OVRInput.Button.Start;
        private bool OculusRequested
        {
            get
            {
                return OVRInput.Get(quitButton, OVRInput.Controller.LTouch) || OVRInput.Get(quitButton, OVRInput.Controller.RTouch);
            }
        }
        private bool QuitRequested
        {
            get
            {
                return GameManager.instance.vrDeviceManager.VRActive ?
                    (OculusRequested || Input.GetKey(quitKey))
                    : Input.GetKey(quitKey);
            }
        }

        [Tooltip("If true, game will quit after X frames.")]
        public bool QuitAfterX = false;
        [Tooltip("Number of frames to quit after.")]
        public int xFrames = 300;

        // Update is called once per frame
        void Update()
        {
            // Quit if the user requests or when the user requests
            if ((QuitAfterX && Time.frameCount >= xFrames) || QuitRequested)
            {
                Quit();
            }
        }

        private void Quit()
        {
#if UNITY_STANDALONE
            Application.Quit();
#endif
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}