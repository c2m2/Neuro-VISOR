using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
namespace C2M2.Interaction.UI
{
    public abstract class RaycastToggle : MonoBehaviour
    {
        public bool toggled { get; private set; } = false;
        public Image enabledImg = null;
        public Image disabledImg = null;

        public RaycastToggleGroup group = null;
        public int groupId { get; set; } = -1;

        public void Toggle(RaycastHit hit)
        {
            // If there is no group, toggle on/off normally
            if(group == null)
            {
                Toggle(hit, !toggled);
            }
            else
            {
                group.RequestToggle(hit, groupId, !toggled);
            }
        }
        public void Toggle(RaycastHit hit, bool toggle)
        {
            toggled = toggle;
            if (enabledImg == null || disabledImg == null)
            {
                Debug.LogError("No buttons found for RaycastToggle!");
                return;
            }

            // If enabled was just pressed, toggle turns off
            enabledImg.enabled = toggled;
            disabledImg.enabled = !toggled;

            // Child toggle functionality
            OnToggle(hit, toggled);
        }
        public abstract void OnToggle(RaycastHit hit, bool toggled);
    }
}