using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace C2M2.Interaction.UI
{
    public class RaycastInputField : MonoBehaviour
    {
        public enum ContentType { Standard, IntegerNumber, DecimalNumber };
        [System.Serializable]
        public class OnSubmitEvent : UnityEvent<string> { }
        [System.Serializable]
        public class OnChangeEvent : UnityEvent<string> { }
        #region Public Variables
        public Transform menuAnchor;
        [Header("Image")]
        [Tooltip("Background image for the input field")]
        public Image targetGraphic;
        public Color defaultColor;
        public Color highlightedColor;
        public Color invalidColor = Color.red;
        public Color validColor = Color.green;
        [Header("Text")]
        [Tooltip("Text mesh object for the input text")]
        public TextMeshProUGUI textComponent;
        public string text;
        [Tooltip("Text mesh object for the placeholder text")]
        public TextMeshProUGUI placeHolder;
        public bool takeFromHolder = false;
        public string placeHolderText = "n/a";
        [Tooltip("Text mesh object for the caret")]
        public TextMeshProUGUI caret;
        public char caretCharacter = '|';
        [Range(0, 8)]
        public float caretBlinkRate = 1f;
        [Header("Validation")]
        [Tooltip("Limit input to a number of characters. 0 means no limit")]
        public int characterLimit;
        public ContentType contentType = ContentType.Standard;
        [Header("Events")]
        [SerializeField]
        public OnChangeEvent onValueChanged;
        [SerializeField]
        public OnSubmitEvent onEndEdit;
        #endregion
        #region Private Variables
        private RaycastKeyboard keyboard;
        private bool caretEnabled = false;
        #endregion

        private void Start()
        {
            keyboard = GameManager.instance.raycastKeyboard;
        }

        public void Activate(RaycastHit hit)
        { // Avoid confusion and disable keyboard, get active state from keyboard, then enable keyboard
            keyboard.gameObject.SetActive(false);
            keyboard.InputFieldActivate(this, hit);
            if (menuAnchor != null)
            {
                keyboard.menuAnchor = menuAnchor;
            }
            keyboard.gameObject.SetActive(true);
            // Start caret blink
            StartCoroutine(caretBlink(caretBlinkRate));
            // Set highlighted color
            targetGraphic.color = highlightedColor;
            textComponent.enabled = true;
            placeHolder.enabled = false;
        }
        public void Deactivate()
        {
            // Stop caret blink
            StopAllCoroutines();
            caretEnabled = false;
            caret.enabled = false;
            textComponent.enabled = false;
            placeHolder.enabled = true;
            // Deactivate keyboard
            keyboard.gameObject.SetActive(false);
        }
        public void CharacterIntake(string c)
        { // This should sheck to see if the received char is a backspace, enter, etc and funnel characters appropriately
          // Need a case for TAB
          // Need a case for Backspace
            if (string.Compare(c, "DEL") == 0)
            { // If we receive a backspace
                text = RemoveLast(text, onValueChanged);
            }
            else if (string.Compare(c, "ENT") == 0)
            { // If we receive an enter
                if (ValidateInput(text, contentType))
                {
                    ColorToValid();
                    Invoke("ColorToDefault", 0.5f); ;
                    Deactivate();
                    text = SubmitText(onEndEdit, text);
                }
                else
                {
                    ColorToInvalid();
                    Invoke("ColorToHighlighted", 0.5f);
                }
            }
            else
            { // Need data validation here
                text = AppendText(text, c, onValueChanged);
                UpdateDisplayText();
            }
        }
        private static string AppendText(string txt, string c, OnChangeEvent changeEvent)
        {
            txt += c;
            changeEvent.Invoke(txt);
            return txt;
        }
        private static string RemoveLast(string txt, OnChangeEvent changeEvent)
        {
            if (txt.Length > 0)
            {
                txt = txt.Remove(txt.Length - 1);
                changeEvent.Invoke(txt);
            }
            return txt;
        }
        private static string SubmitText(OnSubmitEvent submitEvent, string txt)
        {
            submitEvent.Invoke(txt);
            txt = "";
            return txt;
        }
        public static void PrintText(string text)
        {
            Debug.Log(text);
        }
        private bool ValidateInput(string txt, ContentType criteria)
        {
            if (criteria == ContentType.Standard)
            {
                return true;
            }
            else if (criteria == ContentType.IntegerNumber)
            {
                return int.TryParse(txt, out int result);
            }
            else if (criteria == ContentType.DecimalNumber)
            {
                return float.TryParse(txt, out float result);
            }
            return false;
        }
        private void UpdateDisplayText()
        {
            textComponent.text = text;
            UpdateCaretPosition();
            if (caretEnabled)
            {
                caret.enabled = true;
            }
            else
            {
                caret.enabled = false;
            }
        }
        private void ColorToDefault()
        {
            targetGraphic.color = defaultColor;
        }
        private void ColorToHighlighted()
        {
            targetGraphic.color = highlightedColor;
        }
        private void ColorToInvalid()
        {
            targetGraphic.color = invalidColor;
        }
        private void ColorToValid()
        {
            targetGraphic.color = validColor;
        }
        private void UpdateCaretPosition()
        {
            if (text.Length == 0)
            { // If there is no text, put it at 0
                caret.transform.localPosition = new Vector3(0f, caret.transform.position.y, caret.transform.position.z);
            }
            else
            { // Otherwise get it from the text
                TMP_CharacterInfo curInfo = textComponent.textInfo.characterInfo[textComponent.text.Length - 1];
                // Find the rightmost point in the current text and put the caret just to the right of that
                caret.transform.localPosition = new Vector3(curInfo.bottomRight.x + (curInfo.pointSize / 4), caret.transform.localPosition.y, caret.transform.localPosition.z);
            }
        }
        private IEnumerator caretBlink(float blinkRate)
        {
            while (true)
            {
                yield return new WaitForSeconds(blinkRate);
                caretEnabled = !caretEnabled;
                UpdateDisplayText();
            }
        }
    }
}
