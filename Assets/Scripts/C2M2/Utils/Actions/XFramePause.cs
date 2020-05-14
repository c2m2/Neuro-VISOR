using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Utils.DebugUtils.Actions
{
    public class XFramePause : MonoBehaviour
    {
        [Tooltip("Pause the game after this many frames")]
        public int frameCount = 5;

        // Update is called once per frame
        void Update()
        {
            if (Time.frameCount == frameCount)
            {
                Debug.Break();
            }
        }
    }
}