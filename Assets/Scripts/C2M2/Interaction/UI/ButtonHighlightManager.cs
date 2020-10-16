using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.UI
{
    public class ButtonHighlightManager : MonoBehaviour
    {
        public ButtonHighlight[] buttons = null;
        public int activeIndex { get; private set; } = -1;

        private Dictionary<ButtonHighlight, int> buttonLookup;

        private void Awake()
        {
            if(buttons == null)
                buttons = GetComponentsInChildren<ButtonHighlight>();

            if(buttons == null || buttons.Length == 0)
            {
                Debug.LogError("No buttons given to button manager on " + name);
                Destroy(this);
            }

            buttonLookup = new Dictionary<ButtonHighlight, int>(buttons.Length);
            for(int i = 0; i < buttons.Length; i++)
            {
                buttonLookup.Add(buttons[i], i);
            }

            SetDefaultState();
        }

        public void HighlightButton(ButtonHighlight target)
        {
            int index = buttonLookup[target];
            HighlightButton(index);
        }

        public void HighlightButton(int index)
        {
            // Unhighlight all buttons
            foreach (ButtonHighlight button in buttons)
            {
                button.Unhighlight();
            }

            // Highlight target button
            buttons[index].Highlight();
            activeIndex = index;
        }

        private void SetDefaultState()
        {
            HighlightButton(0);
        }
    }
}