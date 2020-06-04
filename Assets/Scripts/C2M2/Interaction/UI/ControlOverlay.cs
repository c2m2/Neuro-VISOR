using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.UI
{
    /// <summary>
    /// Display controls to the user upon the game start, and destroys the overlay once the user moves
    /// </summary>
    public class ControlOverlay : MonoBehaviour
    {
        private KeyCode[] keys;
        private bool anyRPressed
        {
            get
            {
                // If any activation ket is pressed, disable this object
                foreach (KeyCode key in keys)
                {
                    if (Input.GetKey(key)) return true;
                }
                if (Input.GetAxis("Horizontal") > 0
                    || Input.GetAxis("Vertical") > 0) return true;

                return false;
            }
        }
        public void SetActivationKeys(KeyCode[] keys)
        {
            this.keys = keys;
        }

        // Update is called once per frame
        void Update()
        {
            if (anyRPressed)
            {
                Destroy(gameObject);
            }
        }
    }
}
