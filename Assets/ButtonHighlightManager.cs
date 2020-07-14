using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.UI
{
    public class ButtonHighlightManager : MonoBehaviour
    {
        public ButtonHighlight[] buttons = null;

        private Dictionary<ButtonHighlight, int> buttonLookup;

        private void Awake()
        {
            if(buttons == null)
                buttons = GetComponentsInChildren<ButtonHighlight>();

            if(buttons == null)
            {
                Debug.LogError("No buttons given to button manager on " + name);
                Destroy(this);
            }

            buttonLookup = new Dictionary<ButtonHighlight, int>(buttons.Length);
            for(int i = 0; i < buttons.Length; i++)
            {
                buttonLookup.Add(buttons[i], i);
            }
        }

        public void HighlightButton(ButtonHighlight target)
        {
            // Unhighlight all buttons
            foreach(ButtonHighlight button in buttons)
            {
                button.Unhighlight();
            }

            // Highlight target button
            buttons[buttonLookup[target]].Highlight();
        }
    }
}