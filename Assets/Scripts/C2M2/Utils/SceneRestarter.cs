using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace C2M2.Utils
{
    /// <summary>
    /// Restart the scene upon a button click.
    /// </summary>
    /// <remarks>
    /// Used for research symposium live demos
    /// </remarks>
    public class SceneRestarter : MonoBehaviour
    {
        public KeyCode keyboardRestartButton = KeyCode.Space;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(keyboardRestartButton))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}
