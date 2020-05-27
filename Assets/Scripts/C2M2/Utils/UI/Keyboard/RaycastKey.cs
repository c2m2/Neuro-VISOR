using UnityEngine;
using UnityEngine.UI;

namespace C2M2.Interaction.UI
{
    public class RaycastKey : MonoBehaviour
    {
        public Image image;
        public Color defaultColor = Color.white;
        public Color clickedColor = Color.white;
        public Color inactiveColor = Color.gray;
        public float clickTime = 0.3f;
        private bool active = true;
        private RaycastKeyboard keyboard;
        public string character;
        // Start is called before the first frame update
        void Awake()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }
            if (image != null)
            {
                image.color = defaultColor;
            }
            else
            {
                Debug.LogError("No image found on key" + name);
            }
            GetComponent<RaycastPressEvents>().OnPress.AddListener(raycastHit => Click());
        }
        private void Start()
        {
            keyboard = GameObject.Find("GameManager").GetComponent<GameManager>().raycastKeyboard;
            GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text = character;
        }
        public void Click()
        {
            if (keyboard != null)
            {
                if (active)
                {
                    active = false;
                    if (keyboard.PassChar(character))
                    { // If we successfully pass a character to the input field,
                        image.color = clickedColor;
                    }
                    else
                    { // If our keyboard has no input field
                        image.color = inactiveColor;
                    }
                    Invoke("Reenable", clickTime);
                }
            }
            else
            {
                image.color = inactiveColor;
            }
        }
        private void Reenable()
        {
            image.color = defaultColor;
            active = true;
        }
    }
}