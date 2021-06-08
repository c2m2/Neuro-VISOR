using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Interaction.UI
{
    public class RaycastShiftKey : MonoBehaviour
    {
        public GameObject uppercaseKeys;
        public GameObject lowercaseKeys;

        public void SwitchKeyboard()
        {
            if (uppercaseKeys.activeSelf)
            {
                uppercaseKeys.SetActive(false);
                lowercaseKeys.SetActive(true);
            }
            else
            {
                uppercaseKeys.SetActive(true);
                lowercaseKeys.SetActive(false);
            }
        }
    }
}