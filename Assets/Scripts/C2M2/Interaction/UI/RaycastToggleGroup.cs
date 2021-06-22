using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Interaction.UI
{
    public class RaycastToggleGroup : MonoBehaviour
    {
        public RaycastToggle[] toggles;
        public int defaultToggleId = -1;

        private void Awake()
        {
            if(toggles.Length == 0)
            {
                Debug.LogError("No RaycastToggles given to RaycastToggleGroup");
                Destroy(this);
            }

            for(int i = 0; i < toggles.Length; i++)
            {
                toggles[i].groupId = i;
            }

        }
        private void Start()
        {
            if(defaultToggleId >= 0 && defaultToggleId < toggles.Length)
            {
                RequestToggle(new RaycastHit(), toggles[defaultToggleId].groupId, true);
            }
        }
        public void RequestToggle(RaycastHit hit, int toggleId, bool toggle)
        {
            if(toggleId < 0 || toggleId >= toggles.Length)
            {
                Debug.LogError("Invalid toggle requesting access.");
                return;
            }

            if (toggle == true)
            {
                for (int i = 0; i < toggles.Length; i++)
                {
                    // If we find the right toggle, turn it on
                    if (toggles[i].groupId == toggleId)
                    {
                        toggles[i].Toggle(hit, true);
                    }
                    else if (toggles[i].toggled == true)
                    {
                        // Only toggle off if necessary
                        toggles[i].Toggle(hit, false);
                    }
                }
            }
            else
            {
                if (toggleId < 0 || toggleId >= toggles.Length)
                {
                    Debug.LogError("Invalid toggle requesting access.");
                    return;
                }
                bool allOff = true;
                for (int i = 0; i < toggles.Length; i++)
                {
                    if (toggles[i].groupId == toggleId)
                    {
                        toggles[i].Toggle(hit, false);
                    }
                    else if (toggles[i].toggled == true)
                    {
                        allOff = false;
                    }
                }

                // If all are off, turn on default toggle
                if (allOff && defaultToggleId >= 0 && defaultToggleId < toggles.Length)
                {
                    RequestToggle(hit, toggles[defaultToggleId].groupId, true);
                }
            }
        }
    }
}