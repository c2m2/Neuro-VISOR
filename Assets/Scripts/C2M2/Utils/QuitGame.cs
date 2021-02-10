using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Utils
{
    public class QuitGame : MonoBehaviour
    {
        public KeyCode quitKey = KeyCode.Escape;
        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(quitKey))
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