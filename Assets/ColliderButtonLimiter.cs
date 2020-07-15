using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.UI {
    public class ColliderButtonLimiter : MonoBehaviour
    {
        public ButtonHighlightManager vizController;
        public ButtonHighlightManager colController;

        // Update is called once per frame
        void Update()
        {
            // If the collider diameter is smaller than the visual diameter, highlight the correct collider button and invoke a collider diameter increase
            if(colController.activeIndex < vizController.activeIndex)
            {
                colController.HighlightButton(vizController.activeIndex);
                ButtonHighlight correctButton = colController.buttons[colController.activeIndex];
                correctButton.gameObject.GetComponent<RaycastPressEvents>().OnPress.Invoke(new RaycastHit());
            }
        }
    }
}