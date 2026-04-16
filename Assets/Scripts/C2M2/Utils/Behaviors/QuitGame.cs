using UnityEngine;
using C2M2.Interaction.VR;

namespace C2M2.Utils
{
    public class QuitGame : MonoBehaviour
    {
        public KeyCode quitKey = KeyCode.Escape;

        private bool OculusRequested
        {
            get
            {
                XRInputBridge xri = XRInputBridge.Instance;
                return xri != null && xri.GetMenuButton();
            }
        }

        private bool QuitRequested
        {
            get
            {
                return GameManager.instance.vrDeviceManager.VRActive
                    ? (OculusRequested || Input.GetKey(quitKey))
                    : Input.GetKey(quitKey);
            }
        }

        [Tooltip("If true, game will quit after X frames.")]
        public bool QuitAfterX = false;
        [Tooltip("Number of frames to quit after.")]
        public int xFrames = 300;

        void Update()
        {
            if ((QuitAfterX && Time.frameCount >= xFrames) || QuitRequested)
                Quit();
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
