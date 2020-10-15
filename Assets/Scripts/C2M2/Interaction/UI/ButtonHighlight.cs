using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace C2M2.Interaction.UI
{
    public class ButtonHighlight : MonoBehaviour
    {
        public Image defaultImg = null;
        public Image highlightImg = null;
        public float highlightSeconds = 0.3f;
        private bool highlighted = false;

        private void Awake()
        {
            if (defaultImg == null || highlightImg == null)
            {
                Debug.LogError("ImageSwitch missing images on " + name);
            }
        }

        public void Highlight()
        {
            defaultImg.gameObject.SetActive(false);
            highlightImg.gameObject.SetActive(true);
            highlighted = true;
        }

        public void Unhighlight()
        {
            highlightImg.gameObject.SetActive(false);
            defaultImg.gameObject.SetActive(true);
            highlighted = false;
        }

        public void Toggle()
        {
            if (highlighted) Unhighlight();
            else Highlight();

        }

        public void TimedHighlight()
        {
            StartCoroutine(SwitchCoroutine());
        }

        private IEnumerator SwitchCoroutine()
        {
            Highlight();

            yield return new WaitForSeconds(highlightSeconds);

            Unhighlight();
        }
    }
}