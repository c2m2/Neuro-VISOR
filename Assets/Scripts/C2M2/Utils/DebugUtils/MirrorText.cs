using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace C2M2.Utils.DebugUtils
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MirrorText : MonoBehaviour
    {
        public TextMeshProUGUI textToMirror;
        public int fps = 10;
        private TextMeshProUGUI personalText;
        private void Awake()
        {
            personalText = GetComponent<TextMeshProUGUI>();
        }
        void Start()
        {
            StartCoroutine(UpdateSlow(1 / fps));
        }
        private IEnumerator UpdateSlow(float delayTime)
        {
            while (true)
            {
                personalText.text = textToMirror.text;
                yield return new WaitForSeconds(delayTime);
            }
        }
    }
}